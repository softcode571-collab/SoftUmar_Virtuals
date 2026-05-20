using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SoftUmar_Virtuals.API.Middleware;
using SoftUmar_Virtuals.Infrastructure.DependencyInjection;
using SoftUmar_Virtuals.Infrastructure.Security;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SoftUmar_Virtuals.Application.DependencyInjection;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


// =========================
// DEPENDENCY INJECTION
// =========================

builder.Services.AddHttpContextAccessor();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApplication();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();


// =========================
// JWT SETTINGS
// =========================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>();


// =========================
// JWT AUTHENTICATION
// =========================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;

        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings!.Issuer,

                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ClockSkew = TimeSpan.Zero
            };
    });


// =========================
// AUTHORIZATION POLICIES
// =========================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin"));

    options.AddPolicy("UserOnly",
        policy => policy.RequireRole("User"));
});


// =========================
// RATE LIMITING
// =========================

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", config =>
    {
        config.PermitLimit = 10;

        config.Window = TimeSpan.FromMinutes(1);

        config.QueueProcessingOrder =
            QueueProcessingOrder.OldestFirst;

        config.QueueLimit = 2;
    });
});


// =========================
// SERILOG
// =========================

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/app-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();


// =========================
// CORS
// =========================

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// =========================
// HEALTH CHECKS
// =========================

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("VirtualNumber")!)
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!);


// =========================
// HANGFIRE
// =========================

//builder.Services.AddHangfire(config =>
//{
//    config.SetDataCompatibilityLevel(
//        CompatibilityLevel.Version_180);

//    config.UseSimpleAssemblyNameTypeSerializer();

//    config.UseRecommendedSerializerSettings();

//    config.UseSqlServerStorage(
//        builder.Configuration.GetConnectionString("VirtualNumber"));
//});

//builder.Services.AddHangfireServer();


// =========================
// SWAGGER
// =========================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VirtualNumber API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your token}"
        });
});

// =========================
// BUILD APP
// =========================

var app = builder.Build();


// =========================
// DEVELOPMENT TOOLS
// =========================

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}


// =========================
// MIDDLEWARE PIPELINE
// =========================

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


// =========================
// HEALTH CHECKS
// =========================

app.MapHealthChecks("/health");


// =========================
// HANGFIRE DASHBOARD
// =========================

//app.UseHangfireDashboard();


// =========================
// RUN APP
// =========================

app.Run();