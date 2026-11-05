USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_UpsertMetaPlanner]
    @IdPlannerTask NVARCHAR(255),
    @Area VARCHAR(255) = NULL,
    @CodigoProyecto VARCHAR(255) = NULL,
    @Url VARCHAR(MAX) = NULL,
    @NombreProyecto VARCHAR(500) = NULL,
    @MesDeCierre VARCHAR(100) = NULL,
    @Responsable VARCHAR(255) = NULL,
    @EtapaActividadTarea VARCHAR(MAX) = NULL,
    @Mes VARCHAR(100) = NULL,
    @MesUd VARCHAR(100) = NULL,
    @Anio VARCHAR(4) = NULL,
    @SolicitudesCambio VARCHAR(MAX) = NULL,
    @EstadoBucket NVARCHAR(255) = NULL,
    @FechaExpectativa DATE = NULL,
    @FechaCierreReal DATE = NULL,
    @ProyectoUmbrella NVARCHAR(255) = NULL,
    @CorreosAutorizados NVARCHAR(MAX) = NULL,
    @UsuarioCarga VARCHAR(255) = NULL,
    @EstadoProyecto VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.pry_metas_planner WHERE id_planner_task = @IdPlannerTask)
    BEGIN
        UPDATE dbo.pry_metas_planner
        SET
            area = ISNULL(@Area, area),
            codigo_proyecto = ISNULL(@CodigoProyecto, codigo_proyecto),
            url = ISNULL(@Url, url),
            nombre_proyecto = ISNULL(@NombreProyecto, nombre_proyecto),
            mes_de_cierre = ISNULL(@MesDeCierre, mes_de_cierre),
            responsable = ISNULL(@Responsable, responsable),
            etapa_actividad_tarea = ISNULL(@EtapaActividadTarea, etapa_actividad_tarea),
            mes = ISNULL(@Mes, mes),
            mes_ud = ISNULL(@MesUd, mes_ud),
            anio = ISNULL(@Anio, anio),
            solicitudes_cambio = ISNULL(@SolicitudesCambio, solicitudes_cambio),
            estado_bucket = ISNULL(@EstadoBucket, 'Pendiente'),
            fecha_expectativa = ISNULL(@FechaExpectativa, fecha_expectativa),
            fecha_cierre_real = ISNULL(@FechaCierreReal, fecha_cierre_real),
            proyecto_umbrella = ISNULL(@ProyectoUmbrella, proyecto_umbrella),
            correos_autorizados = ISNULL(@CorreosAutorizados, correos_autorizados),
            estado_proyecto = ISNULL(@EstadoProyecto, ISNULL(estado_proyecto, 'Activo')),
            fecha_carga = DATEADD(hour, -6, GETUTCDATE())
        WHERE id_planner_task = @IdPlannerTask;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.pry_metas_planner (
            id_planner_task,
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
            estado_bucket,
            fecha_expectativa,
            fecha_cierre_real,
            proyecto_umbrella,
            correos_autorizados,
            usuario_carga,
            estado_proyecto,
            fecha_carga
        )
        VALUES (
            @IdPlannerTask,
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
            ISNULL(@EstadoBucket, 'Pendiente'),
            @FechaExpectativa,
            @FechaCierreReal,
            @ProyectoUmbrella,
            @CorreosAutorizados,
            @UsuarioCarga,
            ISNULL(@EstadoProyecto, 'Activo'),
            DATEADD(hour, -6, GETUTCDATE())
        );
    END
END;
GO
