# BIM Concierge — Plugin Revit 2026

> **Assistente BIM Inteligente** para escritórios de arquitetura, engenharia e construção.  
> Treina novos colaboradores de forma guiada, padronizada e monitorada — sem consumir horas de quem já sabe.

---

## Visão Geral

| Problema                                              | Solução BIM Concierge                               |
|-------------------------------------------------------|-----------------------------------------------------|
| Onboarding lento e sem padronização                   | Tutoriais guiados passo a passo dentro do Revit     |
| Erros de modelagem descobertos tarde                  | Correção em tempo real com alertas automáticos      |
| Padrões da empresa aplicados de forma inconsistente   | Motor de regras com opção de correção automática    |
| Dificuldade de medir progresso de colaboradores       | Dashboard de progresso + gamificação com conquistas |

---

## Telas do Plugin

| Tela                          | Descrição                                            |
|-------------------------------|------------------------------------------------------|
| **Login + Boas-vindas**       | Autenticação com e-mail, senha e chave de licença    |
| **Painel Principal**          | Dashboard com métricas, acesso rápido e notificações |
| **Biblioteca de Tutoriais**   | Catálogo filtrável por categoria e dificuldade       |
| **Detalhes do Tutorial**      | Visão geral, etapas e botão "Iniciar"                |
| **Tutorial Guiado**           | Wizard passo a passo com progress bar                |
| **Correção em Tempo Real**    | Lista de violações com severidade e auto-fix         |
| **Padrões da Empresa**        | CRUD de regras BIM por categoria                     |
| **Progresso do Aluno**        | Histórico de tutoriais e métricas por colaborador    |
| **Conquistas & Gamificação**  | Badges, XP, níveis e ranking                         |

---

## Arquitetura

```
BIMConcierge/
├── src/
│   ├── BIMConcierge.Plugin/          # Entry point (IExternalApplication + IExternalCommand)
│   │   ├── BIMConciergeApplication.cs
│   │   ├── Commands/
│   │   │   ├── OpenDashboardCommand.cs
│   │   │   └── StartTutorialCommand.cs
│   │   ├── Ribbon/
│   │   │   └── RibbonBuilder.cs
│   │   └── Resources/
│   │       └── BIMConcierge.addin
│   │
│   ├── BIMConcierge.UI/              # WPF (MVVM com CommunityToolkit.Mvvm)
│   │   ├── Views/
│   │   │   ├── LoginWindow.xaml(.cs)
│   │   │   ├── DashboardWindow.xaml(.cs)
│   │   │   └── TutorialWindow.xaml(.cs)
│   │   ├── ViewModels/
│   │   │   ├── LoginViewModel.cs
│   │   │   ├── DashboardViewModel.cs
│   │   │   └── TutorialViewModel.cs
│   │   ├── Themes/
│   │   │   └── BIMConciergeTheme.xaml
│   │   └── Converters/
│   │       └── Converters.cs
│   │
│   ├── BIMConcierge.Core/            # Domain puro (sem dependências externas)
│   │   ├── Models/
│   │   │   ├── User.cs
│   │   │   ├── License.cs
│   │   │   ├── Tutorial.cs
│   │   │   ├── Progress.cs
│   │   │   └── CompanyStandard.cs
│   │   └── Interfaces/
│   │       └── IServices.cs
│   │
│   └── BIMConcierge.Infrastructure/  # Implementações: API, Auth, DB local
│       ├── Auth/
│       │   ├── AuthService.cs
│       │   └── TokenStore.cs         # DPAPI (Windows, CurrentUser scope)
│       ├── License/
│       │   └── LicenseService.cs
│       ├── Api/
│       │   ├── BimApiClient.cs
│       │   └── ServiceImplementations.cs
│       ├── Revit/
│       │   └── RevitEventDispatcher.cs
│       ├── Persistence/
│       │   └── SqliteDatabase.cs     # Cache offline via SQLite-net-pcl
│       └── DependencyInjection.cs
│
└── tests/
    └── BIMConcierge.Core.Tests/
        ├── AuthServiceTests.cs
        └── ModelTests.cs
```

---

## Stack Tecnológico

| Camada       | Tecnologia                                   |
|--------------|----------------------------------------------|
| Plataforma   | Revit 2026 API, .NET 8, Windows x64, Visual Studio 2026 |
| UI           | WPF + XAML, CommunityToolkit.Mvvm 8.x        |
| UI Design    | Material Symbols, Inter font, palette #6A7D90 |
| HTTP         | `HttpClient` com `IHttpClientFactory`         |
| Autenticação | JWT Bearer + Windows DPAPI (token store)      |
| Cache local  | SQLite via `SQLite-net-pcl`                   |
| Logging      | Serilog (file sink, rolling diário)           |
| Testes       | xUnit + Moq + FluentAssertions                |

---

## Pré-requisitos

- **Revit 2026** instalado (x64)
- **.NET 8 SDK** (https://dotnet.microsoft.com/download)
- **Visual Studio 2026 Community / Professional / Enterprise** (Stable Channel)
- Variável de ambiente `REVIT_API_PATH` apontando para a pasta de instalação do Revit  
  Ex.: `C:\Program Files\Autodesk\Revit 2026`

---

## Como compilar

```bash
# 1. Clonar / abrir o projeto
cd BIMConcierge

# 2. Restaurar pacotes
dotnet restore

# 3. Compilar (modo Debug — copia .addin + .dll para %APPDATA%\Autodesk\Revit\Addins\2026 automaticamente)
dotnet build --configuration Debug

# 4. Executar testes
dotnet test
```

---

## Instalação Manual

1. Compile em `Release`.
2. Copie `BIMConcierge.dll` e todas as dependências para:  
   `%APPDATA%\Autodesk\Revit\Addins\2026\`
3. Copie `BIMConcierge.addin` para o mesmo diretório.
4. Inicie o Revit 2026 — a aba **BIM Concierge** aparecerá na Ribbon.

---

## Design System

| Token              | Valor      |
|--------------------|------------|
| Primary            | `#6A7D90`  |
| Background Dark    | `#17191B`  |
| Surface Dark       | `#1C1F22`  |
| Border Dark        | `#2A2F35`  |
| Text Primary       | `#F1F5F9`  |
| Text Secondary     | `#94A3B8`  |
| Error              | `#EF4444`  |
| Success            | `#22C55E`  |
| Warning            | `#F59E0B`  |
| Font               | Inter      |
| Border Radius Md   | 8px        |
| Border Radius Lg   | 12px       |

---

## Configuração da API Backend

Por padrão o plugin aponta para `https://api.bimconcierge.io/v1/`.  
Para ambientes on-premise, defina a variável de ambiente:

```
BIMCONCIERGE_API_URL=https://seu-servidor/api/v1/
```

---

## Roadmap

- [ ] Implementar `SqliteDatabase` com SQLite-net-pcl (CRUD completo)
- [ ] Construir `RevitEventDispatcher` com motor de regras de standards
- [ ] Completar XAML das views: `TutorialWindow`, `CorrectionsView`, `StandardsView`, `ProgressView`, `AchievementsView`
- [ ] Implementar `ExternalEvent` para aplicar correções automáticas via thread do Revit
- [ ] Adicionar suporte a múltiplos idiomas (PT-BR / EN)
- [ ] Testes de integração com Revit API (via `RevitTestFramework`)
- [ ] Pipeline CI/CD com GitHub Actions

---

## Licença

Proprietário — BIM Concierge © 2026. Todos os direitos reservados.
