using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PollyTestWebApi.Controllers
{
    [EnableRateLimiting("ThreeRequestsPerFiveSeconds")]
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET: api/Values/5
        [HttpGet("{id}")]
        public string Get(int id) => $"Response from server to request #{id}";
    }
}
