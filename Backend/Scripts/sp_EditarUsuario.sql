CREATE PROCEDURE [dbo].[sp_EditarUsuario]
    @Id INT,
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(255) = NULL,
    @RolId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PasswordHash IS NOT NULL AND @PasswordHash <> ''
    BEGIN
        UPDATE [dbo].[cert_usuarios]
        SET username = @Username,
            password_hash = @PasswordHash,
            rol_id = @RolId
        WHERE id = @Id;
    END
    ELSE
    BEGIN
        UPDATE [dbo].[cert_usuarios]
        SET username = @Username,
            rol_id = @RolId
        WHERE id = @Id;
    END
END
GO
