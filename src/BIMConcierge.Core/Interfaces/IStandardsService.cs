using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface IStandardsService
{
    Task<List<CompanyStandard>> GetStandardsAsync(string companyId);
    Task SaveStandardAsync(CompanyStandard standard);
    Task DeleteStandardAsync(string id);
    Task<List<CorrectionEvent>> ValidateModelAsync();
    Task<bool> AutoFixAsync(string correctionEventId);
}
