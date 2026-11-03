USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_ObtenerHistorialMetas]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id,
        area,
        codigo_proyecto,
        url,
        nombre_proyecto,
        mes_cierre,
        responsable,
        etapa_actividades_tareas,
        fecha_carga,
        usuario_carga
    FROM [dbo].[cert_metas]
    ORDER BY fecha_carga DESC;
END;
GO
