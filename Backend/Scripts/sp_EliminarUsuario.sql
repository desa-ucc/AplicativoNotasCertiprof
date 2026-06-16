CREATE PROCEDURE [dbo].[sp_EliminarUsuario]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[cert_usuarios]
    WHERE id = @Id;
END
GO
