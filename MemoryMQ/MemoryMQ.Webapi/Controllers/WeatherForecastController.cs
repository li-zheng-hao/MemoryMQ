using Microsoft.AspNetCore.Mvc;

namespace MemoryMQ.Webapi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IMessagePublisher _messagePublisher;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    [HttpGet]
    public IActionResult Test(string body)
    {
        _messagePublisher.Publish("topic-a", body);
        return Ok();
    }
}