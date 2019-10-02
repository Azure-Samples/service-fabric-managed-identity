namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;
    using System.Threading;

    public sealed class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Launching vault probe..");
            try
            {
                VaultProbe.EndlessRun();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            while(true) {
                Thread.Sleep(5000);
            }
        }
    }
}
