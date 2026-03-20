using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using Serilog;

namespace BIMConcierge.Infrastructure.Api;

public class StandardsService : IStandardsService
{
    private readonly IBimApiClient        _api;
    private readonly ILocalDatabase       _db;
    private readonly IRevitEventDispatcher _dispatcher;

    public StandardsService(IBimApiClient api, ILocalDatabase db, IRevitEventDispatcher dispatcher)
    {
        _api = api;
        _db = db;
        _dispatcher = dispatcher;
    }

    public async Task<List<CompanyStandard>> GetStandardsAsync(string companyId)
    {
        try
        {
            var list = await _api.GetAsync<List<CompanyStandard>>($"standards/{companyId}");
            if (list is not null) await _db.SaveStandardsAsync(list);
            return list ?? [];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "API call failed for standards — falling back to local cache");
            return await _db.GetStandardsAsync(companyId);
        }
    }

    public async Task SaveStandardAsync(CompanyStandard standard)
    {
        await _api.PutAsync<CompanyStandard, object>($"standards/{standard.Id}", standard);
        await _db.SaveStandardAsync(standard);
    }

    public async Task DeleteStandardAsync(string id)
    {
        await _api.DeleteAsync($"standards/{id}");
        await _db.DeleteStandardAsync(id);
    }

    public Task<List<CorrectionEvent>> ValidateModelAsync()
    {
        if (_dispatcher is RevitEventDispatcher dispatcher)
            return Task.FromResult(dispatcher.RunValidation());

        Log.Warning("ValidateModelAsync: dispatcher is not RevitEventDispatcher — validation skipped");
        return Task.FromResult(new List<CorrectionEvent>());
    }

    public async Task<bool> AutoFixAsync(string correctionEventId)
    {
        if (_dispatcher is RevitEventDispatcher dispatcher)
            return await dispatcher.TryAutoFixAsync(correctionEventId);

        Log.Warning("AutoFixAsync: dispatcher is not RevitEventDispatcher — auto-fix skipped");
        return false;
    }
}
