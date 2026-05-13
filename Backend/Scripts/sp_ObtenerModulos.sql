CREATE PROCEDURE [dbo].[sp_ObtenerModulos]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT id, name, path, icon
    FROM [dbo].[cert_modulos]
    ORDER BY id;
END
GO
