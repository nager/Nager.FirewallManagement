using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace Nager.FirewallManagement.WebApi
{
    /// <summary>
    /// InfoController
    /// </summary>
    [RoutePrefix("Firewall")]
    public class FirewallController : ApiController
    {
        /// <summary>
        /// Get service version
        /// </summary>
        [HttpGet]
        [Route("Version")]
        [ResponseType(typeof(string))]
        public IHttpActionResult Version()
        {
            var rule = FirewallManager.Instance.Rules.Where(o =>
                o.Direction == FirewallDirection.Inbound &&
                o.Name.Equals("Allow Remote Desktop")
            ).FirstOrDefault();

            if (rule != null)
            {
                //Update an existing Rule
                rule.RemoteAddresses = new IAddress[]
                {
                    SingleIP.Parse("192.168.184.1"),
                    SingleIP.Parse("192.168.184.2")
                };

                return Ok();
            }

            //Create a new rule
            rule = FirewallManager.Instance.CreateApplicationRule(
                 FirewallManager.Instance.GetProfile().Type,
                 @"Allow Remote Desktop",
                 FirewallAction.Allow,
                 null
            );

            rule.Direction = FirewallDirection.Inbound;
            rule.LocalPorts = new ushort[] { 3389 };
            rule.Action = FirewallAction.Allow;
            rule.Protocol = FirewallProtocol.TCP;
            rule.Scope = FirewallScope.All;
            rule.Profiles = FirewallProfiles.Public | FirewallProfiles.Private;
            rule.RemoteAddresses = new IAddress[] { SingleIP.Parse("192.168.184.1") };

            FirewallManager.Instance.Rules.Add(rule);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return Ok($"{version.Major}.{version.Minor}.{version.Build}");
        }
    }
}
