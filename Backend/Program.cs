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
    options.AddPolicy("DocentePolicy", policy => policy.RequireRole("Docente", "Administrador"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Administrador"));
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

app.Run();
