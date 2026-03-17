using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BIMConcierge.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    XpReward = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tutorials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    DurationMins = table.Column<int>(type: "integer", nullable: false),
                    StepCount = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: false),
                    IsCompanyOwned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutorials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyStandards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Rule = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AutoFix = table.Column<bool>(type: "boolean", nullable: false),
                    AlertLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyStandards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyStandards_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    MaxSeats = table.Column<int>(type: "integer", nullable: false),
                    UsedSeats = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: false),
                    XpPoints = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutorialSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TutorialId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    RevitCommand = table.Column<string>(type: "text", nullable: true),
                    HighlightZone = table.Column<string>(type: "text", nullable: true),
                    AutoApplicable = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationRule = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialSteps_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Progress",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TutorialId = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Progress_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Progress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AchievementId = table.Column<string>(type: "text", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => new { x.UserId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Description", "Icon", "Title", "XpReward" },
                values: new object[,]
                {
                    { "ach-first-login", "Fez login pela primeira vez no BIM Concierge", "Star", "Primeiro Acesso", 50 },
                    { "ach-first-tutorial", "Completou o primeiro tutorial", "School", "Aprendiz", 100 },
                    { "ach-five-tutorials", "Completou 5 tutoriais", "Trophy", "Estudante Dedicado", 250 },
                    { "ach-perfect-score", "Obteve 100% em um tutorial", "CheckCircle", "Nota Máxima", 150 },
                    { "ach-standards-master", "Corrigiu 10 violações de padrão da empresa", "Shield", "Guardião dos Padrões", 200 }
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[] { "company-demo-001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BIM Concierge Demo" });

            migrationBuilder.InsertData(
                table: "Tutorials",
                columns: new[] { "Id", "Category", "Description", "Difficulty", "DurationMins", "IsCompanyOwned", "StepCount", "ThumbnailUrl", "Title" },
                values: new object[,]
                {
                    { "tut-families-001", "Famílias", "Criação de famílias com parâmetros condicionais e fórmulas.", "Advanced", 40, false, 10, "", "Famílias Paramétricas" },
                    { "tut-floors-001", "Modelagem", "Tutorial completo sobre criação de pisos e lajes estruturais.", "Intermediate", 20, false, 6, "", "Pisos e Lajes" },
                    { "tut-sheets-001", "Documentação", "Como gerar pranchas de impressão com carimbo e vistas organizadas.", "Intermediate", 25, false, 7, "", "Gerando Pranchas" },
                    { "tut-walls-001", "Modelagem", "Aprenda a criar e configurar paredes no Revit, incluindo tipos, alturas e conexões.", "Beginner", 15, false, 5, "", "Criando Paredes no Revit" }
                });

            migrationBuilder.InsertData(
                table: "CompanyStandards",
                columns: new[] { "Id", "AlertLevel", "AutoFix", "Category", "CompanyId", "Description", "IsActive", "Name", "Rule" },
                values: new object[,]
                {
                    { "std-doc-001", 0, false, "Documentação", "company-demo-001", "Vistas de planta devem usar escala 1:50 ou 1:100", true, "Escala de vistas", "ViewScale:FloorPlan=50,100" },
                    { "std-model-001", 1, true, "Modelagem", "company-demo-001", "Paredes de detalhe não devem ser Room Bounding", true, "Paredes sem Room Bounding", "WallRoomBounding:DetailWalls=false" },
                    { "std-model-002", 1, false, "Modelagem", "company-demo-001", "Paredes devem estar conectadas a um nível superior", true, "Altura de paredes", "WallTopConstraint:NotUnconnected" },
                    { "std-naming-001", 2, false, "Nomenclatura", "company-demo-001", "Views devem seguir o padrão DISCIPLINA-TIPO-PAVIMENTO", true, "Nome de Views", "ViewNameRegex:^[A-Z]{3}-[A-Z]{3}-.*$" },
                    { "std-naming-002", 2, false, "Nomenclatura", "company-demo-001", "Sheets devem ter número no formato XX.XX", true, "Nome de Sheets", "SheetNumberRegex:^\\d{2}\\.\\d{2}$" }
                });

            migrationBuilder.InsertData(
                table: "Licenses",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "ExpiresAt", "Key", "MaxSeats", "Type", "UsedSeats" },
                values: new object[] { "license-ent-001", "company-demo-001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BIM-DEMO-0001-0001", 999, "Enterprise", 0 });

            migrationBuilder.InsertData(
                table: "TutorialSteps",
                columns: new[] { "Id", "AutoApplicable", "HighlightZone", "Instruction", "Order", "RevitCommand", "Title", "TutorialId", "ValidationRule" },
                values: new object[,]
                {
                    { "step-f1", false, "Ribbon.Architecture.Build", "Clique em Architecture > Floor > Floor: Architectural", 1, "ID_OBJECTS_FLOOR", "Abrir ferramenta Floor", "tut-floors-001", null },
                    { "step-f2", false, "Ribbon.Modify.Draw", "Use Pick Walls para selecionar as paredes como contorno do piso", 2, null, "Selecionar contorno", "tut-floors-001", null },
                    { "step-f3", false, "OptionsBar.Offset", "Verifique se o offset está em -200mm para que o piso fique alinhado", 3, null, "Ajustar offset", "tut-floors-001", null },
                    { "step-f4", false, "Ribbon.Modify.Mode", "Clique no botão verde (Finish Edit Mode) para criar o piso", 4, null, "Confirmar edição", "tut-floors-001", null },
                    { "step-f5", false, "QuickAccess.3D", "Abra a vista 3D (atalho: 3D) para verificar o resultado", 5, "ID_VIEW_DEFAULT_3DVIEW", "Verificar 3D", "tut-floors-001", null },
                    { "step-f6", false, "Properties.TypeSelector", "Se necessário, altere o tipo do piso no painel Properties", 6, null, "Ajustar tipo", "tut-floors-001", null },
                    { "step-w1", false, "Ribbon.Architecture.Build", "Clique em Architecture > Wall > Wall: Architectural", 1, "ID_OBJECTS_WALL", "Abrir ferramenta Wall", "tut-walls-001", null },
                    { "step-w2", false, "Properties.TypeSelector", "No Type Selector, escolha 'Basic Wall: Generic - 200mm'", 2, null, "Selecionar tipo", "tut-walls-001", null },
                    { "step-w3", false, "Properties.UnconnectedHeight", "No painel Properties, ajuste 'Unconnected Height' para 3000mm", 3, null, "Definir altura", "tut-walls-001", null },
                    { "step-w4", false, "Canvas", "Clique dois pontos na vista de planta para criar a parede", 4, null, "Desenhar parede", "tut-walls-001", null },
                    { "step-w5", false, "Ribbon.Modify", "Pressione ESC ou clique em Modify para finalizar o comando", 5, "ID_OBJECTS_MODIFY", "Finalizar", "tut-walls-001", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CompanyId", "CreatedAt", "Email", "Level", "Name", "PasswordHash", "RefreshToken", "RefreshTokenExpiresAt", "Role", "XpPoints" },
                values: new object[] { "user-admin-001", "", "company-demo-001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@bimconcierge.com", 1, "Admin Demo", "$2a$11$arY.DKm7gaMSUkZqbI2lPunv4scXIOuhpzmLTPGm3SsWfHGeK9yc2", null, null, "Admin", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStandards_CompanyId",
                table: "CompanyStandards",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_CompanyId",
                table: "Licenses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Key",
                table: "Licenses",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Progress_TutorialId",
                table: "Progress",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_Progress_UserId",
                table: "Progress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialSteps_TutorialId",
                table: "TutorialSteps",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyStandards");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "Progress");

            migrationBuilder.DropTable(
                name: "TutorialSteps");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "Tutorials");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
