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
            bio: "Adoro viajar sozinha, mas prefiro me hospedar em lugares que ofereçam uma rede de apoio feminina. Busco uma experiência autêntica, com segurança e trocas culturais enriquecedoras.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "SP",
            cidade: "São Paulo",
            videoVerificacao: "https://example.com/video1"
        );

        var usuariaAtiva2 = await EnsureUserAsync(
            userManager,
            nome: "Roberta Oliveira",
            email: "roberta.oliveira@portosegura.local",
            telefone: "11990000001",
            bio: "Adoro viajar sozinha, mas prefiro me hospedar em lugares que ofereçam uma rede de apoio feminina. Busco uma experiência autêntica, com segurança e trocas culturais enriquecedoras.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "RJ",
            cidade: "Rio de Janeiro",
            videoVerificacao: "https://example.com/video1"
        );

        var usuariaPendente = await EnsureUserAsync(
            userManager,
            nome: "Bruna Martins",
            email: "bruna.martins@portosegura.local",
            telefone: "11990000002",
            bio: "Estou planejando minha primeira viagem solo para o Rio de Janeiro e adoraria o suporte de uma madrinha local para dicas de segurança e roteiros menos turísticos. Animada para começar!",
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
            bio: "Sou apaixonada por viajar e também acredito no poder do acolhimento. Quando não estou viajando, gosto de ajudar outras mulheres a se sentirem em casa em suas jornadas.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "PE",
            cidade: "Recife",
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
        await EnsureRoleAssignmentAsync(userManager, usuariaAtiva2, "Usuaria");
        await EnsureRoleAssignmentAsync(userManager, usuariaPendente, "Usuaria");
        await EnsureRoleAssignmentAsync(userManager, madrinhaUsuario, "Madrinha");
        await EnsureRoleAssignmentAsync(userManager, madrinhaUsuario, "Usuaria");
        await EnsureRoleAssignmentAsync(userManager, operador, "Operador");
        await EnsureRoleAssignmentAsync(userManager, admin, "Admin");

        var timeRecife = await EnsureTimeLocalAsync(context, "Time Recife", "Recife", "PE");

        await EnsureMadrinhaAsync(context, madrinhaUsuario, timeRecife.Id);

        // Seed Daniela Silva (Recife, PE)
        var madrinhaRecifeUsuario1 = await EnsureUserAsync(
            userManager,
            nome: "Daniela Silva",
            email: "daniela.silva@portosegura.local",
            telefone: "81990000001",
            bio: "Nascida e criada no Recife, conheço cada detalhe do centro histórico e dos museus. Minha missão como madrinha é garantir que você não apenas conheça a cidade, mas se sinta segura e bem-vinda em cada esquina. Especialista em roteiros culturais e históricos.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "PE",
            cidade: "Recife",
            videoVerificacao: "https://example.com/video6"
        );
        await EnsureRoleAssignmentAsync(userManager, madrinhaRecifeUsuario1, "Madrinha");
        await EnsureRoleAssignmentAsync(userManager, madrinhaRecifeUsuario1, "Usuaria");
        await EnsureMadrinhaAsync(context, madrinhaRecifeUsuario1, timeRecife.Id);

        // Seed Fernanda Souza (Recife, PE)
        var madrinhaRecifeUsuario2 = await EnsureUserAsync(
            userManager,
            nome: "Fernanda Souza",
            email: "fernanda.souza@portosegura.local",
            telefone: "81990000002",
            bio: "Madrinha do Time Recife, sou uma entusiasta da gastronomia local e das praias do litoral pernambucano. Se você busca dicas de onde comer bem, segredos dos moradores locais e um ambiente seguro e alegre, conte comigo para tornar sua estadia inesquecível.",
            status: UserStatus.Ativo,
            senha: "Senha@12345",
            estado: "PE",
            cidade: "Recife",
            videoVerificacao: "https://example.com/video7"
        );
        await EnsureRoleAssignmentAsync(userManager, madrinhaRecifeUsuario2, "Madrinha");
        await EnsureRoleAssignmentAsync(userManager, madrinhaRecifeUsuario2, "Usuaria");
        await EnsureMadrinhaAsync(context, madrinhaRecifeUsuario2, timeRecife.Id);

        // Atualizar saldo de créditos da usuária de teste
        usuariaAtiva.SaldoCreditos = 40;
        await userManager.UpdateAsync(usuariaAtiva);

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

    private static async Task<TimeLocal> EnsureTimeLocalAsync(AppDbContext context, string nome, string cidade, string estado)
    {
        var time = await context.Set<TimeLocal>().FirstOrDefaultAsync(t => t.Nome == nome);
        if (time == null)
        {
            time = new TimeLocal { Nome = nome, Cidade = cidade, Estado = estado };
            context.Add(time);
            await context.SaveChangesAsync();
        }
        return time;
    }

    private static async Task EnsureMadrinhaAsync(AppDbContext context, Usuaria madrinhaUsuario, int? timeLocalId = null)
    {
        var exists = await context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == madrinhaUsuario.Id);

        if (exists != null)
        {
            if (timeLocalId.HasValue && exists.TimeLocalId != timeLocalId)
            {
                exists.TimeLocalId = timeLocalId;
                await context.SaveChangesAsync();
            }
            return;
        }

        context.Add(new Madrinha
        {
            UsuarioID = madrinhaUsuario.Id,
            PrecoDiaria = 180.00m,
            VerificadoIdentidade = true,
            VerificadoResidencia = true,
            TrilhaCursoCompleto = true,
            Motivacao = "Acredito que o acolhimento transforma viagens em vivências. Como madrinha em Recife, foco em oferecer um ambiente de confiança, trocas de experiências e suporte constante para mulheres que desejam explorar o Nordeste com autonomia.",
            DataCriacao = DateTime.UtcNow,
            Usuario = madrinhaUsuario,
            TimeLocalId = timeLocalId
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