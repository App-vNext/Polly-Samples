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
    public class NonThrottledFaultingController : ControllerBase
    {
        private readonly TimeSpan delay = TimeSpan.FromSeconds(20);

        // GET: api/NonThrottledFaulting
        [HttpGet(Name = "GetNonThrottled")]
        public async Task<IEnumerable<string>> Get()
        {
            await Task.Delay(delay);
            return new string[] {"Slow, faulting", "responses", "to slow controller"};
        }

        // GET: api/NonThrottledFaulting/5
        [HttpGet("{id}", Name = "GetThrottledFaultingById")]
        public async Task<string> Get(int id)
        {
            await Task.Delay(delay);
            return "Slow, faulting response from server to request #" + id;
        }
    }
}