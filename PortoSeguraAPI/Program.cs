using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
            ,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddIdentityCore<Usuaria>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("PermitFrontendLocal", policy =>
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    }
);

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BlobStorageService>();

var app = builder.Build();

app.MapHealthChecks("/health");

var databasePath = Path.Combine(Directory.GetCurrentDirectory(), "portosegura.db");
var databaseWalPath = databasePath + "-wal";
var databaseShmPath = databasePath + "-shm";

if (File.Exists(databasePath))
{
    File.Delete(databasePath);
}

if (File.Exists(databaseWalPath))
{
    File.Delete(databaseWalPath);
}

if (File.Exists(databaseShmPath))
{
    File.Delete(databaseShmPath);
}

await DbSeeder.SeedAsync(app.Services);

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PortoSegura API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
});

app.UseStaticFiles();

app.UseCors("PermitFrontendLocal");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
