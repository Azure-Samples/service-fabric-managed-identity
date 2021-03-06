namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography;
    using System.Web;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Newtonsoft.Json;
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Security;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Azure.Core;


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
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback += ServerCertificateValidation;
            httpClient = new HttpClient(handler);
        }
        private bool ServerCertificateValidation(
            HttpRequestMessage httpRequest,
            X509Certificate2 cert,
            System.Security.Cryptography.X509Certificates.X509Chain certChain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                Log(LogLevel.Info, "ServerCertificateValidation error");
                return false;
            }
            return 0 == string.Compare(
                cert.GetCertHashString(),
                config.ManagedIdentityServerThumbprint,
                StringComparison.OrdinalIgnoreCase);
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
        public async Task<string> ProbeSecretAsync()
        {
            
            // Demonstrates using Azure.Identity to abstract away tokens, and use the managed identity credential to speak to KeyVault
            string azIdFetchedSecret = await ProbeSecretWithManagedIdentityCredentialAsync(config.VaultName, config.SecretName);

            // Demonstrates using ManagedIdentityTokenService to fetch a token, cache, and present to KeyVault
            string mitsTokenFetchedSecret = await ProbeSecretAsync(config.VaultName, config.SecretName, String.Empty);

            return $"Token fetched secret:\n{mitsTokenFetchedSecret}\n\nManagedIdentityCredential fetched secret:\n{azIdFetchedSecret}";
        }

        /// <summary>
        /// Probe the specified secret using ManagedIdentityCredential, displaying metadata on success.  
        /// </summary>
        /// <param name="vault">vault name</param>
        /// <param name="secret">secret name</param>
        /// <returns></returns>
        public async Task<string> ProbeSecretWithManagedIdentityCredentialAsync(string vaultUri, string secretName)
        {

            string response;

            try
            {
                ManagedIdentityCredential creds;
                KeyVaultSecret secret;

                try
                {
                    // Get a credential representing the service's SF Application Identity
                    creds = new ManagedIdentityCredential();

                    // Throw away token to allow early failures
                    creds.GetToken(new TokenRequestContext(new[] { "https://vault.azure.net" }));
                }
                catch (CredentialUnavailableException e)
                {
                    response = $"0x{e.HResult:X}: {e.Message} Encountered an exception accessing the service's managed identity. Was it deployed to Azure with an identity?";
                    Log(LogLevel.Info, response);
                    return response;
                }

                try
                {
                    SecretClient client = new SecretClient(new Uri(vaultUri), creds);
                    secret = (await client.GetSecretAsync(secretName)).Value;
                    response = PrintKeyVaultSecretMetadata(secret);
                }
                catch (RequestFailedException e)
                {
                    response = $"0x{e.HResult:X}: Status={e.Status} {e.Message} Encountered an exception fetching secret {secretName} from vault {vaultUri}";
                    Log(LogLevel.Info, response);
                }

                
            }
            catch (Exception e)
            {
                // handle generic errors here
                response = $"0x{e.HResult:X}: {e.Message} Encountered an exception fetching secret {secretName} from vault {vaultUri}";
            }

            Log(LogLevel.Info, response);
            return response;
        }

        /// <summary>
        /// Probe the specified secret using a token from ManagedIdentityTokenService, displaying metadata on success.  
        /// </summary>
        /// <param name="vaultUri">vault name</param>
        /// <param name="Name">secret name</param>
        /// <param name="version">secret version id</param>
        /// <returns></returns>
        public async Task<string> ProbeSecretAsync(string vaultUri, string Name, string version)
        {
            // initialize a KeyVault client with a managed identity-based authentication callback
            var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback((a, r, s) => { return AuthenticationCallbackAsync(a, r, s); }));

            Log(LogLevel.Info, $"\nRunning with configuration: \n\tobserved vault: {config.VaultName}\n\tobserved secret: {config.SecretName}\n\tMI endpoint: {config.ManagedIdentityEndpoint}\n\tMI auth code: {config.ManagedIdentityAuthenticationCode}\n\tMI auth header: {config.ManagedIdentityAuthenticationHeader}");
            string response = String.Empty;

            // start probe
            Log(LogLevel.Info, $"\n== {DateTime.UtcNow.ToString()}: Probing secret...");
            try
            {
                var secretResponse = await kvClient.GetSecretWithHttpMessagesAsync(vaultUri, Name, version)
                    .ConfigureAwait(false);

                if (secretResponse.Response.IsSuccessStatusCode)
                {
                    // use the secret: secretValue.Body.Value;
                    response = String.Format($"Successfully probed secret '{Name}' in vault '{vaultUri}': {PrintSecretBundleMetadata(secretResponse.Body)}");
                }
                else
                {
                    response = String.Format($"Non-critical error encountered retrieving secret '{Name}' in vault '{vaultUri}': {secretResponse.Response.ReasonPhrase} ({secretResponse.Response.StatusCode})");
                }
            }
            catch (Microsoft.Rest.ValidationException ve)
            {
                response = String.Format($"encountered REST validation exception 0x{ve.HResult.ToString("X")} trying to access '{Name}' in vault '{vaultUri}' from {ve.Source}: {ve.Message}");
            }
            catch (KeyVaultErrorException kvee)
            {
                response = String.Format($"encountered KeyVault exception 0x{kvee.HResult.ToString("X")} trying to access '{Name}' in vault '{vaultUri}': {kvee.Response.ReasonPhrase} ({kvee.Response.StatusCode})");
            }
            catch (Exception ex)
            {
                // handle generic errors here
                response = String.Format($"encountered exception 0x{ex.HResult.ToString("X")} trying to access '{Name}' in vault '{vaultUri}': {ex.Message}");
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

            strBuilder.AppendFormat($"id: {bundle.Id}\n");
            strBuilder.AppendFormat($"\tcontent type: {bundle.ContentType}\n");
            strBuilder.AppendFormat($"\tmanaged: {bundle.Managed}\n");
            strBuilder.AppendFormat($"\tenabled: {bundle.Attributes.Enabled}\n");
            strBuilder.AppendFormat($"\tnbf: {bundle.Attributes.NotBefore}\n");
            strBuilder.AppendFormat($"\texp: {bundle.Attributes.Expires}\n");
            strBuilder.AppendFormat($"\tcreated: {bundle.Attributes.Created}\n");
            strBuilder.AppendFormat($"\tupdated: {bundle.Attributes.Updated}\n");
            strBuilder.AppendFormat($"\trecoveryLevel: {bundle.Attributes.RecoveryLevel}\n");

            return strBuilder.ToString();
        }

        private string PrintKeyVaultSecretMetadata(KeyVaultSecret secret)
        {
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendFormat($"id: {secret.Id}\n");
            strBuilder.AppendFormat($"\tcontent type: {secret.Properties.ContentType}\n");
            strBuilder.AppendFormat($"\tmanaged: {secret.Properties.Managed}\n");
            strBuilder.AppendFormat($"\tenabled: {secret.Properties.Enabled}\n");
            strBuilder.AppendFormat($"\tnbf: {secret.Properties.NotBefore}\n");
            strBuilder.AppendFormat($"\texp: {secret.Properties.ExpiresOn}\n");
            strBuilder.AppendFormat($"\tcreated: {secret.Properties.CreatedOn}\n");
            strBuilder.AppendFormat($"\tupdated: {secret.Properties.UpdatedOn}\n");
            strBuilder.AppendFormat($"\trecoveryLevel: {secret.Properties.RecoveryLevel}\n");

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
