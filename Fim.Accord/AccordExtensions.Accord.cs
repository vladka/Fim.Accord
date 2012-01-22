namespace Fim.Accord
{

    /// <summary>
    /// Extensions (shortcuts)
    /// </summary>
    public static class AccordExtensions
    {
        /// <summary>
        /// Uses the component itself.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="scope">The scope.</param>
        /// <returns></returns>
        public static IConfiguredFactory UseItself(this Container.Resolver resolver, IScope scope = null)
        {
            //todo: check types
            return resolver.Use(resolver.PluginTypes[resolver.PluginTypes.Count - 1],scope);
        }
        public static IConfiguredFactory<TConcreteType> UseItself<TConcreteType>(this Container.Resolver<TConcreteType> resolver, IScope scope = null)
        {
            //todo: check types
            return resolver.Use<TConcreteType>(scope);
        }
        
        public static IConfiguredFactory Use<TConcreteType>(this Container.Resolver resolver, TConcreteType instance, bool externallyOwned = true,IScope scope = null)
        {
            //todo: check types
            var configuredFactory = resolver.Use(() => instance, scope ?? resolver.Container);
            configuredFactory.BeforeRelease += (sender, args) => { args.RunDispose = !externallyOwned; };
            return configuredFactory;
        }
        public static IConfiguredFactory Use<TConcreteType,TPluginType>(this Container.Resolver<TPluginType> resolver, TConcreteType instance, bool externallyOwned = true, IScope scope = null) where TConcreteType : TPluginType
        {
            //todo: check types
            var configuredFactory = resolver.Use(() => instance, scope ?? resolver.Container);
            configuredFactory.BeforeRelease += (sender, args) => { args.RunDispose = !externallyOwned; };
            return configuredFactory;
        }
        public static IConfiguredFactory Use<TConcreteType, TPluginType, TAnotherPluginType>(this Container.Resolver<TPluginType, TAnotherPluginType> accord, TConcreteType instance, bool externallyOwned = true, IScope scope = null) where TConcreteType : TPluginType, TAnotherPluginType
        {
            //todo: check types
            var configuredFactory = accord.Use(() => instance, scope ?? accord.Container);
            configuredFactory.BeforeRelease += (sender, args) => { args.RunDispose = !externallyOwned; };
            return configuredFactory;
        }
        public static IConfiguredFactory Use<TConcreteType, TPluginType, TAnotherPluginType1, TAnotherPluginType2>(this Container.Resolver<TPluginType, TAnotherPluginType1, TAnotherPluginType2> resolver, TConcreteType instance, bool externallyOwned = true, IScope scope = null) where TConcreteType : TPluginType, TAnotherPluginType1, TAnotherPluginType2
        {
            //todo: check types
            var configuredFactory = resolver.Use(() => instance, scope ?? resolver.Container);
            configuredFactory.BeforeRelease += (sender, args) => { args.RunDispose = !externallyOwned; };
            return configuredFactory;
        }
        
    }
}