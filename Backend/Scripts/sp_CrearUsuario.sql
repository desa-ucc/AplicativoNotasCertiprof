CREATE PROCEDURE [dbo].[sp_CrearUsuario]
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @RolId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[cert_usuarios] (username, password_hash, rol_id)
    VALUES (@Username, @PasswordHash, @RolId);
END
GO
