using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/ResponseFormatController")]
    [ApiController]
    public class ResponseFormatController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetSample()
        {
            var sampleData = new
            {
                Id = 1,
                Name = "Sample Item",
                Description = "This is a sample item."
            };

            return Ok(sampleData); // Trả về 200 OK với dữ liệu
        }
    }
}
