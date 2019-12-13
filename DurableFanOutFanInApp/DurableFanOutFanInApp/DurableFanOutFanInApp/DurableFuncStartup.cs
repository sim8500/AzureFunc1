using DurableFanOutFanInApp;
using DurableFanOutFanInApp.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DurableFuncStartup))]
namespace DurableFanOutFanInApp
{
    class DurableFuncStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddHttpClient<IGiosApiClient, GiosApiClient>();


            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddEnvironmentVariables();
            var configuration = configBuilder.Build();
        }
    }
}
