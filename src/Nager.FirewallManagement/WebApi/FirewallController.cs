using log4net;
using System;
using System.Linq;
using System.Web.Http;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace Nager.FirewallManagement.WebApi
{
    /// <summary>
    /// FirewallController
    /// </summary>
    [RoutePrefix("Firewall")]
    public class FirewallController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FirewallController));

        /// <summary>
        /// Add an additional ip address
        /// </summary>
        [HttpPost]
        [Route("AddAdditionalIpForRdp")]
        //[ResponseType(typeof(string))]
        public IHttpActionResult AddAdditionalIpForRdp([FromUri] string ipAddress)
        {
            var ruleName = "Allow Remote Desktop";

            try
            {
                var rule = FirewallManager.Instance.Rules.Where(o =>
                    o.Direction == FirewallDirection.Inbound &&
                    o.Name.Equals(ruleName)
                ).FirstOrDefault();

                if (rule != null)
                {
                    var items = rule.RemoteAddresses.ToList();
                    items.Add(SingleIP.Parse(ipAddress));

                    //Update an existing Rule
                    rule.RemoteAddresses = items.ToArray();

                    return Ok();
                }

                //Create a new rule
                rule = FirewallManager.Instance.CreateApplicationRule(
                     FirewallManager.Instance.GetProfile().Type,
                     ruleName,
                     FirewallAction.Allow,
                     null
                );

                rule.Direction = FirewallDirection.Inbound;
                rule.LocalPorts = new ushort[] { 3389 };
                rule.Action = FirewallAction.Allow;
                rule.Protocol = FirewallProtocol.TCP;
                rule.Scope = FirewallScope.All;
                rule.Profiles = FirewallProfiles.Public | FirewallProfiles.Private;
                rule.RemoteAddresses = new IAddress[] { SingleIP.Parse(ipAddress) };

                FirewallManager.Instance.Rules.Add(rule);
                return Ok();
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(AddAdditionalIpForRdp)}", exception);
            }

            return InternalServerError();
        }
    }
}
