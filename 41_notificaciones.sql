-- ============================================================
--  41_notificaciones.sql
--  NOTIFICACIONES: avisos que nacen de acciones del sistema
--  (venta señada, control de calidad rechazado, cotización
--  recibida, mercadería recibida...) o de situaciones detectadas
--  una sola vez (atrasos, stock bajo).
--
--    Notificaciones        el evento, dirigido a una patente
--                          (Patente = Patentes.Id; NULL = todos).
--    NotificacionesLeidas  qué usuario la marcó como leída.
--
--  La aplicación crea las mismas tablas si no existen; este
--  script queda para instalar la base completa a mano.
--  No son tablas protegidas por DV (igual que Bitacora).
--
--  Es idempotente: se puede correr varias veces.
-- ============================================================

SET NOCOUNT ON;
GO

IF OBJECT_ID('Notificaciones') IS NULL
CREATE TABLE Notificaciones (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Fecha      DATETIME      NOT NULL DEFAULT GETDATE(),
    Tipo       INT           NOT NULL,
    Datos      NVARCHAR(400) NULL,
    Patente    NVARCHAR(100) NULL,
    Destino    NVARCHAR(80)  NULL,
    ClaveUnica NVARCHAR(150) NULL,
    DniOrigen  NVARCHAR(20)  NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Notificaciones_ClaveUnica')
    CREATE UNIQUE INDEX UX_Notificaciones_ClaveUnica ON Notificaciones(ClaveUnica) WHERE ClaveUnica IS NOT NULL;
GO

IF OBJECT_ID('NotificacionesLeidas') IS NULL
CREATE TABLE NotificacionesLeidas (
    IdNotificacion INT          NOT NULL REFERENCES Notificaciones(Id) ON DELETE CASCADE,
    Dni            NVARCHAR(20) NOT NULL,
    FechaLectura   DATETIME     NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (IdNotificacion, Dni)
);
GO
