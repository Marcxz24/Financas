using Financas.Api.Data;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

/// <summary>
/// Configuração principal da aplicação ASP.NET Core.
/// Responsável por registrar dependências, configurar middleware, autenticação JWT,
/// CORS, Swagger e inicialização do pipeline HTTP.
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// Recupera string de conexão do appsettings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registro do DbContext com MySQL
builder.Services.AddDbContext<FinancasDbContext>(options =>
    options.UseNpgsql(connectionString ?? throw new Exception("Connection String não encontrada")));

// Injeção de dependências dos serviços da aplicação
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddHttpClient<EmailService>();
builder.Services.AddScoped<ContaBancariaService>();
builder.Services.AddScoped<CartaoCreditoService>();
builder.Services.AddScoped<FaturaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AjudaService>();
builder.Services.AddScoped<MetaGastoService>();
builder.Services.AddHttpClient<GoogleGeminiService>();
builder.Services.AddScoped<IAService>();
builder.Services.AddScoped<TransferenciaService>();
builder.Services.AddScoped<CofrinhoService>();

// Configuração de CORS para liberar o frontend (local e produção)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://[::1]:5173",
                "https://financas-navy.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Registro dos controllers da API
builder.Services.AddControllers();

// Configuração do Swagger para documentação da API
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Financas.Api",
        Version = "v1"
    });

    // Configuração de autenticação Bearer no Swagger
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Digite: Bearer SEU_TOKEN"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
});

// Configuração de autenticação JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("Jwt:key não configurada");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };

    // Personaliza resposta quando token expira ou é inválido
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(
                "{\"error\": \"Sua sessão expirou. Por favor, faça login novamente.\"}"
            );
        }
    };
});

var app = builder.Build();

// Pipeline HTTP
app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

// Swagger sempre habilitado
app.UseSwagger();
app.UseSwaggerUI();

// Endpoint simples de health check
app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.MapControllers();

app.Run();