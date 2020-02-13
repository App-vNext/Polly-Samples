using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace PollyTestWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET: api/Values
        [HttpGet(Name = "GetValues")]
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // GET: api/Values/5
        [HttpGet("{id}", Name = "GetValuesById")]
        public string Get(int id)
        {
            return "Response from server to request #" + id;
        }
    }
}