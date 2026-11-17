-- ============================================================
--  42_reporte_combos_stock_parado.sql
--  REPORTE INTELIGENTE: Combos y ofertas por stock inmovilizado.
--
--  sp_Reporte_StockParado devuelve, por cada componente vigente (con o
--  sin stock libre: la demanda por tipo se mide sobre todos), los datos
--  de rotación que la BLL usa para detectar el stock parado y armar los
--  combos:
--    FechaAlta            primer registro en Componentes_C (alta).
--    UltimoIngreso        última recepción de mercadería (FacturaCompra).
--    UltimaVenta          última venta no anulada que lo incluye.
--    VendidasVentana      unidades vendidas en los últimos @DiasVentana días.
--    VendidasTotal        unidades vendidas en toda la historia.
--    CostoUltimaCompra    precio unitario de la última cotización aprobada
--                         (NULL si nunca se compró con precio por ítem).
--  Las ventas anuladas (Estado = 4) no cuentan como rotación.
--
--  Solo lectura: no modifica tablas protegidas, no hace falta recalcular DV.
--  Requiere 21, 25, 26, 29 y 38. Idempotente.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Reporte_StockParado
    @DiasVentana INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    IF @DiasVentana IS NULL OR @DiasVentana < 1 SET @DiasVentana = 90;
    DECLARE @Desde DATETIME = DATEADD(DAY, -@DiasVentana, CAST(GETDATE() AS DATE));

    ;WITH Vendidas AS (
        SELECT cc.CodigoComponente,
               MAX(v.FechaVenta)                                             AS UltimaVenta,
               SUM(cc.Cantidad)                                              AS VendidasTotal,
               SUM(CASE WHEN v.FechaVenta >= @Desde THEN cc.Cantidad ELSE 0 END) AS VendidasVentana
        FROM   Ventas v
        JOIN   ComputadoraComponentes cc ON cc.IdComputadora = v.IdComputadora
        WHERE  v.Estado <> 4
        GROUP BY cc.CodigoComponente
    ),
    Ingresos AS (
        SELECT d.CodigoComponente, MAX(f.FechaEntrega) AS UltimoIngreso
        FROM   FacturaCompraDetalle d
        JOIN   FacturaCompra f ON f.NumeroFactura = d.NumeroFactura
        GROUP BY d.CodigoComponente
    ),
    Altas AS (
        SELECT CodigoComponente, MIN(Fecha) AS FechaAlta
        FROM   Componentes_C
        GROUP BY CodigoComponente
    ),
    Costos AS (
        SELECT d.CodigoComponente, d.PrecioUnitario,
               ROW_NUMBER() OVER (PARTITION BY d.CodigoComponente ORDER BY p.FechaEmision DESC) AS n
        FROM   PedidoCotizacionDetalle d
        JOIN   PedidosCotizacion p ON p.Numero = d.NumeroCotizacion
        WHERE  p.Estado = 1
    )
    SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockReservado,
           a.FechaAlta,
           i.UltimoIngreso,
           v.UltimaVenta,
           ISNULL(v.VendidasVentana, 0) AS VendidasVentana,
           ISNULL(v.VendidasTotal, 0)   AS VendidasTotal,
           k.PrecioUnitario             AS CostoUltimaCompra
    FROM   Componentes c
    LEFT JOIN Vendidas v ON v.CodigoComponente = c.Codigo
    LEFT JOIN Ingresos i ON i.CodigoComponente = c.Codigo
    LEFT JOIN Altas    a ON a.CodigoComponente = c.Codigo
    LEFT JOIN Costos   k ON k.CodigoComponente = c.Codigo AND k.n = 1
    WHERE  c.Bit_Lo_Bo = 0
    ORDER BY c.Codigo;
END
GO

-- Pares de componentes que ya se vendieron juntos (misma computadora):
-- la BLL lo usa para subir la probabilidad de venta de un combo.
CREATE OR ALTER PROCEDURE sp_Reporte_ComponentesVendidosJuntos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.CodigoComponente AS CodigoA, b.CodigoComponente AS CodigoB, COUNT(*) AS Veces
    FROM   Ventas v
    JOIN   ComputadoraComponentes a ON a.IdComputadora = v.IdComputadora
    JOIN   ComputadoraComponentes b ON b.IdComputadora = v.IdComputadora
                                   AND a.CodigoComponente < b.CodigoComponente
    WHERE  v.Estado <> 4
    GROUP BY a.CodigoComponente, b.CodigoComponente;
END
GO

PRINT '42_reporte_combos_stock_parado.sql aplicado.';
GO
