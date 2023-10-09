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
        [HttpGet("{id}", Name = "GetValuesById")]
        public string Get(int id)
        {
            return "Response from server to request #" + id;
        }
    }
}
