import re

with open('Backend/Controllers/PlannerMetasController.cs', 'r') as f:
    content = f.read()

new_endpoints = """
        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpGet("projects")]
        public async Task<IActionResult> GetUmbrellaProjects()
        {
            var records = new System.Collections.Generic.List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "v_pry_ProyectosPrincipales";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new
                            {
                                codigo_proyecto = reader.IsDBNull(reader.GetOrdinal("codigo_proyecto")) ? null : reader.GetString(reader.GetOrdinal("codigo_proyecto")),
                                nombre_proyecto = reader.IsDBNull(reader.GetOrdinal("nombre_proyecto")) ? null : reader.GetString(reader.GetOrdinal("nombre_proyecto")),
                                area = reader.IsDBNull(reader.GetOrdinal("area")) ? null : reader.GetString(reader.GetOrdinal("area")),
                                estado_global = reader.IsDBNull(reader.GetOrdinal("estado_global")) ? null : reader.GetString(reader.GetOrdinal("estado_global"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                // Fallback for dev if table doesn't exist
                if (ex.Message.Contains("Could not find stored procedure") || ex.Message.Contains("Invalid object name"))
                {
                    return Ok(new object[] { });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }

        public class UpdateProjectStatusRequest
        {
            public string Estado { get; set; } = string.Empty;
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPut("projects/{codigo}/status")]
        public async Task<IActionResult> UpdateProjectStatus(string codigo, [FromBody] UpdateProjectStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(request.Estado))
                return BadRequest(new { Message = "Datos inválidos." });

            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ActualizarEstadoProyecto";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@CodigoProyecto", codigo));
                    command.Parameters.Add(new SqlParameter("@Estado", request.Estado));

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Estado actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al actualizar estado.", Detail = ex.Message });
            }
        }
"""

# Insert before N8nSyncPayload
content = content.replace("        public class N8nSyncPayload", new_endpoints + "\n        public class N8nSyncPayload")

with open('Backend/Controllers/PlannerMetasController.cs', 'w') as f:
    f.write(content)
