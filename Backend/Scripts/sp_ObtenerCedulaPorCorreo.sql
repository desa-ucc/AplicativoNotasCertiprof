USE CERTIFICACIONES_BD;
GO

ALTER PROCEDURE [dbo].[sp_ObtenerCedulaPorCorreo]
    @Correo VARCHAR(MAX),
    @CertificationName VARCHAR(255) = ''
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Periodo VARCHAR(4) = CAST(YEAR(GETDATE()) AS VARCHAR(4));

    IF OBJECT_ID('tempdb..#cert_correos_temp') IS NOT NULL
        DROP TABLE #cert_correos_temp;

    CREATE TABLE #cert_correos_temp (
        CorreoLimpio NVARCHAR(255)
    );

    INSERT INTO #cert_correos_temp (CorreoLimpio)
    SELECT DISTINCT
        LTRIM(RTRIM(LOWER(value)))
    FROM STRING_SPLIT(@Correo, ',')
    WHERE value IS NOT NULL AND LTRIM(RTRIM(value)) <> '';

    -- Validar y evitar duplicados por Cedula + CertificationName, insertando solo los que tienen cedula valida
    INSERT INTO dbo.cert_registros (
        status,
        percentage,
        first_name,
        last_name,
        email,
        certification_name,
        created_at,
        cedula
    )
    SELECT
        COALESCE(m.M12EST, p.pla20est, 'Activo') AS status,
        '0' AS percentage,
        '' AS first_name, -- Not mapping name to raw excel right here to respect the strict DB rules
        '' AS last_name,
        COALESCE(m.m12emi, p.pla20emi, c.CorreoLimpio) AS email,
        @CertificationName AS certification_name,
        GETDATE() AS created_at,
        COALESCE(m.M12CAR, p.pla20ced) AS cedula
    FROM #cert_correos_temp c
    LEFT JOIN AVATAR_TEST_03.dbo.M12ARC m
        ON c.CorreoLimpio = LTRIM(RTRIM(LOWER(m.m12emi)))
    LEFT JOIN AVATAR_TEST_03.dbo.PLA20ARC p
        ON c.CorreoLimpio = LTRIM(RTRIM(LOWER(p.pla20emi)))
        AND m.M12CAR IS NULL
    WHERE COALESCE(m.M12CAR, p.pla20ced) IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM dbo.cert_registros existing
        WHERE existing.cedula = COALESCE(m.M12CAR, p.pla20ced)
        AND existing.certification_name = @CertificationName
    );

    DROP TABLE #cert_correos_temp;
END;
GO
