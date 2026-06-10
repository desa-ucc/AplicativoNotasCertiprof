using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityController : ControllerBase
    {
        private readonly Data.AppDbContext _dbContext;

        public SecurityController(Data.AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ListarTodosLosModulos";
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
                                id = reader.GetInt32(reader.GetOrdinal("Id")),
                                nombre = reader.GetString(reader.GetOrdinal("NombreModulo")),
                                ruta = reader.GetString(reader.GetOrdinal("RutaModulo"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Could not find stored procedure"))
                {
                    return Ok(new[]
                    {
                        new { id = 1, nombre = "Cargar Archivo", ruta = "/upload" },
                        new { id = 2, nombre = "Historial", ruta = "/history" },
                        new { id = 3, nombre = "Seguridad", ruta = "/security" }
                    });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
