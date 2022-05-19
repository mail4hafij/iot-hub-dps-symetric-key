using Autofac;
using System;

namespace Function
{
    internal sealed class LifetimeScopeWrapper : IDisposable
    {
        public ILifetimeScope Scope { get; }

        public LifetimeScopeWrapper(IContainer container)
        {
            Scope = container.BeginLifetimeScope();
        }

        public void Dispose()
        {
            Scope.Dispose();
        }
    }
}
