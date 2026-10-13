using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Settings;

/// <summary>
/// POST settings/save-settings
/// Updates existing settings only (does not create keys). Color.* values must be #RRGGBB.
/// </summary>
[ApiController]
[Route("settings/save-settings")]
public class SaveSettingsAction(MainDbContext dbContext) : ControllerBase {
    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] List<SaveSettingsRequestDTO> request) {
        if (request.Count == 0) {
            return BadRequest("Settings are required");
        }

        var keys = request.Select(x => x.Setting).ToList();
        var existing = await dbContext.Settings
            .Where(x => keys.Contains(x.Setting))
            .ToListAsync();

        foreach (var item in request) {
            if (string.IsNullOrWhiteSpace(item.Value)) {
                return BadRequest($"Value is required for '{item.Setting}'");
            }

            if (item.Setting.StartsWith("color.") && !HexColor.IsMatch(item.Value.Trim())) {
                return BadRequest($"'{item.Setting}' must be a hex color");
            }

            var row = existing.FirstOrDefault(x => x.Setting == item.Setting);
            if (row is null) {
                return BadRequest($"Unknown setting '{item.Setting}'");
            }

            row.Value = item.Value.Trim();
        }

        await dbContext.SaveChangesAsync();
        return Ok();
    }

    public class SaveSettingsRequestDTO {
        [Required]
        public string Setting { get; set; } = "";

        [Required]
        public string Value { get; set; } = "";
    }
}
