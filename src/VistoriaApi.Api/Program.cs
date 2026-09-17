using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VistoriaApi.Api.Authentication;
using VistoriaApi.Api.Middlewares;
using VistoriaApi.Application.Abstractions;
using VistoriaApi.Application.Services;
using VistoriaApi.Infrastructure.Authentication;
using VistoriaApi.Infrastructure.Email;
using VistoriaApi.Infrastructure.Files;
using VistoriaApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vistor.ia API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<VistoriaApi.Application.Abstractions.IEmailTemplateRenderer, VistoriaApi.Infrastructure.Email.SmtpEmailTemplateRenderer>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key não configurada.");
}

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key deve possuir pelo menos 32 caracteres.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<ExceptionMiddleware>();
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "Vistor.ia API",
        utcTime = DateTime.UtcNow
    }))
    .WithTags("Health");
app.MapGet("/health/database", async (
        AppDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (connected)
            {
                return Results.Ok(new
                {
                    status = "healthy",
                    database = "PostgreSQL"
                });
            }

            return Results.Problem(
                title: "Não foi possível conectar ao PostgreSQL.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Falha ao conectar ao PostgreSQL.",
                detail: app.Environment.IsDevelopment() ? exception.Message : null,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .WithTags("Health");

app.Run();

namespace VistoriaApi.Api
{
    public partial class Program
    {
    }
}
