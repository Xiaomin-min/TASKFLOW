// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskFlowApi.Data; // DbContext
using TaskFlowApi.Services; // Servicios

var builder = WebApplication.CreateBuilder(args);

// --- 1. Registrar DbContext ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- 2. Registrar Servicios ---
// Usamos Scoped para servicios que usan DbContext (DbContext es Scoped por defecto)
builder.Services.AddScoped<AuthService>();
// Podríamos crear un TareaService para encapsular la lógica de tareas
// builder.Services.AddScoped<TareaService>();
builder.Services.AddScoped<JwtService>(); // Scoped o Singleton está bien
builder.Services.AddSingleton<MotivacionService>(); // No usa DB, Singleton ok

// --- 3. Configurar Controladores y Explorador de API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 4. Configurar Swagger con Soporte para Autenticación JWT ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // ... (configuración idéntica a la respuesta anterior) ...
        Description = "Autenticación JWT (Bearer). Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, // Cambiado a Http para mejor estándar
        Scheme = "bearer", // Esquema en minúsculas
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});


// --- 5. Configurar Autenticación JWT ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // True en producción
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1) // Pequeño margen de tiempo permitido
    };
});

// --- 6. Configurar Autorización (si es necesario) ---
builder.Services.AddAuthorization();

var app = builder.Build();

// --- 7. Configurar Pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API V1");
        c.ConfigObject.AdditionalItems["persistAuthorization"] = "true"; // Mantiene token en Swagger UI
    });
    app.UseDeveloperExceptionPage(); // Muestra errores detallados en desarrollo
}

app.UseHttpsRedirection();

// **ORDEN IMPORTANTE**
app.UseAuthentication(); // Identifica al usuario
app.UseAuthorization();  // Verifica permisos

app.MapControllers(); // Mapea rutas a controladores

app.Run();