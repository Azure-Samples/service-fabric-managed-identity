namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;

    public sealed class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Launching vault probe..");
            VaultProbe.EndlessRun();
        }
    }
}
