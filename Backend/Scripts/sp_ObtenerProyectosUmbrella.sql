USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[v_pry_ProyectosPrincipales]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.codigo_proyecto,
        MAX(m.nombre_proyecto) as nombre_proyecto,
        MAX(m.area) as area,
        ISNULL(MAX(m.estado_proyecto), 'Activo') as estado_global
    FROM dbo.pry_metas_planner m
    WHERE m.codigo_proyecto IS NOT NULL AND m.codigo_proyecto <> ''
    GROUP BY m.codigo_proyecto
    ORDER BY m.codigo_proyecto;
END;
GO
