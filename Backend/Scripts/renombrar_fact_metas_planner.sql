USE [CERTIFICACIONES_BD]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fact_metas_planner]') AND type in (N'U'))
BEGIN
    EXEC sp_rename 'dbo.fact_metas_planner', 'pry_metas_planner';
    EXEC sp_rename 'dbo.pry_metas_planner.PK_fact_metas_planner', 'PK_pry_metas_planner', 'INDEX';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[pry_metas_planner]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[pry_metas_planner](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [area] [varchar](255) NULL,
        [codigo_proyecto] [varchar](255) NULL,
        [url] [varchar](max) NULL,
        [nombre_proyecto] [varchar](500) NULL,
        [mes_de_cierre] [varchar](100) NULL,
        [responsable] [varchar](255) NULL,
        [etapa_actividad_tarea] [varchar](max) NULL,
        [mes] [varchar](100) NULL,
        [mes_ud] [varchar](100) NULL,
        [anio] [varchar](4) NULL,
        [solicitudes_cambio] [varchar](max) NULL,

        [id_planner_task] [nvarchar](255) NULL,
        [estado_bucket] [nvarchar](255) NULL,
        [fecha_expectativa] [date] NULL,
        [fecha_cierre_real] [date] NULL,
        [proyecto_umbrella] [nvarchar](255) NULL,
        [correos_autorizados] [nvarchar](max) NULL,

        [fecha_carga] [datetime] NULL DEFAULT (dateadd(hour,(-6),getutcdate())),
        [usuario_carga] [varchar](255) NULL,
        [estado_proyecto] [varchar](50) NULL DEFAULT 'Activo',
     CONSTRAINT [PK_pry_metas_planner] PRIMARY KEY CLUSTERED
    (
        [id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT *
        FROM sys.columns
        WHERE Name = N'estado_proyecto'
        AND Object_ID = Object_ID(N'dbo.pry_metas_planner')
    )
    BEGIN
        ALTER TABLE dbo.pry_metas_planner ADD [estado_proyecto] [varchar](50) NULL DEFAULT 'Activo';
    END
END
GO
