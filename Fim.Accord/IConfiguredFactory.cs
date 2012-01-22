using System;
using System.Collections.Generic;
using Fim.Accord.EventArgs;
using Fim.Accord.Scopes;

namespace Fim.Accord
{
    /// <summary>
    /// Extension methods dor IConfiguredFactory.
    /// </summary>
    public static class ConfiguredFactoryExtension
    {
        /// <summary>
        /// Makes component owned externally
        /// </summary>
        public static IConfiguredFactory ExternalOwned (this IConfiguredFactory factory)
        {
            factory.BeforeRelease += (sender, args) => args.RunDispose = false;
            return factory;
        }
        /// <summary>
        ///  Makes component owned externally
        /// </summary>
        public static IConfiguredFactory<T> ExternalOwned<T>(this IConfiguredFactory<T> factory)
        {
            factory.BeforeRelease += (sender, args) => args.RunDispose = false;
            return factory;

            
        }

    }

    /// <summary>
    /// Configured factory.
    /// </summary>
    public interface IConfiguredFactory : IEnumerable<IConfiguredPluginResolver>,IDisposable
    {

        /// <summary>
        /// Gets the <see cref="Agama.Resolver.IConfiguredPluginResolver"/> by the specified type.
        /// </summary>
        IConfiguredPluginResolver this[Type key] { get; }

        /// <summary>
        /// Occurs when component is created (by constructor or by any func).
        /// </summary>
        event EventHandler<AfterCreateEventArgs> AfterCreate;

        /// <summary>
        /// Occurs when component is releasing. 
        /// For transient scope (<see cref="TransientScope.Instance"/>) this event dont' occurs, because this components are not tracked.
        /// </summary>
        event EventHandler<BeforeReleaseEventArgs> BeforeRelease;
    }

    

    /// <summary>
    /// Configured factory.
    /// </summary>
    /// <typeparam name="T">Type which can be produces by this factory.</typeparam>
    public interface IConfiguredFactory<T> : IConfiguredFactory
    {

        /// <summary>
        /// Occurs when component is created (by constructor or by any func).
        /// </summary>
        new event EventHandler<AfterCreateEventArgs<T>> AfterCreate;

        /// <summary>
        /// Occurs before component is releasing. 
        /// For transient scope (<see cref="TransientScope.Instance"/>) this event dont' occurs, because this components are not tracked.
        /// </summary>
        new event EventHandler<BeforeReleaseEventArgs<T>> BeforeRelease;
    }



    /// <summary>
    /// Configured Factory for internal use.
    /// </summary>
    public interface IConfiguredFactoryInternal : IConfiguredFactory
    {
        
        
        /// <summary>
        /// Existing scoped values.
        /// </summary>
        ScopeTable ScopedValues { get; }


        /// <summary>
        /// Releases the component.
        /// </summary>
        /// <param name="instanceToRelease">The instance to release.</param>
        void ReleaseComponent(object instanceToRelease);
        

        /// <summary>
        /// Registers new resolver with this.
        /// </summary>
        /// <param name="resolver"></param>
        void Add(IConfiguredPluginResolverInternal resolver);


        /// <summary>
        /// Removes the specified resolver.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>Retuns remainings resolvers registered with this resolver</returns>
        int Remove(IConfiguredPluginResolverInternal resolver);

        /// <summary>
        /// Gets created instance specified by <paramref name="ctx"/>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        object Get(BuildingContext ctx);

        /// <summary>
        /// Gets a value indicating whether this <see cref="IConfiguredFactoryInternal"/> is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disposed; otherwise, <c>false</c>.
        /// </value>
        bool Disposed { get; }

    }



    /// <summary>
    /// Configured Factory for internal use.
    /// </summary>
    /// <typeparam name="TCommonType">The type of the common type.</typeparam>
    public interface IConfiguredFactoryInternal<TCommonType> : IConfiguredFactoryInternal, IConfiguredFactory<TCommonType> 
    {
        /// <summary>
        /// Gets created instance specified by <paramref name="ctx"/>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        new TCommonType Get(BuildingContext ctx);
        
    }
}