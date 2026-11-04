USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[fact_metas_planner](
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
 CONSTRAINT [PK_fact_metas_planner] PRIMARY KEY CLUSTERED
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
