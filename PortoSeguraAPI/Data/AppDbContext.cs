using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Usuaria, IdentityRole<int>, int>(options)
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>();
        builder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>();

        builder.Entity<Usuaria>(entity =>
        {
            entity.ToTable("Usuarios");
        });

        // Atualizando as Tabelas de Identity para usar int como chave primária
        builder.Entity<IdentityRole<int>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRoles");

        builder.Entity<Madrinha>(entity =>
        {
            entity.ToTable("Madrinhas");
            entity.HasOne(m => m.Usuario)
                .WithOne()
                .HasForeignKey<Madrinha>(m => m.UsuarioID)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(m => m.Servicos)
                .WithOne()
                .HasForeignKey(s => s.MadrinhaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Avaliacao>(entity =>
        {
            entity.ToTable("Avaliacoes");
            entity.HasOne(a => a.Usuaria)
                .WithMany(u => u.Avaliacoes)
                .HasForeignKey(a => a.UsuariaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Madrinha)
                .WithMany(m => m.Avaliacoes)
                .HasForeignKey(a => a.MadrinhaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Solicitacao)
                .WithMany(s => s.Avaliacoes)
                .HasForeignKey(a => a.SolicitacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Servico>(entity =>
        {
            entity.ToTable("Servicos");
        });

        builder.Entity<Solicitacao>(entity =>
        {
            entity.ToTable("Solicitacoes");
            entity.HasOne(s => s.Usuaria)
                .WithMany(u => u.Solicitacoes)
                .HasForeignKey(s => s.UsuariaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Madrinha)
                .WithMany(m => m.Solicitacoes)
                .HasForeignKey(s => s.MadrinhaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Documento>(entity =>
        {
            entity.ToTable("Documentos");
            entity.HasKey(d => d.NomeArquivo);
            entity.HasOne<Usuaria>()
                .WithMany(u => u.Documentos)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Criando dados iniciais para as roles
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "Usuaria", NormalizedName = "USUARIA" },
            new IdentityRole<int> { Id = 2, Name = "Madrinha", NormalizedName = "MADRINHA" },
            new IdentityRole<int> { Id = 3, Name = "Operador", NormalizedName = "OPERADOR" },
            new IdentityRole<int> { Id = 4, Name = "Admin", NormalizedName = "ADMIN" }
        );

        
    }
}