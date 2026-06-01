using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Enums;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuaria>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        await context.Database.EnsureCreatedAsync();

        await EnsureRolesAsync(roleManager);

        var usuariaAtiva = await EnsureUserAsync(
            userManager,
            nome: "Ana Lima",
            email: "ana.lima@portosegura.local",
            telefone: "11990000001",
            bio: "Usuaria ativa para testar cadastro, login e solicitacoes.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "SP",
            cidade: "São Paulo",
            videoVerificacao: "https://example.com/video1"
        );

        var usuariaPendente = await EnsureUserAsync(
            userManager,
            nome: "Bruna Martins",
            email: "bruna.martins@portosegura.local",
            telefone: "11990000002",
            bio: "Conta pendente para validar a regra de aprovacao.",
            status: UserStatus.Pendente,
            senha: "Senha@12345",
            estado: "RJ",
            cidade: "Rio de Janeiro",
            videoVerificacao: "https://example.com/video2"
        );

        var madrinhaUsuario = await EnsureUserAsync(
            userManager,
            nome: "Camila Souza",
            email: "camila.souza@portosegura.local",
            telefone: "11990000003",
            bio: "Perfil de madrinha para testar listagem e pareamento.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "MG",
            cidade: "Belo Horizonte",
            videoVerificacao: "https://example.com/video3"
        );

        var operador = await EnsureUserAsync(
            userManager,
            nome: "Operador da Plataforma",
            email: "operador@portosegura.local",
            telefone: "11990000004",
            bio: "Conta para testar autorizacao de administracao.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "RS",
            cidade: "Porto Alegre",
            videoVerificacao: "https://example.com/video4"
        );

        var admin = await EnsureUserAsync(
            userManager,
            nome: "Admin da Plataforma",
            email: "admin@portosegura.local",
            telefone: "11990000005",
            bio: "Conta administrativa para testes gerais.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "BA",
            cidade: "Salvador",
            videoVerificacao: "https://example.com/video5"
        );

        await EnsureRoleAssignmentAsync(userManager, usuariaAtiva, "Usuaria");
        await EnsureRoleAssignmentAsync(userManager, usuariaPendente, "Usuaria");
        await EnsureRoleAssignmentAsync(userManager, madrinhaUsuario, "Madrinha");
        await EnsureRoleAssignmentAsync(userManager, operador, "Operador");
        await EnsureRoleAssignmentAsync(userManager, admin, "Admin");

        await EnsureMadrinhaAsync(context, madrinhaUsuario);
        await EnsureSolicitacoesAsync(context, usuariaAtiva, madrinhaUsuario);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        var roles = new[] { "Usuaria", "Madrinha", "Operador", "Admin" };

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        }
    }

    private static async Task<Usuaria> EnsureUserAsync(
        UserManager<Usuaria> userManager,
        string nome,
        string email,
        string telefone,
        string bio,
        UserStatus status,
        string senha,
        string estado,
        string cidade,
        string videoVerificacao,
        string urlLinkedin = "",
        string urlInstagram = "",
        string urlFacebook = "")
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            return user;
        }

        user = new Usuaria
        {
            Nome = nome,
            Email = email,
            UserName = email,
            Telefone = telefone,
            Bio = bio,
            Status = status,
            Estado = estado,
            Cidade = cidade,
            VideoVerificacao = videoVerificacao,
            urlLinkedin = urlLinkedin,
            urlInstagram = urlInstagram,
            urlFacebook = urlFacebook,
            DataCriacao = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, senha);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao criar usuario seed {email}: {errors}");
        }

        return user;
    }

    private static async Task EnsureRoleAssignmentAsync(
        UserManager<Usuaria> userManager,
        Usuaria user,
        string roleName)
    {
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static async Task EnsureMadrinhaAsync(AppDbContext context, Usuaria madrinhaUsuario)
    {
        var exists = await context.Set<Madrinha>()
            .AnyAsync(m => m.UsuarioID == madrinhaUsuario.Id);

        if (exists)
        {
            return;
        }

        context.Add(new Madrinha
        {
            UsuarioID = madrinhaUsuario.Id,
            PrecoDiaria = 180.00m,
            VerificadoIdentidade = true,
            VerificadoResidencia = true,
            TrilhaCursoCompleto = true,
            Motivacao = "Quero ajudar outras mulheres a terem mais seguranca e autonomia.",
            DataCriacao = DateTime.UtcNow,
            Usuario = madrinhaUsuario
        });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureSolicitacoesAsync(AppDbContext context, Usuaria usuariaAtiva, Usuaria madrinhaUsuario)
    {
        var solicitacoesExistem = await context.Set<Solicitacao>()
            .AnyAsync(s => s.UsuariaId == usuariaAtiva.Id && s.MadrinhaId != 0);

        if (solicitacoesExistem)
        {
            return;
        }

        var madrinha = await context.Set<Madrinha>()
            .FirstAsync(m => m.UsuarioID == madrinhaUsuario.Id);

        var hoje = DateTime.UtcNow.Date;

        context.AddRange(
            new Solicitacao
            {
                UsuariaId = usuariaAtiva.Id,
                MadrinhaId = madrinha.Id,
                Destino = madrinha.Usuario.Cidade + ", " + madrinha.Usuario.Estado,
                Descricao = "Teste de solicitacao aberta para fluxo completo.",
                DataCriacao = DateTime.UtcNow,
                DataInicio = hoje.AddDays(7),
                DataFim = hoje.AddDays(14),
                QtdDiarias = 7,
                Status = "Aberta",
                Valor = 1260m,
                Usuaria = usuariaAtiva,
                Madrinha = madrinha
            },
            new Solicitacao
            {
                UsuariaId = usuariaAtiva.Id,
                MadrinhaId = madrinha.Id,
                Destino = madrinha.Usuario.Cidade + ", " + madrinha.Usuario.Estado,
                Descricao = "Teste de solicitacao aceita para validar mudanca de status.",
                DataCriacao = DateTime.UtcNow,
                DataInicio = hoje.AddDays(20),
                DataFim = hoje.AddDays(25),
                QtdDiarias = 5,
                Status = "Aceita",
                Valor = 900m,
                Usuaria = usuariaAtiva,
                Madrinha = madrinha
            },
            new Solicitacao
            {
                UsuariaId = usuariaAtiva.Id,
                MadrinhaId = madrinha.Id,
                Destino = madrinha.Usuario.Cidade + ", " + madrinha.Usuario.Estado,
                Descricao = "Teste de solicitacao cancelada para validar historico.",
                DataCriacao = DateTime.UtcNow,
                DataInicio = hoje.AddDays(30),
                DataFim = hoje.AddDays(32),
                QtdDiarias = 2,
                Status = "Cancelada",
                Valor = 360m,
                Usuaria = usuariaAtiva,
                Madrinha = madrinha
            },
            new Solicitacao
            {
                UsuariaId = usuariaAtiva.Id,
                MadrinhaId = madrinha.Id,
                Destino = madrinha.Usuario.Cidade + ", " + madrinha.Usuario.Estado,
                Descricao = "Teste de solicitacao avaliada para validacao de pos-processamento.",
                DataCriacao = DateTime.UtcNow,
                DataInicio = hoje.AddDays(40),
                DataFim = hoje.AddDays(44),
                QtdDiarias = 4,
                Status = "Avaliada",
                Valor = 720m,
                Usuaria = usuariaAtiva,
                Madrinha = madrinha
            }
        );

        await context.SaveChangesAsync();
    }
}