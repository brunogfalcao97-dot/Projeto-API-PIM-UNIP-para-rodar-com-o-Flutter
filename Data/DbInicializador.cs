using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Models;
using PimTintas.Api.Services;

namespace PimTintas.Api.Data;

/// <summary>
/// Prepara o banco na inicialização:
///  1. aplica as migrations (ou cria as tabelas, se o projeto ainda não tiver migrations);
///  2. completa os parâmetros dos tipos de tinta que já existiam antes desta versão;
///  3. carrega um catálogo de exemplo (cores, tipos, marcas, produtos e cupons) só quando as tabelas estão vazias;
///  4. cria o administrador definido na seção "Admin" do appsettings.json, se ele ainda não existir.
///
/// É seguro rodar toda vez: nada que já existe é sobrescrito.
/// As marcas, preços e cupons abaixo são FICTÍCIOS, só para o app ter o que mostrar.
/// </summary>
public static class DbInicializador
{
    public static void Inicializar(AppDbContext db, AdminSeedOptions admin, ILogger logger)
    {
        if (db.Database.GetMigrations().Any())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();

        CompletarTiposExistentes(db);
        CarregarCatalogo(db);
        CriarAdministrador(db, admin, logger);
    }

    // ------------------------------------------------------------------ tipos que já existiam

    /// <summary>
    /// Na migration, as colunas novas de TiposTinta chegam zeradas nas linhas antigas.
    /// Aqui elas recebem os valores padrão (o fator visual, antes fixo no app, passa a vir daqui).
    /// </summary>
    private static void CompletarTiposExistentes(AppDbContext db)
    {
        var pendentes = db.TiposTinta
            .Where(t => t.RendimentoM2PorLitro <= 0 || t.DemaosRecomendadas <= 0 || t.FatorContraste <= 0)
            .ToList();
        if (pendentes.Count == 0) return;

        foreach (var tipo in pendentes)
        {
            var (rendimento, demaos, fator) = ParametrosPadrao(tipo.Nome, tipo.Acabamento);
            if (tipo.RendimentoM2PorLitro <= 0) tipo.RendimentoM2PorLitro = rendimento;
            if (tipo.DemaosRecomendadas <= 0) tipo.DemaosRecomendadas = demaos;
            if (tipo.FatorContraste <= 0) tipo.FatorContraste = fator;
        }
        db.SaveChanges();
    }

    private static (decimal Rendimento, int Demaos, decimal Fator) ParametrosPadrao(string nome, string acabamento)
    {
        var fator = acabamento.Contains("fosc", StringComparison.OrdinalIgnoreCase) ? 0.80m
                  : acabamento.Contains("brilh", StringComparison.OrdinalIgnoreCase) ? 1.10m
                  : 0.95m;

        var n = nome.ToLowerInvariant();
        if (n.Contains("esmalte")) return (11m, 2, fator);
        if (n.Contains("pva")) return (8m, 3, fator);
        return (10m, 2, fator);
    }

    // ------------------------------------------------------------------ catálogo de exemplo

    private static void CarregarCatalogo(AppDbContext db)
    {
        if (!db.TiposTinta.Any())
        {
            db.TiposTinta.AddRange(
                NovoTipo("Acrílica", "Fosco", "Para paredes internas. Sem brilho, disfarça pequenas imperfeições."),
                NovoTipo("Acrílica", "Acetinado", "Leve brilho e toque macio. Fácil de limpar."),
                NovoTipo("Esmalte Sintético", "Brilhante", "Para madeira e metal. Acabamento brilhante e resistente."),
                NovoTipo("Látex PVA", "Fosco", "Opção econômica para paredes internas.")
            );
        }

        if (!db.Cores.Any())
        {
            db.Cores.AddRange(
                // Brancos
                NovaCor("Branco Neve", "#F8F8F5", "Branco"),
                NovaCor("Gelo", "#E9EDEF", "Branco"),
                NovaCor("Pérola", "#EAE0C8", "Branco"),
                // Neutros
                NovaCor("Areia", "#E3D5B8", "Neutro"),
                NovaCor("Bege Camurça", "#D2B48C", "Neutro"),
                NovaCor("Marrom Café", "#5C4033", "Neutro"),
                // Cinzas
                NovaCor("Cinza Concreto", "#9A9A9A", "Cinza"),
                NovaCor("Cinza Chumbo", "#4A4F54", "Cinza"),
                NovaCor("Grafite", "#2F3337", "Cinza"),
                // Azuis
                NovaCor("Azul Céu", "#A8D4F0", "Azul"),
                NovaCor("Azul Oceano", "#1F5F8B", "Azul"),
                NovaCor("Azul Petróleo", "#1B4D5C", "Azul"),
                // Verdes
                NovaCor("Verde Menta", "#B8E0CE", "Verde"),
                NovaCor("Verde Oliva", "#6B7B3A", "Verde"),
                NovaCor("Verde Floresta", "#2E5E3E", "Verde"),
                // Amarelos e laranjas
                NovaCor("Amarelo Sol", "#F5D547", "Amarelo"),
                NovaCor("Mostarda", "#D4A017", "Amarelo"),
                NovaCor("Laranja Terra", "#D9772B", "Laranja"),
                NovaCor("Terracota", "#B5543A", "Laranja"),
                // Vermelhos e rosas
                NovaCor("Vermelho Cereja", "#A8232F", "Vermelho"),
                NovaCor("Rosa Antigo", "#D8A7A7", "Rosa"),
                // Roxos
                NovaCor("Lilás Suave", "#C9B6E0", "Roxo"),
                NovaCor("Uva", "#5B3A74", "Roxo")
            );
        }

        if (!db.Marcas.Any())
        {
            db.Marcas.AddRange(
                new Marca { Nome = "FixTintas", Descricao = "Linha própria da loja." },
                new Marca { Nome = "ColorMax", Descricao = "Boa cobertura com preço acessível." },
                new Marca { Nome = "Premium Casa", Descricao = "Linha premium, alto rendimento." }
            );
        }

        db.SaveChanges(); // grava para os Ids existirem antes de criar os produtos

        if (!db.Produtos.Any())
        {
            var marcas = db.Marcas.AsNoTracking().OrderBy(m => m.Id).ToList();
            var tipos = db.TiposTinta.AsNoTracking().Where(t => !t.Inativo).OrderBy(t => t.Id).ToList();

            foreach (var marca in marcas)
            foreach (var tipo in tipos)
            foreach (var (volume, fatorTamanho) in new[] { (0.9m, 1.25m), (3.6m, 1.00m), (18m, 0.85m) })
            {
                var bruto = PrecoBasePorLitro(tipo) * FatorMarca(marca) * volume * fatorTamanho;
                db.Produtos.Add(new Produto
                {
                    Sku = $"{Prefixo(marca.Nome)}-T{tipo.Id}-{(int)(volume * 10)}",
                    MarcaId = marca.Id,
                    TipoTintaId = tipo.Id,
                    VolumeLitros = volume,
                    Preco = Math.Floor(bruto) + 0.90m,
                    Estoque = 50,
                });
            }
        }

        if (!db.Cupons.Any())
        {
            db.Cupons.AddRange(
                new Cupom { Codigo = "BEMVINDO10", Tipo = TipoDesconto.Percentual, Valor = 10m, ValorMinimoPedido = 100m },
                new Cupom { Codigo = "PINTE50", Tipo = TipoDesconto.ValorFixo, Valor = 50m, ValorMinimoPedido = 300m }
            );
        }

        db.SaveChanges();
    }

    private static TipoTinta NovoTipo(string nome, string acabamento, string descricao)
    {
        var (rendimento, demaos, fator) = ParametrosPadrao(nome, acabamento);
        return new TipoTinta
        {
            Nome = nome,
            Acabamento = acabamento,
            Descricao = descricao,
            RendimentoM2PorLitro = rendimento,
            DemaosRecomendadas = demaos,
            FatorContraste = fator,
        };
    }

    private static Cor NovaCor(string nome, string hex, string familia) =>
        new() { Nome = nome, CodigoHex = hex, Familia = familia };

    private static decimal PrecoBasePorLitro(TipoTinta tipo)
    {
        var nome = tipo.Nome.ToLowerInvariant();
        if (nome.Contains("esmalte")) return 45m;
        if (nome.Contains("pva")) return 18m;
        return tipo.Acabamento.Contains("acet", StringComparison.OrdinalIgnoreCase) ? 34m : 28m;
    }

    private static decimal FatorMarca(Marca marca) => marca.Nome switch
    {
        "ColorMax" => 0.90m,
        "Premium Casa" => 1.35m,
        _ => 1.00m,
    };

    private static string Prefixo(string nome)
    {
        var letras = new string(nome.Where(char.IsLetter).ToArray()).ToUpper(CultureInfo.InvariantCulture);
        return letras.Length >= 3 ? letras[..3] : letras.PadRight(3, 'X');
    }

    // ------------------------------------------------------------------ administrador

    private static void CriarAdministrador(AppDbContext db, AdminSeedOptions admin, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Senha)) return;

        var email = admin.Email.Trim().ToLowerInvariant();
        if (db.Usuarios.Any(u => u.Email == email)) return;

        db.Usuarios.Add(new Usuario
        {
            Nome = string.IsNullOrWhiteSpace(admin.Nome) ? "Administrador" : admin.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(admin.Senha),
            Perfil = Perfis.Admin,
        });
        db.SaveChanges();

        logger.LogWarning(
            "Administrador {Email} criado a partir do appsettings.json. Troque a senha de exemplo antes de publicar.",
            email);
    }
}
