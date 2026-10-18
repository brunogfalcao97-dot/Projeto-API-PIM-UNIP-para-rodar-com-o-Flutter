using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PimTintas.Api.Data;
using PimTintas.Api.Services;
using Scalar.AspNetCore;

// Números em formulários (ex.: areaM2=12.5 enviado pelo app) são lidos sempre com ponto decimal,
// independente do idioma do Windows do servidor.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Banco (SQL Server)
var conexao = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection no appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(conexao));

// ---------------------------------------------------------------- Regras da loja (appsettings.json -> "Loja")
builder.Services.Configure<LojaOptions>(builder.Configuration.GetSection("Loja"));
builder.Services.AddScoped<PedidoService>();

// ---------------------------------------------------------------- Login (JWT)
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 32 caracteres (appsettings.json).");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // Mantém os nomes curtos das claims ("sub", "email", "name", "role") como foram emitidas.
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            // É esta linha que faz [Authorize(Roles = "Admin")] enxergar o perfil que vem no token.
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------- Limite de tentativas no login/cadastro
// Dificulta quem tenta adivinhar senhas em sequência. Vale por endereço IP.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy("auth", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// ---------------------------------------------------------------- CORS (só importa para Flutter Web/Chrome)
// Em produção, liste o endereço do site em appsettings.json -> "Cors": { "Origens": [ "https://meusite.com" ] }.
// Sem nenhuma origem listada, qualquer origem é aceita (ok só para desenvolvimento).
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opcoes =>
    opcoes.AddPolicy("App", politica =>
    {
        if (origens.Length > 0)
            politica.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod();
        else
            politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }));

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------------------------------------------- Prepara o banco na primeira execução
using (var escopo = app.Services.CreateScope())
{
    var admin = builder.Configuration.GetSection("Admin").Get<AdminSeedOptions>() ?? new AdminSeedOptions();
    try
    {
        DbInicializador.Inicializar(escopo.ServiceProvider.GetRequiredService<AppDbContext>(), admin, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "Não foi possível preparar o banco de dados. Confira a connection string, se o SQL Server está ligado " +
            "e se as migrations foram criadas (veja o README da API).");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // JSON em /openapi/v1.json
    app.MapScalarApiReference(); // interface em /scalar/v1
}
else
{
    app.UseExceptionHandler();   // erro inesperado vira uma resposta 500 limpa, sem detalhes internos
    app.UseHttpsRedirection();   // em desenvolvimento o app Flutter usa http://
}

// Sem UseStaticFiles de propósito: as fotos dos projetos são privadas e só saem por
// GET /api/simulacoes/{id}/imagem, para o dono autenticado.
app.UseRouting();
app.UseCors("App");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
