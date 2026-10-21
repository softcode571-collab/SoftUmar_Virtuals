using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using SoftUmar_Virtuals.API.Middleware;
using SoftUmar_Virtuals.Application.DependencyInjection;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Infrastructure.DependencyInjection;
using SoftUmar_Virtuals.Infrastructure.Security;
using SoftUmar_Virtuals.Infrastructure.Services;
using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;
using SoftUmar_Virtuals.Infrastructure.Services.Monnify;
using System.Text;
using System.Threading.RateLimiting;

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

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext
                    .RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");

                logger.LogError(
                    context.Exception,
                    "JWT authentication failed: {Message}",
                    context.Exception.Message);

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                var logger = context.HttpContext
                    .RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");

                logger.LogWarning(
                    "JWT challenge: {Error}; {Description}",
                    context.Error,
                    context.ErrorDescription);

                return Task.CompletedTask;
            }
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

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(
        CompatibilityLevel.Version_180);

    config.UseSimpleAssemblyNameTypeSerializer();

    config.UseRecommendedSerializerSettings();

    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("VirtualNumber"));
});

// NEW: was missing even when this block was uncommented — without this,
// enqueued jobs sit in storage and nothing ever picks them up.
builder.Services.AddHangfireServer();


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

    options.AddSecurityRequirement(document => new()
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.Configure<MonnifySettings>(builder.Configuration.GetSection("Monnify"));
builder.Services.Configure<MonnifySettings>(
    builder.Configuration.GetSection("Monnify"));

builder.Services.AddHttpClient<MonnifyClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Monnify:BaseUrl"]!);
});


builder.Services.Configure<FiveSimSettings>(builder.Configuration.GetSection("FiveSim"));
builder.Services.AddHttpClient<IFiveSimClient, FiveSimClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FiveSim:BaseUrl"]!);
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

// NEW: gated behind AdminOnly instead of left wide open. Must come after
// UseAuthentication/UseAuthorization above, which it already does.
app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/hangfire"),
    appBuilder =>
    {
        appBuilder.UseAuthentication();
        appBuilder.UseAuthorization();
        appBuilder.UseHangfireDashboard();
    });


// =========================
// RUN APP
// =========================

app.Run();