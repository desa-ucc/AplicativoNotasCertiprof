USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_ObtenerProyectosUmbrella]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.codigo_proyecto,
        MAX(m.nombre_proyecto) as nombre_proyecto,
        MAX(m.area) as area,
        ISNULL(MAX(e.estado), 'Activo') as estado_global
    FROM dbo.fact_metas_planner m
    LEFT JOIN dbo.cert_estado_proyectos e ON m.codigo_proyecto = e.codigo_proyecto
    WHERE m.codigo_proyecto IS NOT NULL AND m.codigo_proyecto <> ''
    GROUP BY m.codigo_proyecto
    ORDER BY m.codigo_proyecto;
END;
GO
