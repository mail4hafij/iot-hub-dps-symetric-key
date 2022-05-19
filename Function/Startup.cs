using Autofac;
using Autofac.Extensions.DependencyInjection;
using Function.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: FunctionsStartup(typeof(Function.Startup))]

namespace Function
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Use IServiceCollection.Add extension method to add features as needed, e.g.
            // builder.Services.AddDataProtection();

            builder.Services.AddSingleton(GetContainer(builder.Services));

            // Important: Use AddScoped so our Autofac lifetime scope gets disposed
            // when the function finishes executing
            builder.Services.AddScoped<LifetimeScopeWrapper>();

            builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IJobActivator), typeof(AutofacJobActivator)));
            builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IJobActivatorEx), typeof(AutofacJobActivator)));
        }

        private static IContainer GetContainer(IServiceCollection serviceCollection)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(serviceCollection);
            containerBuilder.RegisterModule<LoggerModule>();


            // Autofac START
            
            // All the functions.
            containerBuilder.RegisterAssemblyTypes(typeof(Startup).Assembly)
                .InNamespaceOf<IotHubEndPointTrigger>();
            containerBuilder.RegisterAssemblyTypes(typeof(Startup).Assembly)
                .InNamespaceOf<ServiceBusQueueTigger>();
            
            // Autofac END


            return containerBuilder.Build();
        }
    }
}

