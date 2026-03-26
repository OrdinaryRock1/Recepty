using Microsoft.AspNetCore.Mvc;

namespace Recepty;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}