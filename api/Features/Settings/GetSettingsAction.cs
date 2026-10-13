using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Settings;

/// <summary>
/// GET settings/get-settings
/// Returns all app settings (name, currency, theme colors) as key/value pairs.
/// </summary>
[Route("settings/get-settings")]
public class GetSettingsAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle() {
        var items = await dbContext.Settings
            .AsNoTracking()
            .OrderBy(x => x.Setting)
            .Select(x => new GetSettingsResponseDTO(x.Setting, x.Value))
            .ToListAsync();

        return Ok(items);
    }

    public record GetSettingsResponseDTO(string Setting, string Value);
}
