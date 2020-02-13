using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PollyTestWebApi.Controllers
{
    [ApiController]
    [Route("/")]
    public class DocumentationController : ControllerBase
    {
        [HttpGet]
        public RedirectResult Get()
        {
            return Redirect("/index.html");
        }
    }
}