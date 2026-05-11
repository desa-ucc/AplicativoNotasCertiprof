USE CERTIFICACIONES_BD;
GO

ALTER PROCEDURE [dbo].[sp_ObtenerCedulaPorCorreo]
    @Correos NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Regla de Prefijos: Crear tabla temporal con el prefijo cert_
    IF OBJECT_ID('tempdb..#cert_correos_temp') IS NOT NULL
        DROP TABLE #cert_correos_temp;

    CREATE TABLE #cert_correos_temp (
        CorreoLimpio NVARCHAR(255)
    );

    -- 2. Limpieza de datos en la separación
    INSERT INTO #cert_correos_temp (CorreoLimpio)
    SELECT DISTINCT
        LTRIM(RTRIM(LOWER(value)))
    FROM STRING_SPLIT(@Correos, ',')
    WHERE value IS NOT NULL AND LTRIM(RTRIM(value)) <> '';

    -- 3. Reescribir la búsqueda utilizando LEFT JOIN para traer información completa
    SELECT
        c.CorreoLimpio AS EmailBuscado,
        COALESCE(m.M12CAR, p.pla20ced, 'No está dentro del registro') AS Cedula,
        COALESCE(m.m12emi, p.pla20emi, c.CorreoLimpio) AS EmailEncontrado,

        -- Datos adicionales expandidos según los requerimientos de "mostrar toda la información"
        COALESCE(m.M12NOM, p.pla20nom, '') AS NombreCompleto,
        COALESCE(m.M12EST, p.pla20est, '') AS Estado,
        m.M12FEC AS FechaIngreso,
        p.pla20fec AS FechaNacimiento,
        COALESCE(m.M12PUE, p.pla20pue, '') AS Puesto,
        COALESCE(m.M12DEP, p.pla20dep, '') AS Departamento

    FROM #cert_correos_temp c
    -- Cruce con tabla de estudiantes (M12ARC) aplicando limpieza en la condición
    LEFT JOIN AVATAR_TEST_03.dbo.M12ARC m
        ON c.CorreoLimpio = LTRIM(RTRIM(LOWER(m.m12emi)))
    -- Cruce con tabla de administrativos (PLA20ARC) aplicando limpieza, si no se encontró en la primera
    LEFT JOIN AVATAR_TEST_03.dbo.PLA20ARC p
        ON c.CorreoLimpio = LTRIM(RTRIM(LOWER(p.pla20emi)))
        AND m.M12CAR IS NULL;

    DROP TABLE #cert_correos_temp;
END
GO
