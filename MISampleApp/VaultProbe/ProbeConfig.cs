namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;

    /// <summary>
    /// Config object for vault probe.
    /// </summary>
    public sealed class ProbeConfig
    {
        private static readonly string _vaultEnvVarName = "sfmi_observed_vault";
        private static readonly string _secretEnvVarName = "sfmi_observed_secret";
        private static readonly string _miEndpointEnvVarName = "IDENTITY_ENDPOINT";
        private static readonly string _miAuthCodeEnvVarName = "IDENTITY_HEADER";
        private static readonly string _miServerCertThumbprintEnvVarName = "IDENTITY_SERVER_THUMBPRINT";
        private static readonly string _miApiVersionEnvVarName ="IDENTITY_API_VERSION";
        private static readonly string _miAuthCodeHeaderName = "secret";
        private static readonly string _verboseLoggingEnvVarName = "sfmi_verbose_logging";
        private static readonly string _pollIntervalInSecEnvVarName = "sfmi_poll_interval";

        /// <summary>
        /// Vault name (not an URI)
        /// </summary>
        public string VaultName { get; set; }

        /// <summary>
        /// Secret name
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Managed identity endpoint uri.
        /// </summary>
        public string ManagedIdentityEndpoint { get; set; }

        /// <summary>
        /// Managed identity authentication code.
        /// </summary>
        public string ManagedIdentityAuthenticationCode { get; set; }

        /// <summary>
        /// Header name for managed identity authentication code.
        /// </summary>
        public string ManagedIdentityAuthenticationHeader { get; set; }

        /// <summary>
        /// Toggle verbose logging
        /// </summary>
        public bool DoVerboseLogging { get; set; }

        /// <summary>
        /// Interval between successive probes.
        /// </summary>
        public int ProbeIntervalInSeconds { get; set; }

        /// <summary>
        /// SF Managed Identity API version
        /// </summary>
        public string ManagedIdentityApiVersion { get; set; }

          /// <summary>
        /// SF Managed Identity Server Certificate Thumbprint
        /// </summary>
        public string ManagedIdentityServerThumbprint { get; set; }
        /// <summary>
        /// Build a probe config object from environment variables.
        /// </summary>
        /// <returns></returns>
        public static ProbeConfig FromEnvironment()
        {
            bool.TryParse(Environment.GetEnvironmentVariable(_verboseLoggingEnvVarName), out bool doVerboseLogging);
            if (!int.TryParse(Environment.GetEnvironmentVariable(_pollIntervalInSecEnvVarName), out int pollIntervalInS))
            {
                // use a safe default
                pollIntervalInS = 60;
            }

            var managedIdentityAuthenticationCode = Environment.GetEnvironmentVariable(_miAuthCodeEnvVarName);
            if (String.IsNullOrWhiteSpace(managedIdentityAuthenticationCode))
            {
                throw new ArgumentNullException("ManagedIdentityAuthenticationCode", "environment does not contain the expected variables (min: MI endpoint and authentication code");
            }

            var managedIdentityEndpoint = Environment.GetEnvironmentVariable(_miEndpointEnvVarName);
            if (String.IsNullOrWhiteSpace(managedIdentityEndpoint))
            {
                throw new ArgumentNullException("ManagedIdentityEndpoint", "environment does not contain the expected variables (min: MI endpoint and authentication code");
            }

            var secretName = Environment.GetEnvironmentVariable(_secretEnvVarName);
            if (String.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentNullException("SecretName", "environment does not contain the expected variables (min: MI endpoint and authentication code");
            }

            var serverThumbprint = Environment.GetEnvironmentVariable(_miServerCertThumbprintEnvVarName);
            if (String.IsNullOrWhiteSpace(serverThumbprint))
            {
                throw new ArgumentNullException("ServerThumbprint", "environment does not contain the expected variable IDENTITY_SERVER_THUMBPRINT");
            }

            var latestApiVersion = Environment.GetEnvironmentVariable(_miApiVersionEnvVarName);
            if (String.IsNullOrWhiteSpace(latestApiVersion))
            {
                throw new ArgumentNullException("APIVersion", "environment does not contain the expected variable IDENTITY_API_VERSION");
            }

            var vaultName = Environment.GetEnvironmentVariable(_vaultEnvVarName);
            if (String.IsNullOrWhiteSpace(vaultName))
            {
                throw new ArgumentNullException("VaultName", "environment does not contain the expected variables (min: MI endpoint and authentication code");
            }

            return new ProbeConfig
            {
                ManagedIdentityAuthenticationCode = managedIdentityAuthenticationCode,
                ManagedIdentityAuthenticationHeader = _miAuthCodeHeaderName,
                ManagedIdentityEndpoint = managedIdentityEndpoint,
                ManagedIdentityServerThumbprint = serverThumbprint,
                ManagedIdentityApiVersion = latestApiVersion,
                SecretName = secretName,
                VaultName = vaultName,
                DoVerboseLogging = doVerboseLogging,
                ProbeIntervalInSeconds = pollIntervalInS
            };
        }
    }
}
