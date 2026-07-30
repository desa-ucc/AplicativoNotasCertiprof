# Manual de Usuario y Operación: AVATAR Admin

Bienvenido al manual oficial de **AVATAR Admin**. Este documento está diseñado para ayudarle a conocer, comprender y utilizar todas las funcionalidades del sistema de manera rápida y amigable.

---

## 1. Introducción

**¿Qué es AVATAR Admin?**
AVATAR Admin es una plataforma web desarrollada para facilitar el procesamiento, análisis y resguardo de las calificaciones de certificaciones (como Certiprof).

**¿Para qué sirve?**
Permite tomar un archivo con resultados de estudiantes, filtrarlos según la certificación específica, visualizar estadísticas de aprobación al instante y, finalmente, generar un **Acta Auxiliar** formal (en formato Excel) lista para ser enviada o almacenada.

**¿A quiénes va dirigido?**
Este sistema está pensado principalmente para el personal docente y administrativo responsable de gestionar calificaciones y certificaciones de los estudiantes.

---

## 2. Guía de Acceso

Para comenzar a utilizar el sistema, debe iniciar sesión. Este mecanismo protege la información sensible de los estudiantes.

1. **Ingreso a la plataforma:** Abra su navegador web (se recomienda Google Chrome) e ingrese a la dirección web proporcionada por el departamento de tecnología (por ejemplo, `http://localhost:4200`).
2. **Pantalla de Inicio de Sesión:**
   - Verá dos campos: **Usuario** y **Contraseña**.
   - Ingrese los datos de acceso proporcionados por su administrador (ej. usuario: `admin` o `docente`).
   - Haga clic en el botón de ingresar.
3. **Roles y Permisos:** Dependiendo de si usted tiene el rol de *Docente* o *Administrador*, el sistema ajustará lo que puede ver. Por ejemplo, los administradores pueden ver el historial completo de todas las cargas en el sistema, mientras que los docentes ven únicamente los archivos que ellos mismos han subido.

---

## 3. Recorrido por Módulos y Pantallas (Paso a Paso)

Una vez iniciada la sesión, en la parte superior verá un menú principal con las siguientes opciones: **Cargar Archivo** e **Historial**, además del botón para **Cerrar Sesión**.

### 3.1. Cargar Archivo (Procesamiento de Certificaciones)
Esta es la pantalla principal donde usted procesará los resultados de los estudiantes.

* **Paso 1: Seleccionar el archivo.**
  - Puede **arrastrar y soltar** el archivo CSV con las calificaciones directamente en el cuadro punteado que aparece en pantalla, o bien, hacer clic en "Seleccionar Archivo" para buscarlo en su computadora.
* **Paso 2: Previsualización y Análisis.**
  - Al cargar el archivo, el sistema leerá la información de inmediato.
  - Verá un **gráfico de anillo (Doughnut)** colorido en la pantalla que le indicará de forma resumida cuántos estudiantes han **Aprobado** (en color verde) y cuántos han **Reprobado** (en color rojo).
* **Paso 3: Llenar los datos del reporte.**
  - **Código del Curso:** Escriba el código oficial del curso al que pertenecen estas calificaciones (Ej: `CE0501`).
  - **Nombre de Certificación:** Ingrese el nombre exacto de la certificación que desea filtrar de ese archivo (Ej: `Generative AI Professional Certification - GAIPC`). El sistema ignorará cualquier estudiante que no pertenezca a esta certificación.
* **Paso 4: Procesar y Guardar.**
  - Haga clic en el botón **"Procesar"**.
  - Espere unos segundos. El sistema le mostrará un mensaje indicando que el archivo fue procesado con éxito.
* **Paso 5: Generar el Acta.**
  - Tras el procesamiento exitoso, se habilitará la opción para **"Generar Acta"**.
  - Al hacer clic, se descargará automáticamente a su computadora un archivo Excel (`.xlsx`) llamado `Acta_Auxiliar_CE0501...`, el cual contiene la lista final estandarizada y ordenada.

### 3.2. Historial (Historial de Cargas)
Este módulo sirve como bitácora de todo el trabajo que ha realizado.

* **¿Qué puedo hacer aquí?**
  - Ver un listado de todos los archivos que ha procesado en el pasado (con su fecha, código de curso y cantidad de registros válidos procesados).
* **Paso a paso para re-descargar un acta:**
  - Si perdió el archivo Excel que generó ayer, ¡no se preocupe!
  - Busque la fila correspondiente a la carga en la tabla del historial.
  - Haga clic en el botón de descarga situado a la derecha de esa fila.
  - El sistema le generará y descargará el archivo Excel de nuevo, tal cual estaba.

---

## 4. Preguntas Frecuentes / Resolución de Dudas Comunes

* **¿Qué sucede si introduzco un usuario o contraseña incorrectos?**
  - El sistema mostrará un mensaje en rojo indicando "Credenciales inválidas". Verifique sus datos e intente de nuevo. Si olvidó su contraseña, contacte a soporte técnico.
* **Intento arrastrar un archivo Excel (.xlsx) en la carga y no funciona bien, ¿por qué?**
  - Actualmente, la funcionalidad de previsualización en pantalla y carga inicial está optimizada para archivos **CSV (valores separados por comas)**. Si tiene un Excel, guárdelo primero como CSV antes de subirlo.
* **El sistema me dice que el archivo fue "procesado con éxito", pero el Acta descargada está vacía.**
  - Esto ocurre casi siempre porque el texto ingresado en el campo **"Nombre de Certificación"** no coincide con lo que está escrito dentro del archivo CSV. Asegúrese de escribir el nombre exacto de la certificación para que el filtro inteligente encuentre a los estudiantes.
* **¿Cuándo se considera a un estudiante como Aprobado o Reprobado en el gráfico?**
  - Para fines visuales rápidos, el sistema asume que la nota de aprobación es **60 o superior**.
* **¿Qué pasa si presiono "Cerrar Sesión"?**
  - Saldrá del sistema de forma segura. Se requerirá iniciar sesión nuevamente para acceder a los datos.

---

## 5. Anexo Técnico (Para referencia interna IT)

A continuación, se detalla un resumen de la interacción entre los módulos de la aplicación, las entidades de Entity Framework (Base de Datos) y la lógica de backend utilizada en cada caso:

| Módulo (Frontend) | Funcionalidad Principal | Tablas Afectadas (EF Core Models) | Endpoints (API) | Métodos Principales (Backend / Services) |
| :--- | :--- | :--- | :--- | :--- |
| **Login** | Autenticación de usuarios y asignación de JWT. | `Users` | `POST /api/auth/login` | `IAuthService.VerifyPassword()`<br>`IAuthService.GenerateJwtToken()` |
| **Cargar Archivo (Upload)** | Parseo local (PapaParse), procesamiento en DB (CsvHelper), y filtro inteligente (Upsert). | `UploadHistories`<br>`CertiprofRecords` | `POST /api/certiprof/process-report` | `FileProcessingService.ProcessReportAsync()` (Lógica de Upsert por lote). |
| **Historial (History)** | Consulta de lotes procesados según rol (Admin ve todos, Docente ve los propios). | `UploadHistories` | `GET /api/certiprof/history` | `UploadHistoryRepository.GetAllAsync()`<br>`UploadHistoryRepository.GetByUsernameAsync()` |
| **Descarga de Acta** | Generación de archivo Excel desde la DB en memoria usando ClosedXML. | `UploadHistories`<br>`CertiprofRecords` (Lectura) | `GET /api/certiprof/export-avatar/{id}` | `FileProcessingService.GenerateAvatarActAsync()` |
