using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface ITutorialService
{
    Task<List<Tutorial>> GetAllAsync(string? category = null);
    Task<Tutorial?> GetByIdAsync(string id);
    Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId);
    Task SaveProgressAsync(TutorialProgress progress);
    Task<bool> CompleteStepAsync(string userId, string tutorialId, int stepIndex);
}
