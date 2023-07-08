using MemoryMQ.Publisher;
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
    public async Task<IActionResult> Test(string body)
    {
        await _messagePublisher.PublishAsync("topic-a", body);
        // await _messagePublisher.PublishAsync("topic-b", body);
        return Ok();
    }
}