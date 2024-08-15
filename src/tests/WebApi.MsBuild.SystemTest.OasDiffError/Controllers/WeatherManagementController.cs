using Microsoft.AspNetCore.Mvc;

namespace WebApi.MsBuild.SystemTest.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1-management")]
public class WeatherManagementController : ControllerBase
{
    private static readonly string[] Sources =
    {
        "Accuweather", "AerisWeather", "Foreca", "Open Weathermap", "National Oceanic and Atmospheric Administration",
    };

    private readonly ILogger<WeatherManagementController> _logger;

    public WeatherManagementController(ILogger<WeatherManagementController> logger)
    {
        this._logger = logger;
    }

    [HttpGet(Name = "GetWeatherSources")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IEnumerable<string> Get()
    {
        return Sources;
    }
}