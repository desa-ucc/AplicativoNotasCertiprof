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

    UPDATE dbo.pry_metas_planner
    SET estado_proyecto = @Estado
    WHERE codigo_proyecto = @CodigoProyecto;

END;
GO
