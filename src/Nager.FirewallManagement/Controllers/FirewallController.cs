using Microsoft.AspNetCore.Mvc;
using Nager.FirewallManagement.Attributes;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace Nager.FirewallManagement.Controllers
{
    [ApiController]
    [ApiKey]
    [Route("[controller]")]
    public class FirewallController : ControllerBase
    {
        private readonly ILogger<FirewallController> _logger;

        public FirewallController(ILogger<FirewallController> logger)
        {
            this._logger = logger;
        }

        /// <summary>
        /// Add an additional ip address
        /// </summary>
        [HttpPost]
        [Route("AddAdditionalIpForRdp")]
        public ActionResult AddAdditionalIpForRdp([FromQuery] string ipAddress)
        {
            var ruleName = "Allow Remote Desktop";

            if (!SingleIP.TryParse(ipAddress, out SingleIP singleIpAddress))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            try
            {
                var rule = FirewallManager.Instance.Rules.Where(o =>
                    o.Direction == FirewallDirection.Inbound &&
                    o.Name.Equals(ruleName)
                ).FirstOrDefault();

                if (rule != null)
                {
                    var items = rule.RemoteAddresses.ToList();
                    items.Add(singleIpAddress);

                    //Update an existing Rule
                    rule.RemoteAddresses = items.ToArray();

                    return Ok();
                }

                //Create a new rule
                rule = FirewallManager.Instance.CreatePortRule(
                    FirewallProfiles.Public,
                    ruleName,
                    FirewallAction.Allow,
                    3389,
                    FirewallProtocol.TCP
                );

                rule.Direction = FirewallDirection.Inbound;
                rule.Action = FirewallAction.Allow;
                rule.Scope = FirewallScope.All;
                rule.RemoteAddresses = new IAddress[] { singleIpAddress };

                FirewallManager.Instance.Rules.Add(rule);
                return StatusCode(StatusCodes.Status200OK);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(AddAdditionalIpForRdp)}");
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}