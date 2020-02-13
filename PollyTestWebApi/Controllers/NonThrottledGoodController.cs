using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PollyTestWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NonThrottledGoodController : ControllerBase
    {
        // GET: api/NonThrottledGood
        [HttpGet(Name = "GetNonThrottledGood")]
        public IEnumerable<string> Get()
        {
            return new string[] {"Fast", "response", "from server"};
        }

        // GET: api/NonThrottledGood/5
        [HttpGet("{id}", Name = "GetNonThrottledGoodById")]
        public string Get(int id)
        {
            return "Fast response from server to request #" + id;
        }
    }
}