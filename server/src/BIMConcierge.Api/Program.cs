using System.Text;
using BIMConcierge.Api.Data;
using BIMConcierge.Api.Endpoints;
using BIMConcierge.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── JSON PascalCase (plugin usa Newtonsoft.Json que espera PascalCase) ───────
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// ── EF Core + PostgreSQL ────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ──────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "BIMConcierge",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BIMConcierge.Plugin",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AuthTokenService>();
builder.Services.AddScoped<ProvisioningService>();
builder.Services.AddHttpClient<ResendEmailSender>();
builder.Services.AddSingleton<IEmailSender, ResendEmailSender>();

// ── Swagger ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS (allow plugin & dev) ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────────────────────
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Map Endpoints under /v1/ ────────────────────────────────────────────────
app.MapGroup("/v1/auth").MapAuthEndpoints();
app.MapGroup("/v1/licenses").MapLicenseEndpoints().RequireAuthorization();
app.MapGroup("/v1/tutorials").MapTutorialEndpoints().RequireAuthorization();
app.MapGroup("/v1").MapProgressEndpoints().RequireAuthorization();
app.MapGroup("/v1/standards").MapStandardsEndpoints().RequireAuthorization();
app.MapGroup("/v1/webhooks").MapWebhookEndpoints();
app.MapGroup("/v1/public").MapPublicEndpoints();

// ── Fallback to index.html for SPA ───────────────────────────────────────────
app.MapFallbackToFile("index.html");

// ── Auto-migrate on startup (skip for InMemory provider in tests) ────────────
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program { }
