using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=portosegura.db");
        }
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>();

        builder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
        });

        // Atualizando as Tabelas de Identity para usar int como chave primária
        builder.Entity<IdentityRole<int>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRoles");

        // Criando dados iniciais para as roles
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "Usuaria", NormalizedName = "USUARIA" },
            new IdentityRole<int> { Id = 2, Name = "Madrinha", NormalizedName = "MADRINHA" },
            new IdentityRole<int> { Id = 3, Name = "Operador", NormalizedName = "OPERADOR" },
            new IdentityRole<int> { Id = 4, Name = "Admin", NormalizedName = "ADMIN" }
        );
    }
}