using Autofac;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Function
{
    internal class AutofacJobActivator : IJobActivatorEx
    {
        public T CreateInstance<T>()
        {
            // In practice, this method will not get called. We cannot safely resolve T here
            // because we don't have access to an ILifetimeScope, so it's better to just
            // throw.
            throw new NotSupportedException();
        }

        public T CreateInstance<T>(IFunctionInstanceEx functionInstance)
            where T : notnull
        {
            var lifetimeScope = functionInstance.InstanceServices
                .GetRequiredService<LifetimeScopeWrapper>()
                .Scope;

            // This is necessary because some dependencies of ILoggerFactory are registered
            // after FunctionsStartup.
            var loggerFactory = functionInstance.InstanceServices.GetRequiredService<ILoggerFactory>();
            lifetimeScope.Resolve<ILoggerFactory>(
                new NamedParameter(LoggerModule.LoggerFactoryParam, loggerFactory)
            );
            lifetimeScope.Resolve<ILogger>(
                new NamedParameter(LoggerModule.FunctionNameParam, functionInstance.FunctionDescriptor.LogName)
            );

            return lifetimeScope.Resolve<T>();
        }
    }
}
