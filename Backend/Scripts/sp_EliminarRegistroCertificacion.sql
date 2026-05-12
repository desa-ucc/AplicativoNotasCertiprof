CREATE PROCEDURE [dbo].[sp_EliminarRegistroCertificacion]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[cert_registros]
    WHERE [id] = @Id;
END
GO
