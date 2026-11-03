import re

with open('Backend/Controllers/PlannerMetasController.cs', 'r') as f:
    content = f.read()

new_upload = """
        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No se ha proporcionado un archivo válido." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
                return BadRequest(new { Message = "Solo se permiten archivos Excel (.xlsx)." });

            var usuarioCarga = User.Identity?.Name ?? "Sistema";

            // Read file into memory to pass it to the background task
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scopedPlannerService = scope.ServiceProvider.GetRequiredService<IPlannerService>();

                await ProcessExcelInBackground(fileBytes, usuarioCarga, scopedDbContext, scopedPlannerService, token);
            });

            return Accepted(new { Message = "El archivo se ha recibido y se está procesando en segundo plano." });
        }

        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rowCount = worksheet.RowsUsed().Count();

                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await dbContext.Database.OpenConnectionAsync(token);
                }

                for (int i = 2; i <= rowCount; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var row = worksheet.Row(i);
                    var area = row.Cell(1).GetString();
                    var codigoProyecto = row.Cell(2).GetString();

                    if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(codigoProyecto)) continue;

                    var url = row.Cell(3).GetString();
                    var nombreProyecto = row.Cell(4).GetString();
                    var mesDeCierre = row.Cell(5).GetString();
                    var responsable = row.Cell(6).GetString();
                    var etapaActividadTarea = row.Cell(7).GetString();
                    var mes = row.Cell(8).GetString();
                    var mesUd = row.Cell(9).GetString();
                    var anio = row.Cell(10).GetString();
                    var solicitudesCambio = row.Cell(11).GetString();

                    string plannerTaskId = null;
                    int maxRetries = 3;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            // Process 1 requirement: Generate Planner Task ID with retry logic
                            plannerTaskId = await plannerService.CreateTaskAsync(nombreProyecto);
                            break;
                        }
                        catch (Exception)
                        {
                            if (retry == maxRetries - 1)
                            {
                                // Log failure, in a real app we might write to an error log table
                                Console.WriteLine($"Failed to create Planner task for project {nombreProyecto} after {maxRetries} retries.");
                            }
                            await Task.Delay(1000 * (retry + 1), token); // exponential-ish backoff
                        }
                    }

                    using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "sp_InsertarMetaPlanner";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        command.Parameters.Add(new SqlParameter("@Area", string.IsNullOrWhiteSpace(area) ? DBNull.Value : area));
                        command.Parameters.Add(new SqlParameter("@CodigoProyecto", string.IsNullOrWhiteSpace(codigoProyecto) ? DBNull.Value : codigoProyecto));
                        command.Parameters.Add(new SqlParameter("@Url", string.IsNullOrWhiteSpace(url) ? DBNull.Value : url));
                        command.Parameters.Add(new SqlParameter("@NombreProyecto", string.IsNullOrWhiteSpace(nombreProyecto) ? DBNull.Value : nombreProyecto));
                        command.Parameters.Add(new SqlParameter("@MesDeCierre", string.IsNullOrWhiteSpace(mesDeCierre) ? DBNull.Value : mesDeCierre));
                        command.Parameters.Add(new SqlParameter("@Responsable", string.IsNullOrWhiteSpace(responsable) ? DBNull.Value : responsable));
                        command.Parameters.Add(new SqlParameter("@EtapaActividadTarea", string.IsNullOrWhiteSpace(etapaActividadTarea) ? DBNull.Value : etapaActividadTarea));
                        command.Parameters.Add(new SqlParameter("@Mes", string.IsNullOrWhiteSpace(mes) ? DBNull.Value : mes));
                        command.Parameters.Add(new SqlParameter("@MesUd", string.IsNullOrWhiteSpace(mesUd) ? DBNull.Value : mesUd));
                        command.Parameters.Add(new SqlParameter("@Anio", string.IsNullOrWhiteSpace(anio) ? DBNull.Value : anio));
                        command.Parameters.Add(new SqlParameter("@SolicitudesCambio", string.IsNullOrWhiteSpace(solicitudesCambio) ? DBNull.Value : solicitudesCambio));
                        command.Parameters.Add(new SqlParameter("@IdPlannerTask", string.IsNullOrWhiteSpace(plannerTaskId) ? DBNull.Value : plannerTaskId));
                        command.Parameters.Add(new SqlParameter("@EstadoBucket", string.IsNullOrWhiteSpace(plannerTaskId) ? "Error" : "En blanco"));
                        command.Parameters.Add(new SqlParameter("@FechaExpectativa", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@FechaCierreReal", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@ProyectoUmbrella", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@CorreosAutorizados", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@UsuarioCarga", usuarioCarga));

                        try
                        {
                            await command.ExecuteNonQueryAsync(token);
                        }
                        catch (Exception ex)
                        {
                            if(ex.Message.Contains("Could not find stored procedure")) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background processing error: {ex.Message}");
            }
        }
"""

content = re.sub(r'\[Backend\.Attributes\.PermissionAuthorize\("/process2"\)\]\s+\[HttpPost\("upload"\)\].*?(?=\s+public class N8nSyncPayload)', new_upload, content, flags=re.DOTALL)

with open('Backend/Controllers/PlannerMetasController.cs', 'w') as f:
    f.write(content)
