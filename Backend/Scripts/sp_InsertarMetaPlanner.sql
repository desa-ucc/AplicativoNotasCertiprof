USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_InsertarMetaPlanner]
    @Area VARCHAR(255),
    @CodigoProyecto VARCHAR(255),
    @Url VARCHAR(MAX),
    @NombreProyecto VARCHAR(500),
    @MesDeCierre VARCHAR(100),
    @Responsable VARCHAR(255),
    @EtapaActividadTarea VARCHAR(MAX),
    @Mes VARCHAR(100),
    @MesUd VARCHAR(100),
    @Anio VARCHAR(4),
    @SolicitudesCambio VARCHAR(MAX),
    @IdPlannerTask NVARCHAR(255),
    @EstadoBucket NVARCHAR(255),
    @FechaExpectativa DATE,
    @FechaCierreReal DATE,
    @ProyectoUmbrella NVARCHAR(255),
    @CorreosAutorizados NVARCHAR(MAX),
    @UsuarioCarga VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.fact_metas_planner (
        area,
        codigo_proyecto,
        url,
        nombre_proyecto,
        mes_de_cierre,
        responsable,
        etapa_actividad_tarea,
        mes,
        mes_ud,
        anio,
        solicitudes_cambio,
        id_planner_task,
        estado_bucket,
        fecha_expectativa,
        fecha_cierre_real,
        proyecto_umbrella,
        correos_autorizados,
        usuario_carga,
        fecha_carga
    )
    VALUES (
        @Area,
        @CodigoProyecto,
        @Url,
        @NombreProyecto,
        @MesDeCierre,
        @Responsable,
        @EtapaActividadTarea,
        @Mes,
        @MesUd,
        @Anio,
        @SolicitudesCambio,
        @IdPlannerTask,
        @EstadoBucket,
        @FechaExpectativa,
        @FechaCierreReal,
        @ProyectoUmbrella,
        @CorreosAutorizados,
        @UsuarioCarga,
        DATEADD(hour, -6, GETUTCDATE())
    );
END;
GO
