CREATE PROCEDURE [dbo].[sp_ActualizarPermisosRol]
    @RolId INT,
    @ModulosIds NVARCHAR(MAX) -- Comma-separated list of module IDs
AS
BEGIN
    SET NOCOUNT ON;

    -- Delete existing permissions for this role
    DELETE FROM [dbo].[cert_rol_modulos]
    WHERE rol_id = @RolId;

    -- Insert new permissions (SQL Server 2016+ supports STRING_SPLIT)
    IF @ModulosIds IS NOT NULL AND @ModulosIds <> ''
    BEGIN
        INSERT INTO [dbo].[cert_rol_modulos] (rol_id, modulo_id)
        SELECT @RolId, TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ModulosIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END
END
GO
