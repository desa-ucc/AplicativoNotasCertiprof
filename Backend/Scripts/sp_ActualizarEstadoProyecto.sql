USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_ActualizarEstadoProyecto]
    @CodigoProyecto VARCHAR(255),
    @Estado VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.pry_estado_proyectos WHERE codigo_proyecto = @CodigoProyecto)
    BEGIN
        UPDATE dbo.pry_estado_proyectos
        SET estado = @Estado
        WHERE codigo_proyecto = @CodigoProyecto;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.pry_estado_proyectos (codigo_proyecto, estado)
        VALUES (@CodigoProyecto, @Estado);
    END
END;
GO
