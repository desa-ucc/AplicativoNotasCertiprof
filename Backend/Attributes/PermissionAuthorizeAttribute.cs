using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Backend.Attributes
{
    public class PermissionAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string _modulePath;

        public PermissionAuthorizeAttribute(string modulePath)
        {
            _modulePath = modulePath;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetService<AppDbContext>();
            if (dbContext == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            bool hasPermission = false;
            try
            {
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        SELECT 1
                        FROM cert_permisos p
                        JOIN cert_modulos m ON p.modulo_id = m.id
                        JOIN cert_roles r ON p.rol_id = r.id
                        WHERE r.nombre_rol = @RoleName AND m.ruta = @ModulePath";

                    var pRole = command.CreateParameter();
                    pRole.ParameterName = "@RoleName";
                    pRole.Value = roleClaim;
                    command.Parameters.Add(pRole);

                    var pModule = command.CreateParameter();
                    pModule.ParameterName = "@ModulePath";
                    pModule.Value = _modulePath;
                    command.Parameters.Add(pModule);

                    if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await dbContext.Database.OpenConnectionAsync();
                    }

                    var result = await command.ExecuteScalarAsync();
                    hasPermission = result != null;
                }
            } catch (System.Exception) {
                context.Result = new StatusCodeResult(500);
                return;
            }

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
