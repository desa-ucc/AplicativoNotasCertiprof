USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_ObtenerCedulaPorCorreo]
    @Correo VARCHAR(MAX),
    @CertificacionNombre VARCHAR(255),
    @FirstName VARCHAR(150) = NULL,
    @LastName VARCHAR(150) = NULL,
    @Percentage DECIMAL(18,2) = NULL,
    @Status VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validación de seguridad para el parámetro obligatorio
    IF @Correo IS NULL OR LTRIM(RTRIM(@Correo)) = '' RETURN;

    --------------------------------------------
    -- 1. Buscar Cédula en AVATAR (Modo House Way)
    --------------------------------------------
    DECLARE @CedulaEncontrada VARCHAR(20);

    SELECT TOP 1 @CedulaEncontrada = Cedula
    FROM (
        SELECT M12CAR AS Cedula, m12emi AS Email FROM AVATAR_TEST_03.dbo.M12ARC
        UNION ALL
        SELECT pla20ced, pla20emi FROM AVATAR_TEST_03.dbo.PLA20ARC
    ) AS BasePersonal
    WHERE Email = LTRIM(RTRIM(@Correo));

    --------------------------------------------
    -- 2. Solo insertar si hay cédula y no es duplicado
    --------------------------------------------
    IF @CedulaEncontrada IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.cert_registros
            WHERE cert_cedula = @CedulaEncontrada
            AND cert_certification_name = @CertificacionNombre
        )
        BEGIN
            INSERT INTO dbo.cert_registros (
                cert_cedula,
                cert_email,
                cert_certification_name,
                cert_first_name,
                cert_last_name,
                cert_percentage,
                cert_status,
                cert_created_at
            )
            VALUES (
                @CedulaEncontrada,
                @Correo,
                @CertificacionNombre,
                @FirstName,
                @LastName,
                @Percentage,
                ISNULL(@Status, 'Procesado'),
                GETDATE()
            );
        END
    END

    -- Retorno para control del Backend
    SELECT ISNULL(@CedulaEncontrada, 'SIN_CEDULA') AS Resultado;
END;
GO
