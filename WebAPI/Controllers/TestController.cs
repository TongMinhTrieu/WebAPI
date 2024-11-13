using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{

    //Test Timeout
    [ApiController]
    [Route("api/TestTimeout")]
    public class TestController : ControllerBase
    {
        [HttpGet("customdelegatepolicy/{waitSeconds:int}")]
        [RequestTimeout("customdelegatepolicy")]
        public async Task<IActionResult> GetCustomerWithCustomDelegateAsync([FromRoute] int waitSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), HttpContext.RequestAborted);
            return Ok();
        }
    }
}
