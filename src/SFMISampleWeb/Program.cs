namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;
    using System.Threading;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Launching vault probe..");

            var builder = new WebHostBuilder()
                   .UseKestrel()
                   .ConfigureServices(s => s.AddMvc())
                   .Configure(a => a.UseMvc())
                   .UseUrls("http://+:80")
                   .Build();
            builder.Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
