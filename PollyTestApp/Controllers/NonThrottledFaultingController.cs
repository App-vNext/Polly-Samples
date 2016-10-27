using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using PollyTestApp.Models;

namespace PollyTestApp.Controllers
{

    public class NonThrottledFaultingController : ApiController
    {
        readonly TimeSpan delay = TimeSpan.FromSeconds(20);

        // GET api/values
        /// <summary>
        /// Returns a simple string array of values.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Get()
        {
            PollyTestApp.WebApiConfig.ThrottleStore.Clear(); // Suppress throttling.

            // Fault, by returning the result slowly.
            Thread.Sleep(delay);

            PollyTestApp.WebApiConfig.ThrottleStore.Clear(); // Suppress throttling.

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        /// <summary>
        /// Accepts a simple integer parameter, which is written back to the
        /// response in the form of a simple message.  Faults by delaying for a 
        /// fixed period, before responding.
        /// </summary>
        /// <param name="id">Integer value that gets returned in the response
        /// message.</param>
        /// <returns>Returns: "Response from server to request #{id}"</returns>
        public Message Get(int id)
        {
            PollyTestApp.WebApiConfig.ThrottleStore.Clear(); // Suppress throttling.

            // Fault, by returning the result slowly.
            Thread.Sleep(delay);

            PollyTestApp.WebApiConfig.ThrottleStore.Clear(); // Suppress throttling.
            
            return new Message { Text = "Slow, faulting response from server to request #" + id };
        }

        //// POST api/values
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5
        //public void Delete(int id)
        //{
        //}
    }
}
