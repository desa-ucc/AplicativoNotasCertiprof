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

builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

// Add basic JWT Authentication for RBAC
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // In a real scenario, configure Authority and Audience here.
        // For demonstration, we allow any token or require a specific setup.
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            SignatureValidator = delegate(string token, Microsoft.IdentityModel.Tokens.TokenValidationParameters parameters)
            {
                var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
                return jwt;
            }
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

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations automatically on startup for development purposes
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); // Use EnsureCreated for simplicity, or Migrate() if using migrations
    }
    catch (Exception ex)
    {
        // Log error
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}


app.Run();
