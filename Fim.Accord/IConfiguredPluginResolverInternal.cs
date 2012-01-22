namespace Fim.Accord
{
    /// <summary>
    ///  Configured component being ready to be resolved. INTERNAL.
    /// </summary>
    public interface  IConfiguredPluginResolverInternal:IConfiguredPluginResolver
    {

        /// <summary>
        /// Gets the final plugin. (It can be proxied or not).
        /// </summary>
        /// <param name="ctx">The building context.</param>
        /// <returns></returns>
        object Get(BuildingContext ctx);
        
        
        /// <summary>
        /// Factory used to create new instance.
        /// </summary>
        IConfiguredFactoryInternal Factory { get;  }


        /// <summary>
        /// Gets the owner.
        /// </summary>
        Container Owner { get;}
        
        
    }

    /// <summary>
    ///  Configured component being ready to be resolved. INTERNAL.
    /// </summary>
    /// <typeparam name="TPluginType">Plugin type - ussually interface for what is this factory registered</typeparam>
    /// <typeparam name="TImpType">>Concrete type for what is this factory registered</typeparam>
    public interface IConfiguredPluginResolverInternal<TPluginType,TImpType>  : IConfiguredPluginResolverInternal,IConfiguredPluginResolver<TPluginType,TImpType> where TImpType : TPluginType
    {

        /// <summary>
        /// Gets the strong typed factory.
        /// </summary>
        new IConfiguredFactoryInternal<TImpType> Factory { get; }
      

    }
    
}