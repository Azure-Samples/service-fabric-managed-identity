using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    [Route("[controller]")]
    [ApiController]
    public class VaultController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<String>> Get()
        {
            // initialize a vault probe with settings read from environment
            VaultProbe vaultProbe = new VaultProbe(ProbeConfig.FromEnvironment());

            String result = await vaultProbe.ProbeSecretAsync()
                            .ConfigureAwait(false);
            return result;
        }
    }
}
