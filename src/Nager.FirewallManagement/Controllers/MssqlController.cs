using Microsoft.AspNetCore.Mvc;
using Nager.FirewallManagement.Attributes;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace Nager.FirewallManagement.Controllers
{
    [ApiController]
    [ApiKey]
    [Route("[controller]")]
    public class MssqlController : ControllerBase
    {
        private readonly ILogger<RemoteDesktopController> _logger;
        private readonly string _ruleName = "Nager.FirewallManagement.Mssql";

        public MssqlController(ILogger<RemoteDesktopController> logger)
        {
            this._logger = logger;
        }

        /// <summary>
        /// Create a firewall rule for Microsoft SQL Server on port 1433
        /// </summary>
        [HttpPost]
        [Route("AllowAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult AllowAccess([FromQuery] string ipAddress)
        {
            if (!SingleIP.TryParse(ipAddress, out SingleIP singleIpAddress))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            try
            {
                var rule = FirewallManager.Instance.Rules.Where(o =>
                    o.Direction == FirewallDirection.Inbound &&
                    o.Name.Equals(this._ruleName)
                ).FirstOrDefault();

                if (rule != null)
                {
                    var items = rule.RemoteAddresses.ToList();
                    items.Add(singleIpAddress);

                    //Update an existing Rule
                    rule.RemoteAddresses = items.ToArray();

                    return StatusCode(StatusCodes.Status204NoContent);
                }

                //Create a new rule
                rule = FirewallManager.Instance.CreatePortRule(
                    FirewallProfiles.Public | FirewallProfiles.Private | FirewallProfiles.Domain,
                    this._ruleName,
                    FirewallAction.Allow,
                    1433,
                    FirewallProtocol.TCP
                );

                rule.Direction = FirewallDirection.Inbound;
                rule.Action = FirewallAction.Allow;
                rule.Scope = FirewallScope.All;
                rule.RemoteAddresses = new IAddress[] { singleIpAddress };

                FirewallManager.Instance.Rules.Add(rule);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(AllowAccess)}");
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// Remove firewall rule for Microsoft SQL Server
        /// </summary>
        [HttpDelete]
        [Route("RemoveAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult RemoveAccess()
        {

            try
            {
                var rule = FirewallManager.Instance.Rules.Where(o =>
                    o.Direction == FirewallDirection.Inbound &&
                    o.Name.Equals(this._ruleName)
                ).FirstOrDefault();

                if (rule == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }

                FirewallManager.Instance.Rules.Remove(rule);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(RemoveAccess)}");
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}