namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    public sealed class VaultProbeController : Controller
    {
        [Route("probe")]
        public async Task<string> Probe(string vault, string secret, string version)
        {
            // initialize a vault probe with settings read from environment
            var vaultProbe = new VaultProbe(ProbeConfig.FromEnvironment());

            // start probe
            var result = await vaultProbe.ProbeSecretAsync()
                .ConfigureAwait(false);

            return result;
        }
    }
}
