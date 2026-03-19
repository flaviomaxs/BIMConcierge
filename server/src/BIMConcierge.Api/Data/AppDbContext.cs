using BIMConcierge.Api.Entities;
using BIMConcierge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<LicenseEntity> Licenses => Set<LicenseEntity>();
    public DbSet<TutorialEntity> Tutorials => Set<TutorialEntity>();
    public DbSet<TutorialStepEntity> TutorialSteps => Set<TutorialStepEntity>();
    public DbSet<ProgressEntity> Progress => Set<ProgressEntity>();
    public DbSet<AchievementEntity> Achievements => Set<AchievementEntity>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<CompanyStandardEntity> CompanyStandards => Set<CompanyStandardEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Keys & Indexes ──────────────────────────────────────────────────
        modelBuilder.Entity<Company>().HasKey(c => c.Id);

        modelBuilder.Entity<UserEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Company).WithMany(c => c.Users).HasForeignKey(u => u.CompanyId);
        });

        modelBuilder.Entity<LicenseEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.Key).IsUnique();
            e.HasOne(l => l.Company).WithMany(c => c.Licenses).HasForeignKey(l => l.CompanyId);
        });

        modelBuilder.Entity<TutorialEntity>().HasKey(t => t.Id);

        modelBuilder.Entity<TutorialStepEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Tutorial).WithMany(t => t.Steps).HasForeignKey(s => s.TutorialId);
        });

        modelBuilder.Entity<ProgressEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.User).WithMany(u => u.Progress).HasForeignKey(p => p.UserId);
            e.HasOne(p => p.Tutorial).WithMany().HasForeignKey(p => p.TutorialId);
        });

        modelBuilder.Entity<AchievementEntity>().HasKey(a => a.Id);

        modelBuilder.Entity<UserAchievement>(e =>
        {
            e.HasKey(ua => new { ua.UserId, ua.AchievementId });
            e.HasOne(ua => ua.User).WithMany(u => u.Achievements).HasForeignKey(ua => ua.UserId);
            e.HasOne(ua => ua.Achievement).WithMany(a => a.UserAchievements).HasForeignKey(ua => ua.AchievementId);
        });

        modelBuilder.Entity<CompanyStandardEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Company).WithMany(c => c.Standards).HasForeignKey(s => s.CompanyId);
        });

        modelBuilder.Entity<OrderEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.PaymentId).IsUnique();
            e.HasOne(o => o.License).WithMany().HasForeignKey(o => o.LicenseId);
        });

        // ── Seed Data ───────────────────────────────────────────────────────
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder mb)
    {
        const string companyId = "company-demo-001";
        const string userId = "user-admin-001";
        const string licenseId = "license-ent-001";
        const string licenseKey = "BIM-DEMO-0001-0001";

        // Company
        mb.Entity<Company>().HasData(new Company
        {
            Id = companyId,
            Name = "BIM Concierge Demo",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Admin user (password: Admin123!)
        mb.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = userId,
            Email = "admin@bimconcierge.com",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
            Name = "Admin Demo",
            Role = "Admin",
            CompanyId = companyId,
            AvatarUrl = "",
            XpPoints = 0,
            Level = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Enterprise license (999 seats, expires in 1 year from seed)
        mb.Entity<LicenseEntity>().HasData(new LicenseEntity
        {
            Id = licenseId,
            Key = licenseKey,
            CompanyId = companyId,
            MaxSeats = 999,
            UsedSeats = 0,
            Type = "Enterprise",
            ExpiresAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // ── Tutorials ───────────────────────────────────────────────────────
        var tutorials = new[]
        {
            new TutorialEntity
            {
                Id = "tut-walls-001",
                Title = "Criando Paredes no Revit",
                Description = "Aprenda a criar e configurar paredes no Revit, incluindo tipos, alturas e conexões.",
                Category = "Modelagem",
                Difficulty = "Beginner",
                DurationMins = 15,
                StepCount = 5,
                ThumbnailUrl = "",
                IsCompanyOwned = false
            },
            new TutorialEntity
            {
                Id = "tut-floors-001",
                Title = "Pisos e Lajes",
                Description = "Tutorial completo sobre criação de pisos e lajes estruturais.",
                Category = "Modelagem",
                Difficulty = "Intermediate",
                DurationMins = 20,
                StepCount = 6,
                ThumbnailUrl = "",
                IsCompanyOwned = false
            },
            new TutorialEntity
            {
                Id = "tut-sheets-001",
                Title = "Gerando Pranchas",
                Description = "Como gerar pranchas de impressão com carimbo e vistas organizadas.",
                Category = "Documentação",
                Difficulty = "Intermediate",
                DurationMins = 25,
                StepCount = 7,
                ThumbnailUrl = "",
                IsCompanyOwned = false
            },
            new TutorialEntity
            {
                Id = "tut-families-001",
                Title = "Famílias Paramétricas",
                Description = "Criação de famílias com parâmetros condicionais e fórmulas.",
                Category = "Famílias",
                Difficulty = "Advanced",
                DurationMins = 40,
                StepCount = 10,
                ThumbnailUrl = "",
                IsCompanyOwned = false
            }
        };
        mb.Entity<TutorialEntity>().HasData(tutorials);

        // Tutorial steps (sample for first tutorial)
        mb.Entity<TutorialStepEntity>().HasData(
            new TutorialStepEntity { Id = "step-w1", TutorialId = "tut-walls-001", Order = 1, Title = "Abrir ferramenta Wall", Instruction = "Clique em Architecture > Wall > Wall: Architectural", RevitCommand = "ID_OBJECTS_WALL", HighlightZone = "Ribbon.Architecture.Build", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-w2", TutorialId = "tut-walls-001", Order = 2, Title = "Selecionar tipo", Instruction = "No Type Selector, escolha 'Basic Wall: Generic - 200mm'", RevitCommand = null, HighlightZone = "Properties.TypeSelector", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-w3", TutorialId = "tut-walls-001", Order = 3, Title = "Definir altura", Instruction = "No painel Properties, ajuste 'Unconnected Height' para 3000mm", RevitCommand = null, HighlightZone = "Properties.UnconnectedHeight", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-w4", TutorialId = "tut-walls-001", Order = 4, Title = "Desenhar parede", Instruction = "Clique dois pontos na vista de planta para criar a parede", RevitCommand = null, HighlightZone = "Canvas", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-w5", TutorialId = "tut-walls-001", Order = 5, Title = "Finalizar", Instruction = "Pressione ESC ou clique em Modify para finalizar o comando", RevitCommand = "ID_OBJECTS_MODIFY", HighlightZone = "Ribbon.Modify", AutoApplicable = false },
            // Steps for floors tutorial
            new TutorialStepEntity { Id = "step-f1", TutorialId = "tut-floors-001", Order = 1, Title = "Abrir ferramenta Floor", Instruction = "Clique em Architecture > Floor > Floor: Architectural", RevitCommand = "ID_OBJECTS_FLOOR", HighlightZone = "Ribbon.Architecture.Build", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-f2", TutorialId = "tut-floors-001", Order = 2, Title = "Selecionar contorno", Instruction = "Use Pick Walls para selecionar as paredes como contorno do piso", RevitCommand = null, HighlightZone = "Ribbon.Modify.Draw", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-f3", TutorialId = "tut-floors-001", Order = 3, Title = "Ajustar offset", Instruction = "Verifique se o offset está em -200mm para que o piso fique alinhado", RevitCommand = null, HighlightZone = "OptionsBar.Offset", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-f4", TutorialId = "tut-floors-001", Order = 4, Title = "Confirmar edição", Instruction = "Clique no botão verde (Finish Edit Mode) para criar o piso", RevitCommand = null, HighlightZone = "Ribbon.Modify.Mode", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-f5", TutorialId = "tut-floors-001", Order = 5, Title = "Verificar 3D", Instruction = "Abra a vista 3D (atalho: 3D) para verificar o resultado", RevitCommand = "ID_VIEW_DEFAULT_3DVIEW", HighlightZone = "QuickAccess.3D", AutoApplicable = false },
            new TutorialStepEntity { Id = "step-f6", TutorialId = "tut-floors-001", Order = 6, Title = "Ajustar tipo", Instruction = "Se necessário, altere o tipo do piso no painel Properties", RevitCommand = null, HighlightZone = "Properties.TypeSelector", AutoApplicable = false }
        );

        // ── Achievements ────────────────────────────────────────────────────
        mb.Entity<AchievementEntity>().HasData(
            new AchievementEntity { Id = "ach-first-login", Title = "Primeiro Acesso", Description = "Fez login pela primeira vez no BIM Concierge", Icon = "Star", XpReward = 50 },
            new AchievementEntity { Id = "ach-first-tutorial", Title = "Aprendiz", Description = "Completou o primeiro tutorial", Icon = "School", XpReward = 100 },
            new AchievementEntity { Id = "ach-five-tutorials", Title = "Estudante Dedicado", Description = "Completou 5 tutoriais", Icon = "Trophy", XpReward = 250 },
            new AchievementEntity { Id = "ach-perfect-score", Title = "Nota Máxima", Description = "Obteve 100% em um tutorial", Icon = "CheckCircle", XpReward = 150 },
            new AchievementEntity { Id = "ach-standards-master", Title = "Guardião dos Padrões", Description = "Corrigiu 10 violações de padrão da empresa", Icon = "Shield", XpReward = 200 }
        );

        // ── Company Standards ───────────────────────────────────────────────
        mb.Entity<CompanyStandardEntity>().HasData(
            new CompanyStandardEntity { Id = "std-naming-001", CompanyId = companyId, Category = "Nomenclatura", Name = "Nome de Views", Description = "Views devem seguir o padrão DISCIPLINA-TIPO-PAVIMENTO", Rule = "ViewNameRegex:^[A-Z]{3}-[A-Z]{3}-.*$", IsActive = true, AutoFix = false, AlertLevel = 2 },
            new CompanyStandardEntity { Id = "std-naming-002", CompanyId = companyId, Category = "Nomenclatura", Name = "Nome de Sheets", Description = "Sheets devem ter número no formato XX.XX", Rule = "SheetNumberRegex:^\\d{2}\\.\\d{2}$", IsActive = true, AutoFix = false, AlertLevel = 2 },
            new CompanyStandardEntity { Id = "std-model-001", CompanyId = companyId, Category = "Modelagem", Name = "Paredes sem Room Bounding", Description = "Paredes de detalhe não devem ser Room Bounding", Rule = "WallRoomBounding:DetailWalls=false", IsActive = true, AutoFix = true, AlertLevel = 1 },
            new CompanyStandardEntity { Id = "std-model-002", CompanyId = companyId, Category = "Modelagem", Name = "Altura de paredes", Description = "Paredes devem estar conectadas a um nível superior", Rule = "WallTopConstraint:NotUnconnected", IsActive = true, AutoFix = false, AlertLevel = 1 },
            new CompanyStandardEntity { Id = "std-doc-001", CompanyId = companyId, Category = "Documentação", Name = "Escala de vistas", Description = "Vistas de planta devem usar escala 1:50 ou 1:100", Rule = "ViewScale:FloorPlan=50,100", IsActive = true, AutoFix = false, AlertLevel = 0 }
        );
    }
}
