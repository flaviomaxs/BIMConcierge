namespace BIMConcierge.Api.Dtos;

// ── Auth ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password, string LicenseKey);
public record RefreshRequest(string RefreshToken);
public record LoginResponse(bool Success, string? Message, string AccessToken, string RefreshToken, UserDto User);

// ── License ──────────────────────────────────────────────────────────────────
public record ActivateRequest(string LicenseKey, string UserId);
public record ActivateResponse(bool Success, string? Message);

// ── DTOs (match plugin Core.Models exactly) ──────────────────────────────────
public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public int XpPoints { get; init; }
    public int Level { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record LicenseDto
{
    public string Key { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public int MaxSeats { get; init; }
    public int UsedSeats { get; init; }
    public string Type { get; init; } = "Professional";
    public DateTime ExpiresAt { get; init; }
}

public record TutorialDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public int DurationMins { get; init; }
    public int StepCount { get; init; }
    public string ThumbnailUrl { get; init; } = string.Empty;
    public bool IsCompanyOwned { get; init; }
    public List<TutorialStepDto> Steps { get; init; } = [];
}

public record TutorialStepDto
{
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Instruction { get; init; } = string.Empty;
    public string? RevitCommand { get; init; }
    public string? HighlightZone { get; init; }
    public bool AutoApplicable { get; init; }
    public string? ValidationRule { get; init; }
}

public record ProgressDto
{
    public string UserId { get; init; } = string.Empty;
    public string TutorialId { get; init; } = string.Empty;
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public bool IsCompleted { get; init; }
    public int ScorePercent { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record AchievementDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int XpReward { get; init; }
    public bool IsUnlocked { get; init; }
    public DateTime? UnlockedAt { get; init; }
}

public record CompanyStandardDto
{
    public string Id { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Rule { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool AutoFix { get; init; }
    public int AlertLevel { get; init; }
}

public record XpRequest(int Amount);
