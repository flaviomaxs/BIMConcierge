using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Auth;
using System.Threading;

namespace BIMConcierge.Infrastructure.Api;

/// <summary>
/// Fake API client for local development. Returns realistic mock data
/// so the plugin can be tested without a running backend.
/// Activated only in DEBUG builds when BIMCONCIERGE_DEV_MODE=true.
/// </summary>
public sealed class DevBimApiClient : IBimApiClient
{
    public Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken ct = default) =>
        Task.FromResult(Route<TResponse>(endpoint));

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default) =>
        Task.FromResult(Route<TResponse>(endpoint));

    public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default) =>
        Task.FromResult(Route<TResponse>(endpoint));

    public Task DeleteAsync(string endpoint, CancellationToken ct = default) =>
        Task.CompletedTask;

    private static TResponse? Route<TResponse>(string endpoint)
    {
        object? result = endpoint switch
        {
            "tutorials" => DevData.Tutorials,
            _ when endpoint.StartsWith("tutorials/", StringComparison.Ordinal) => DevData.Tutorials.FirstOrDefault(),
            _ when endpoint.StartsWith("progress/", StringComparison.Ordinal) => DevData.Progress,
            _ when endpoint.StartsWith("achievements/", StringComparison.Ordinal) => DevData.Achievements,
            _ when endpoint.StartsWith("standards/", StringComparison.Ordinal) => DevData.Standards,
            _ when endpoint.StartsWith("leaderboard/", StringComparison.Ordinal) => DevData.Leaderboard,
            "auth/login" => new LoginResponse(true, null, "dev-token", "dev-refresh", new User
            {
                Id = "dev-001", Name = "Dev User", Email = "dev@bimconcierge.com",
                Role = "Admin", CompanyId = "dev-company", XpPoints = 2500, Level = 12
            }),
            _ when endpoint.StartsWith("licenses/validate/", StringComparison.Ordinal) => new License
            {
                Key = "DEV-LICENSE", CompanyId = "dev-company", MaxSeats = 999, UsedSeats = 1,
                Type = LicenseType.Enterprise, ExpiresAt = DateTime.UtcNow.AddYears(1)
            },
            _ => null
        };

        return result is TResponse typed ? typed : default;
    }
}

internal static class DevData
{
    public static readonly List<Tutorial> Tutorials =
    [
        new()
        {
            Id = "t1", Title = "Paredes Básicas no Revit", Description = "Aprenda a modelar paredes externas e internas.",
            Category = "Walls", Difficulty = "Beginner", DurationMins = 15, StepCount = 5,
            Steps =
            [
                new() { Order = 0, Title = "Selecionar tipo de parede", Instruction = "Vá em Arquitetura > Parede e selecione 'Parede Básica'." },
                new() { Order = 1, Title = "Definir altura", Instruction = "Na barra de opções, defina a altura como 'Nível 2'." },
                new() { Order = 2, Title = "Desenhar paredes externas", Instruction = "Desenhe o perímetro usando a ferramenta Retângulo." },
                new() { Order = 3, Title = "Adicionar paredes internas", Instruction = "Selecione parede interna e divida os ambientes." },
                new() { Order = 4, Title = "Verificar junções", Instruction = "Use a ferramenta Unir para corrigir junções." }
            ]
        },
        new()
        {
            Id = "t2", Title = "Famílias Paramétricas", Description = "Crie famílias paramétricas avançadas.",
            Category = "Families", Difficulty = "Advanced", DurationMins = 45, StepCount = 8,
            Steps =
            [
                new() { Order = 0, Title = "Criar família", Instruction = "Abra o Editor de Famílias com o template Métrico Genérico." },
                new() { Order = 1, Title = "Planos de referência", Instruction = "Defina planos de referência para largura e altura." },
                new() { Order = 2, Title = "Parâmetros", Instruction = "Crie parâmetros de instância para Largura e Altura." },
                new() { Order = 3, Title = "Extrusão", Instruction = "Desenhe um sólido e trave as dimensões aos planos." },
                new() { Order = 4, Title = "Flexibilidade", Instruction = "Teste a flexibilidade alterando os parâmetros." },
                new() { Order = 5, Title = "Tipos de família", Instruction = "Crie 3 tipos pré-definidos (P, M, G)." },
                new() { Order = 6, Title = "Categoria", Instruction = "Atribua a categoria correta à família." },
                new() { Order = 7, Title = "Carregar no projeto", Instruction = "Salve e carregue a família no projeto ativo." }
            ]
        },
        new()
        {
            Id = "t3", Title = "Coordenação MEP", Description = "Fluxo de coordenação entre disciplinas.",
            Category = "MEP", Difficulty = "Intermediate", DurationMins = 30, StepCount = 6,
            Steps =
            [
                new() { Order = 0, Title = "Vincular modelos", Instruction = "Vincule os modelos de estrutura e MEP." },
                new() { Order = 1, Title = "Detector de interferências", Instruction = "Abra a ferramenta de Verificação de Interferência." },
                new() { Order = 2, Title = "Configurar teste", Instruction = "Configure teste entre Dutos e Vigas Estruturais." },
                new() { Order = 3, Title = "Executar teste", Instruction = "Execute e analise os resultados." },
                new() { Order = 4, Title = "Resolver conflitos", Instruction = "Ajuste as rotas de dutos para resolver interferências." },
                new() { Order = 5, Title = "Gerar relatório", Instruction = "Exporte o relatório de coordenação em HTML." }
            ]
        },
        new()
        {
            Id = "t4", Title = "Documentação de Pranchas", Description = "Monte pranchas de documentação profissionais.",
            Category = "Sheets", Difficulty = "Beginner", DurationMins = 20, StepCount = 4,
            Steps =
            [
                new() { Order = 0, Title = "Criar prancha", Instruction = "Vá em Vista > Pranchas > Nova Prancha." },
                new() { Order = 1, Title = "Colocar vistas", Instruction = "Arraste vistas da árvore de projetos para a prancha." },
                new() { Order = 2, Title = "Tabelas e legendas", Instruction = "Adicione uma tabela de quantitativos." },
                new() { Order = 3, Title = "Exportar PDF", Instruction = "Exporte a prancha em PDF para revisão." }
            ]
        }
    ];

    public static readonly List<TutorialProgress> Progress =
    [
        new() { UserId = "dev-001", TutorialId = "t1", CurrentStep = 5, TotalSteps = 5, IsCompleted = true, ScorePercent = 100, StartedAt = DateTime.UtcNow.AddDays(-7), CompletedAt = DateTime.UtcNow.AddDays(-5) },
        new() { UserId = "dev-001", TutorialId = "t2", CurrentStep = 5, TotalSteps = 8, IsCompleted = false, ScorePercent = 0, StartedAt = DateTime.UtcNow.AddDays(-2) },
        new() { UserId = "dev-001", TutorialId = "t3", CurrentStep = 2, TotalSteps = 6, IsCompleted = false, ScorePercent = 0, StartedAt = DateTime.UtcNow.AddDays(-1) }
    ];

    public static readonly List<Achievement> Achievements =
    [
        new() { Id = "a1", Title = "Primeiro Passo", Description = "Complete seu primeiro tutorial.", Icon = "school", XpReward = 50, IsUnlocked = true, UnlockedAt = DateTime.UtcNow.AddDays(-5) },
        new() { Id = "a2", Title = "Modelador Limpo", Description = "Modele 10 paredes sem erros de nomenclatura.", Icon = "verified", XpReward = 100, IsUnlocked = true, UnlockedAt = DateTime.UtcNow.AddDays(-3) },
        new() { Id = "a3", Title = "Standard Guardian", Description = "Corrija 20 violações de padrão.", Icon = "shield", XpReward = 150, IsUnlocked = false },
        new() { Id = "a4", Title = "Mestre de Famílias", Description = "Complete todos os tutoriais de Famílias.", Icon = "star", XpReward = 200, IsUnlocked = false },
        new() { Id = "a5", Title = "Coordenador BIM", Description = "Execute 5 verificações de interferência.", Icon = "engineering", XpReward = 250, IsUnlocked = false }
    ];

    public static readonly List<LeaderboardEntry> Leaderboard =
    [
        new() { Rank = 1, Name = "Sarah Jenkins", Title = "LOD Master", XpPoints = 2480 },
        new() { Rank = 2, Name = "Michael Chen", Title = "Regex Guru", XpPoints = 2150 },
        new() { Rank = 3, Name = "Emma Watts", Title = "Wall Wizard", XpPoints = 1920 },
        new() { Rank = 4, Name = "Carlos Silva", Title = "Family Expert", XpPoints = 1750 },
        new() { Rank = 5, Name = "Ana Costa", Title = "Standard Guardian", XpPoints = 1620 }
    ];

    public static readonly List<CompanyStandard> Standards =
    [
        new() { Id = "s1", CompanyId = "dev-company", Category = "Naming Conventions", Name = "Paredes Externas", Description = "Prefixo EXT_ obrigatório para paredes externas.", Rule = "^EXT_.*", IsActive = true, AutoFix = true, AlertLevel = Severity.Warning },
        new() { Id = "s2", CompanyId = "dev-company", Category = "Naming Conventions", Name = "Portas", Description = "Prefixo DR_ e numeração sequencial.", Rule = "^DR-[0-9]{2}-.*", IsActive = true, AutoFix = false, AlertLevel = Severity.Error },
        new() { Id = "s3", CompanyId = "dev-company", Category = "LOD Requirements", Name = "Paredes Externas - CD", Description = "LOD 300 na fase de Documentação de Construção.", Rule = "LOD>=300", IsActive = true, AutoFix = false, AlertLevel = Severity.Warning },
        new() { Id = "s4", CompanyId = "dev-company", Category = "Workset Rules", Name = "Links como Overlay", Description = "Todos os Revit Links devem ser Overlay, não Attachment.", Rule = "LinkType==Overlay", IsActive = true, AutoFix = true, AlertLevel = Severity.Error },
        new() { Id = "s5", CompanyId = "dev-company", Category = "Best Practices", Name = "Tamanho de Família", Description = "Famílias não devem exceder 5MB.", Rule = "FileSize<=5MB", IsActive = true, AutoFix = false, AlertLevel = Severity.Info }
    ];
}
