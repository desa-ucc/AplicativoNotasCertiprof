USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_CargarMetas]
    @Area VARCHAR(255),
    @CodigoProyecto VARCHAR(255),
    @Url VARCHAR(MAX),
    @NombreProyecto VARCHAR(500),
    @MesCierre VARCHAR(100),
    @Responsable VARCHAR(255),
    @EtapaActividadesTareas VARCHAR(MAX),
    @UsuarioCarga VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.cert_metas (
        area,
        codigo_proyecto,
        url,
        nombre_proyecto,
        mes_cierre,
        responsable,
        etapa_actividades_tareas,
        usuario_carga,
        fecha_carga
    )
    VALUES (
        @Area,
        @CodigoProyecto,
        @Url,
        @NombreProyecto,
        @MesCierre,
        @Responsable,
        @EtapaActividadesTareas,
        @UsuarioCarga,
        DATEADD(hour, -6, GETUTCDATE())
    );
END;
GO
