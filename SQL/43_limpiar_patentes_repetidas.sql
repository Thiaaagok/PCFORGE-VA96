-- ============================================================
--  43_limpiar_patentes_repetidas.sql
--  Quita las patentes REPETIDAS dentro de un mismo rol o familia.
--
--  El sistema no permite que una patente esté dos veces en el mismo rol
--  (patrón Composite): si un rol tiene una patente asignada en forma directa
--  y además la recibe por una de sus familias, al iniciar sesión aparecía
--  "No se puede agregar: la patente ... ya está contenida en este rol".
--
--  Este script borra solo la asignación DIRECTA sobrante:
--    · RolPatentes      cuando la patente ya llega por las familias del rol.
--    · FamiliaPatentes  cuando la patente ya llega por las subfamilias.
--  Ningún rol ni familia pierde permisos: siguen teniendo la patente por la familia.
--
--  Es idempotente: se puede ejecutar todas las veces que haga falta.
--  Si la base ya tenía dígitos verificadores calculados, después de correrlo
--  el sistema puede avisar "Inconsistencia de datos" al ingresar: elegí
--  "Recalcular el DV" (el cambio es legítimo).
-- ============================================================

SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    -- ── 1. Patentes que cada rol recibe por sus familias (con subfamilias) ──
    DECLARE @PorFamiliaDeRol TABLE (IdRol NVARCHAR(450), IdPatente NVARCHAR(100), PRIMARY KEY (IdRol, IdPatente));
    ;WITH Arbol AS (
        SELECT rf.IdRol, rf.IdFamilia FROM RolFamilias rf
        UNION ALL
        SELECT a.IdRol, ff.IdHijo FROM FamiliaFamilias ff INNER JOIN Arbol a ON ff.IdPadre = a.IdFamilia
    )
    INSERT INTO @PorFamiliaDeRol (IdRol, IdPatente)
    SELECT DISTINCT a.IdRol, fp.IdPatente
    FROM   Arbol a INNER JOIN FamiliaPatentes fp ON fp.IdFamilia = a.IdFamilia
    OPTION (MAXRECURSION 100);

    DELETE rp
    FROM   RolPatentes rp
    INNER JOIN @PorFamiliaDeRol x ON x.IdRol = rp.IdRol AND x.IdPatente = rp.IdPatente;
    DECLARE @BorradasRol INT = @@ROWCOUNT;

    -- ── 2. Patentes que cada familia recibe por sus subfamilias ─────────────
    DECLARE @PorSubfamilia TABLE (IdFamilia NVARCHAR(450), IdPatente NVARCHAR(100), PRIMARY KEY (IdFamilia, IdPatente));
    ;WITH Arbol AS (
        SELECT ff.IdPadre AS IdRaiz, ff.IdHijo AS IdFamilia FROM FamiliaFamilias ff
        UNION ALL
        SELECT a.IdRaiz, ff.IdHijo FROM FamiliaFamilias ff INNER JOIN Arbol a ON ff.IdPadre = a.IdFamilia
    )
    INSERT INTO @PorSubfamilia (IdFamilia, IdPatente)
    SELECT DISTINCT a.IdRaiz, fp.IdPatente
    FROM   Arbol a INNER JOIN FamiliaPatentes fp ON fp.IdFamilia = a.IdFamilia
    OPTION (MAXRECURSION 100);

    DELETE fp
    FROM   FamiliaPatentes fp
    INNER JOIN @PorSubfamilia x ON x.IdFamilia = fp.IdFamilia AND x.IdPatente = fp.IdPatente;
    DECLARE @BorradasFamilia INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT 'Limpieza de patentes repetidas completada.';
    PRINT '  Asignaciones directas quitadas de roles:    ' + CAST(@BorradasRol AS VARCHAR(10));
    PRINT '  Asignaciones directas quitadas de familias: ' + CAST(@BorradasFamilia AS VARCHAR(10));

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR('Error limpiando patentes repetidas: %s', 16, 1, @Msg);
END CATCH
GO