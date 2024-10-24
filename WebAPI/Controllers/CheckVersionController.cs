using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/CheckVersion")]
[ApiController]
public class CheckVersionController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        return Ok(new { Version = "1.0", Products = new[] { "Product1", "Product2" } });
    }
}

[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/CheckVersion")]
[ApiController]
public class CheckVersionV2Controller : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        return Ok(new { Version = "2.0", Products = new[] { "Product1", "Product2", "Product3" } });
    }
}
