using Microsoft.AspNetCore.Mvc;

namespace WebApi.MsBuild.SystemTest.Controllers;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(GroupName = "v1-management")]
public class WeatherManagementController : ControllerBase
{
    private static readonly string[] Sources =
    {
        "Accuweather", "AerisWeather", "Foreca", "Open Weathermap", "National Oceanic and Atmospheric Administration",
    };

    [HttpGet(Name = "GetWeatherSources")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Get()
    {
        return this.Ok(Sources);
    }
}