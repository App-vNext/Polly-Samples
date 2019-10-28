using System;
using System.Collections.Generic;
using System.Linq;
using Polly;
using Polly.Contrib.Simmy;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Net.Sockets;

namespace PollyTestApp.Controllers
{
    
    public class ValuesController : ApiController
    {
        // GET api/values
        /// <summary>
        /// Returns a simple string array of values.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Get()
        {
            // Following example causes the policy to throw SocketException with a probability of 20% if enabled
            var fault = new SocketException(errorCode: 10013);
            var chaosFault = MonkeyPolicy.InjectFault(
                (Exception)fault,
                injectionRate: 0.2,
                enabled: () => { return true; }
                );

            // Following example causes policy to introduce an added latency of 5 seconds to a randomly-selected 10% of the calls.
            var chaosLatency = MonkeyPolicy.InjectLatency(
                latency: TimeSpan.FromSeconds(5),
                injectionRate: 0.1,
                enabled: () => { return true; }
                );

            // Wrap the chaos policies using PolicyWrap
            var chaosPolicy= Policy.Wrap(chaosFault, chaosLatency);

            return chaosPolicy.Execute(() => new string[] { "value1", "value2" });
        }

        // GET api/values/5
        /// <summary>
        /// Accepts a simple integer parameter, which is written back to the
        /// response in the form of a simple message.
        /// </summary>
        /// <param name="id">Integer value that gets returned in the response
        /// message.</param>
        /// <returns>Returns: "Response from server to request #{id}"</returns>
        
        public Models.Message Get(int id)
        {
            // Following example causes the policy to throw SocketException with a probability of 20% if enabled
            var fault = new SocketException(errorCode: 10013);
            var chaosFault = MonkeyPolicy.InjectFault(
                (Exception)fault,
                injectionRate: 0.2,
                enabled: () => { return true; }
                );

            // Following example causes policy to introduce an added latency of 5 seconds to a randomly-selected 10% of the calls.
            var chaosLatency = MonkeyPolicy.InjectLatency(
                latency: TimeSpan.FromSeconds(5),
                injectionRate: 0.1,
                enabled: () => { return true; }
                );

            // Wrap the chaos policies using PolicyWrap
            var chaosPolicy = Policy.Wrap(chaosFault, chaosLatency);

            var msg = new Models.Message();
            msg.Text = chaosPolicy.Execute(() => "Response from server to request #" + id.ToString());
            return msg;
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
