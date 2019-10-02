namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Sample class demonstrating access to KeyVault using Managed Identity.
    /// </summary>
    public sealed class VaultProbe
    {
        private HttpClient httpClient = null;
        private ProbeConfig config = null;
        private ResponseCache<ManagedIdentityTokenResponse> responseCache = new ResponseCache<ManagedIdentityTokenResponse>();

        /// <summary>
        /// Initializes a vault probe object with the specified config.
        /// </summary>
        /// <param name="config"></param>
        public VaultProbe(ProbeConfig config)
        {
            this.config = config;
            httpClient = new HttpClient();
        }

        /// <summary>
        /// Initializes a vault probe object with default configuration
        /// </summary>
        public VaultProbe()
            : this(ProbeConfig.FromEnvironment())
        { }

        /// <summary>
        /// Start an endless run of probing.
        /// </summary>
        public static void EndlessRun()
        {
            VaultProbe vaultProbe = null;
            TimeSpan downtime = TimeSpan.FromSeconds(2.0);

            for (; ; )
            {
                try
                {
                    // initialize a vault probe with settings read from environment
                    // one time only, but do retry
                    if (vaultProbe == null)
                    {
                        vaultProbe = new VaultProbe(ProbeConfig.FromEnvironment());
                        downtime = TimeSpan.FromSeconds(vaultProbe.config.ProbeIntervalInSeconds);
                    }

                    // start probe
                    var result = Task.Run(async () => await vaultProbe.ProbeSecretAsync().ConfigureAwait(false))
                        .GetAwaiter()
                        .GetResult();

                    Console.WriteLine($"sleeping for {downtime.Seconds}s..");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"encountered '{ex.Message}'; retrying in {downtime.Seconds}..");
                }

                // rest
                Thread.Sleep(downtime);
            }
        }

        /// <summary>
        /// Probe a vault secret according to default config.
        /// </summary>
        /// <returns></returns>
        public Task<string> ProbeSecretAsync()
        {
            return ProbeSecretAsync(config.VaultName, config.SecretName, String.Empty);
        }

        /// <summary>
        /// Probe the specified secret, displaying metadata on success.  
        /// </summary>
        /// <param name="vault">vault name</param>
        /// <param name="secret">secret name</param>
        /// <param name="version">secret version id</param>
        /// <returns></returns>
        public async Task<string> ProbeSecretAsync(string vault, string secret, string version)
        {
            // initialize a KeyVault client with a managed identity-based authentication callback
            var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback((a, r, s) => { return AuthenticationCallbackAsync(a, r, s); }));

            Log(LogLevel.Info, $"\nRunning with configuration: \n\tobserved vault: {config.VaultName}\n\tobserved secret: {config.SecretName}\n\tMI endpoint: {config.ManagedIdentityEndpoint}\n\tMI auth code: {config.ManagedIdentityAuthenticationCode}\n\tMI auth header: {config.ManagedIdentityAuthenticationHeader}");
            string response = String.Empty;

            // start probe
            Log(LogLevel.Info, $"\n== {DateTime.UtcNow.ToString()}: Probing secret...");
            try
            {
                var secretResponse = await kvClient.GetSecretWithHttpMessagesAsync(vault, secret, version)
                    .ConfigureAwait(false);

                if (secretResponse.Response.IsSuccessStatusCode)
                {
                    // use the secret: secretValue.Body.Value;
                    response = String.Format($"Successfully probed secret '{secret}' in vault '{vault}': {PrintSecretBundleMetadata(secretResponse.Body)}");
                }
                else
                {
                    response = String.Format($"Non-critical error encountered retrieving secret '{secret}' in vault '{vault}': {secretResponse.Response.ReasonPhrase} ({secretResponse.Response.StatusCode})");
                }
            }
            catch (Microsoft.Rest.ValidationException ve)
            {
                response = String.Format($"encountered REST validation exception 0x{ve.HResult.ToString("X")} trying to access '{secret}' in vault '{vault}' from {ve.Source}: {ve.Message}");
            }
            catch (KeyVaultErrorException kvee)
            {
                response = String.Format($"encountered KeyVault exception 0x{kvee.HResult.ToString("X")} trying to access '{secret}' in vault '{vault}': {kvee.Response.ReasonPhrase} ({kvee.Response.StatusCode})");
            }
            catch (Exception ex)
            {
                // handle generic errors here
                response = String.Format($"encountered exception 0x{ex.HResult.ToString("X")} trying to access '{secret}' in vault '{vault}': {ex.Message}");
            }

            Log(LogLevel.Info, response);

            return response;
        }

        /// <summary>
        /// KV authentication callback, using the application's managed identity.
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="resource"></param>
        /// <param name="scope"></param>
        /// <returns>Access token</returns>
        public async Task<string> AuthenticationCallbackAsync(string authority, string resource, string scope)
        {
            Log(LogLevel.Verbose, $"authentication callback invoked with: auth: '{authority}', resource: '{resource}', scope: '{scope}'");

            var encodedResource = HttpUtility.UrlEncode(resource);

            // first check the cache; use the resource as the caching key
            ManagedIdentityTokenResponse tokenResponse;
            if (responseCache.TryGetCachedItem(encodedResource, out tokenResponse))
            {
                Log(LogLevel.Verbose, $"cache hit for key '{encodedResource}'");

                return tokenResponse.AccessToken;
            }

            Log(LogLevel.Verbose, $"cache miss for key '{encodedResource}'");

            var requestUri = $"{config.ManagedIdentityEndpoint}?api-version={config.ManagedIdentityApiVersion}&resource={encodedResource}";
            Log(LogLevel.Verbose, $"request uri: {requestUri}");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Add(config.ManagedIdentityAuthenticationHeader, config.ManagedIdentityAuthenticationCode);
            Log(LogLevel.Verbose, $"added header '{config.ManagedIdentityAuthenticationHeader}': '{config.ManagedIdentityAuthenticationCode}'");

            var response = await httpClient.SendAsync(requestMessage)
                .ConfigureAwait(false);
            Log(LogLevel.Verbose, $"response status: success: {response.IsSuccessStatusCode}, status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var tokenResponseString = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            tokenResponse = JsonConvert.DeserializeObject<ManagedIdentityTokenResponse>(tokenResponseString);
            Log(LogLevel.Verbose, "deserialized token response; caching/returning access code..");

            // the response "expires_on" field is in number of seconds from Unix time; cache only if the token is valid for at least another 5s
            // The endpoint should not return an expired token, so pass it on even if it won't get cached.
            var expiration = DateTimeOffset.FromUnixTimeSeconds(Int32.Parse(tokenResponse.ExpiresOn));
            if (expiration > DateTimeOffset.UtcNow.AddSeconds(5.0))
                responseCache.AddOrUpdate(encodedResource, tokenResponse, expiration);

            return tokenResponse.AccessToken;
        }

        private string PrintSecretBundleMetadata(SecretBundle bundle)
        {
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendFormat($"\n\tid: {bundle.Id}\n");
            strBuilder.AppendFormat($"\tcontent type: {bundle.ContentType}\n");
            strBuilder.AppendFormat($"\tmanaged: {bundle.Managed}\n");
            strBuilder.AppendFormat($"\tattributes:\n");
            strBuilder.AppendFormat($"\t\tenabled: {bundle.Attributes.Enabled}\n");
            strBuilder.AppendFormat($"\t\tnbf: {bundle.Attributes.NotBefore}\n");
            strBuilder.AppendFormat($"\t\texp: {bundle.Attributes.Expires}\n");
            strBuilder.AppendFormat($"\t\tcreated: {bundle.Attributes.Created}\n");
            strBuilder.AppendFormat($"\t\tupdated: {bundle.Attributes.Updated}\n");
            strBuilder.AppendFormat($"\t\trecoveryLevel: {bundle.Attributes.RecoveryLevel}\n");

            return strBuilder.ToString();
        }

        private enum LogLevel
        {
            Info,
            Verbose
        };

        private void Log(LogLevel level, string message)
        {
            if (level != LogLevel.Verbose
                || config.DoVerboseLogging)
            {
                Console.WriteLine(message);
            }
        }
    }
}
