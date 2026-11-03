USE [CERTIFICACIONES_BD]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[cert_metas](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[area] [varchar](255) NULL,
	[codigo_proyecto] [varchar](255) NULL,
	[url] [varchar](max) NULL,
	[nombre_proyecto] [varchar](500) NULL,
	[mes_cierre] [varchar](100) NULL,
	[responsable] [varchar](255) NULL,
	[etapa_actividades_tareas] [varchar](max) NULL,
	[fecha_carga] [datetime] NULL DEFAULT (dateadd(hour,(-6),getutcdate())),
	[usuario_carga] [varchar](255) NULL,
 CONSTRAINT [PK_cert_metas] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
