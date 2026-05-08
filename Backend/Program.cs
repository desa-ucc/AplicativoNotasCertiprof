using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Here we use SQL Server. Ensure the connection string is provided in appsettings.json or Environment Variables.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<Backend.Repositories.IUploadHistoryRepository, Backend.Repositories.UploadHistoryRepository>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add basic JWT Authentication for RBAC
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // In a real scenario, configure Authority and Audience here.
        // For demonstration, we allow any token or require a specific setup.
        options.RequireHttpsMetadata = false;
        var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "FallbackDevKeyThatMustBeReplacedInProdForSecurity1234567890";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DocentePolicy", policy => policy.RequireRole("Docente", "Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure cert_registros exists via raw SQL script
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='cert_registros' AND xtype='U')
            BEGIN
                CREATE TABLE cert_registros (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    status NVARCHAR(255),
                    percentage NVARCHAR(255),
                    first_name NVARCHAR(255),
                    last_name NVARCHAR(255),
                    email NVARCHAR(255),
                    certification_name NVARCHAR(255),
                    created_at DATETIME,
                    cedula NVARCHAR(255)
                )
            END
        ";
        context.Database.ExecuteSqlRaw(createTableSql);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the cert_registros table.");
    }
}

app.Run();
