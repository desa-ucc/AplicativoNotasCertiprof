# Documentación Técnica: AVATAR Admin

## 1. Resumen del Proyecto

**AVATAR Admin** es una plataforma web encargada de procesar archivos de resultados de certificaciones (Certiprof), filtrarlos inteligentemente, almacenar el historial de cargas, mostrar métricas visuales y generar actas auxiliares en formato Microsoft Excel (.xlsx).

**Tecnologías, frameworks y librerías utilizadas:**
- **Frontend:** Angular 17, Tailwind CSS (estilos modernos), ngx-charts (visualización de gráficos de aprobados/reprobados), y PapaParse (parseo inicial de CSV en el navegador).
- **Backend:** .NET 8 Web API, C#, Entity Framework Core (ORM), SQL Server.
- **Procesamiento y Documentos:** CsvHelper (lectura y normalización de CSV), ClosedXML (generación rápida de Excel en memoria).
- **Seguridad:** JWT (JSON Web Tokens) para autenticación y autorización (RBAC con roles como Admin y Docente).
- **Infraestructura:** Docker y Docker Compose para orquestación sencilla en ambientes locales/desarrollo (contenedores para base de datos, backend y frontend).

---

## 2. Arquitectura y Estructura de Archivos

La arquitectura sigue el modelo cliente-servidor (SPA + RESTful API), segmentado en dos proyectos principales:

### `Frontend/` (Angular 17)
- **`src/app/`**: Directorio principal.
  - **`app.component.*`**: Layout central que contiene el componente de navegación (Navbar) y el manejador del enrutamiento.
  - **`login/`**: Vista de autenticación donde se solicitan las credenciales.
  - **`certiprof/`**: Módulo/componente de carga de datos ("Upload"). Incluye una zona de *drag-and-drop*, una previsualización de datos parseados localmente y un gráfico Doughnut generado con `ngx-charts` sobre la distribución de las notas.
  - **`history/`**: Vista del historial de cargas donde los administradores/docentes pueden revisar envíos pasados y descargar el acta en cualquier momento.
  - **`auth.guard.ts` / `auth.interceptor.ts`**: Lógica de seguridad que protege las rutas de UI e inyecta el token JWT en las solicitudes HTTP respectivamente.

### `Backend/` (.NET 8 Web API)
- **`Controllers/`**: Expone los endpoints HTTP.
  - `AuthController.cs`: Maneja el login y emisión de JWT.
  - `CertiprofController.cs`: Administra la subida de reportes (`process-report`), consulta de historial y exportación a Excel.
- **`Services/`**: Contiene la lógica de negocio profunda.
  - `FileProcessingService.cs`: Lógica principal para recibir el IFormFile, extraer los registros vía CsvHelper, filtrar por nombre de certificación, manejar un upsert lógico por lote y construir un documento ClosedXML para la descarga de Actas.
  - `AuthService.cs`: Lógica de validación, generación de JWT y hashing seguro de contraseñas.
- **`Repositories/`**: Capa de abstracción de datos para `UploadHistory` (sigue el Patrón Repositorio).
- **`Models/`**: Entidades del dominio (`User`, `CertiprofRecord`, `UploadHistory`).
- **`Data/`**: `AppDbContext.cs` configuración principal de Entity Framework Core.

---

## 3. Flujo de Funcionamiento (Paso a Paso)

### Arranque del sistema
1. Al levantar los contenedores (`docker-compose up -d`), se inician SQL Server, la API y la aplicación web.
2. La API en su archivo `Program.cs` comprueba si existe la estructura de base de datos usando `EnsureCreated()`. Además, inserta dos usuarios por defecto si la tabla está vacía (`admin` y `docente`).

### Flujo de datos: Entrada hasta persistencia
1. **Autenticación:** El usuario accede a la web, ingresa credenciales en el Frontend. Éstas viajan a `/api/auth/login`. El Backend valida contra la DB y devuelve un token JWT con el rol asignado. Angular almacena el token en el LocalStorage y lo incluye en todas las peticiones protegidas (vía Interceptor).
2. **Carga de Archivo (Ingreso de datos):** El usuario navega a "Cargar Archivo". Elige un archivo CSV. PapaParse lo lee localmente para alimentar el gráfico de Aprobados/Reprobados.
3. **Procesamiento en Backend:** Al presionar "Procesar", el archivo y los metadatos (ej. código de curso) se envían a `/api/certiprof/process-report`. El `FileProcessingService`:
   - Lee el CSV usando `CsvHelper`.
   - Filtra y estandariza los registros omitiendo datos inválidos y unificando el nombre de certificación buscado.
   - Crea un registro maestro (`UploadHistory`) e inserta cada fila como detalle (`CertiprofRecord`) usando EF Core.
4. **Generación de Reportes:** Cuando el usuario solicita descargar el Acta (desde la misma pantalla o desde el Historial), el frontend llama a `/api/certiprof/export-avatar/{id}`. El backend utiliza `ClosedXML` para iterar sobre los registros guardados, dar formato (columnas en negrita, mezcla de celdas), y lo retorna como un flujo binario (`FileContentResult`). El frontend convierte este flujo en un Blob que el navegador descarga.

---

## 4. Guía de Configuración y Despliegue (Setup & Deployment)

El proyecto está diseñado para ejecutarse sencillamente utilizando Docker.

### Pre-requisitos:
- Docker y Docker Compose instalados.

### Ejecución usando Docker Compose:
1. Clona el repositorio y ubícate en la raíz del proyecto.
2. Construye y levanta el entorno:
   ```bash
   docker-compose up --build -d
   ```
3. Verifica que los 3 contenedores (`sql_server`, `certiprof_backend`, `certiprof_frontend`) estén ejecutándose correctamente:
   ```bash
   docker-compose ps
   ```
4. **Accesos principales:**
   - **Frontend:** http://localhost:4200
   - **Backend API (Swagger en Dev):** http://localhost:5000/swagger
   - **SQL Server:** `localhost:1433`
5. **Credenciales predeterminadas generadas (semilla automática):**
   - **Admin:** `admin` / `admin123`
   - **Docente:** `docente` / `docente123`

### Configuración de Variables de Entorno (Producción):
Las variables están descritas en el `docker-compose.yml`, se recomiendan ajustarlas para entornos productivos:
- `DB_PASSWORD`: Clave robusta para el usuario SA del SQL Server.
- `JWT_SECRET_KEY`: Llave muy larga para la firma criptográfica de los JSON Web Tokens.

---

## 5. Puntos Clave y Consideraciones

- **Políticas de Autorización Cíclicas:** El backend implementa RBAC (Role-Based Access Control) mediante el uso de la política `"DocentePolicy"`, la cual da acceso tanto a roles "Admin" como "Docente". Si a futuro se requiere un rol exclusivo de lectura, se debe modificar/crear una nueva política en `Program.cs`.
- **Estructura EF Core (Migraciones vs EnsureCreated):** Actualmente en el arranque `Program.cs` se usa `context.Database.EnsureCreated()`. Aunque es fantástico para desarrollo y testing rápido, en producción (y de cara a la memoria del conocimiento) el equipo debe migrar este comando para utilizar `context.Database.Migrate()` luego de agregar formalmente migraciones de EF Core (`dotnet ef migrations add InitialCreate`).
- **Lógica "Upsert" de Memoria:** Durante la subida de un CSV, el backend revisa duplicados *solo en el lote actual* antes de hacer `SaveChanges()`. Si se sube el mismo archivo dos veces seguidas, se crearán dos `UploadHistory` separados.
- **Configuración CORS:** `Program.cs` permite peticiones expresamente desde `http://localhost:4200`. Si el frontend de Angular se despliega en un dominio distinto o un servidor de Nginx productivo, es **estrictamente necesario** añadir esa nueva URL en el constructor del CORS de ASP.NET Core para prevenir bloqueos del navegador.
- **Gráficos en Frontend:** Angular calcula la nota para determinar si un usuario "Aprobó" o "Reprobó" directamente asumiendo una nota >= 60. Si esta regla cambia en la lógica de negocio oficial, el componente `certiprof.component.ts` debe ser ajustado.
