using System;
using Fim.Accord.Scopes;

namespace Fim.Accord.Resolvers
{
 
    
    /// <summary>
    /// Generic builder used for resolving fully specified componenets (not for opened generic types)
    /// </summary>
    internal class ConfiguredResolver<TPluginType,TImplType> : IConfiguredPluginResolverInternal<TPluginType,TImplType> where TImplType:TPluginType
    {

        
        protected readonly Container _owner;
        private readonly IScope _scope;
        protected  Type _pluginType;

        internal ConfiguredResolver(Container owner, IConfiguredFactoryInternal<TImplType> group,   IScope scope )
        {
            _owner = owner;
            
            _scope = scope;
            group.Add(this);
            Factory = group;
            _pluginType = typeof (TPluginType); //to be quick
            

        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }

       
        /// <summary>
        /// Canatine, which is owner of this resolver.
        /// </summary>
        public Container Owner
        {
            get
            {
                return this._owner;
            }
        }
        
        /// <summary>
        /// Scope of this plugin resolver.
        /// </summary>
        public IScope Scope
        {
            get
            {
                return _scope;
            }
        }

        /// <summary>
        /// <see cref="IConfiguredPluginResolverInternal.Factory"/>.
        /// </summary>
        IConfiguredFactoryInternal IConfiguredPluginResolverInternal.Factory
        {
            get { return Factory; }
           
        }

        /// <summary>
        /// Gets or sets the factory.
        /// </summary>
        /// <value>
        /// The factory.
        /// </value>
        public IConfiguredFactoryInternal<TImplType> Factory { get; internal set; }
       

        /// <summary>
        /// Type for what is this instance defined
        /// </summary>
        public virtual Type PluginType
        {
            get
            {
                return _pluginType;
            }
        }



        /// <summary>
        /// Gets the final plugin. (It can be proxied or not).
        /// </summary>
        /// <param name="ctx">The building context.</param>
        /// <returns></returns>
        public TPluginType Get(BuildingContext ctx)
        {
            ctx.CurrentResolver = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;

            return Factory.Get(ctx);

          
        }
        
        /// <summary>
        /// Gets the final plugin. (It can be proxied or not).
        /// </summary>
        /// <param name="ctx">The building context.</param>
        /// <returns></returns>
        object IConfiguredPluginResolverInternal.Get(BuildingContext ctx)
        {
            return Get(ctx);
        }


        /// <summary>
        /// CZ: Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// (Porovnej s <see cref="Dispose()"/>, která naopak ruší sebe včetně toho, že volá Dispose na všech držených komponentách.)
        /// </summary>
        public void UnRegister()
        {
            _owner.UnRegister(this);
        }

        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get; private set; }

       

        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every scope-holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;
            Disposed = true;
            _owner.UnRegister(this);
            if (!Factory.Disposed)
              if (Factory.Remove(this) == 0) //pokud vyjmeme posledni, tak skupinu zrušíme celou
                Factory.Dispose();
          
          
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ConfiguredResolver&lt;TPluginType, TImplType&gt;"/> is reclaimed by garbage collection.
        /// </summary>
        ~ConfiguredResolver()
        {
            Dispose(false);
        }
        #endregion



    }



    
    

    /// <summary>
    /// Factory used for resolving fully specified componenets (not for opened generic types)
    /// </summary>
    internal class ConfiguredPluginResolver : IConfiguredPluginResolverInternal
    {
        private readonly Container _owner;
        /// <summary>
        /// It is used when this resolver was registered by opened definition.
        /// </summary>
        public  readonly OpenedGenericPluginResolver Creator;
        /// <summary>
        /// type of plugin (ussually interface)
        /// </summary>
        private readonly Type _pluginType;
        
        /// <summary>
        /// scope used for this plugin
        /// </summary>
        private readonly IScope _scope;






        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredPluginResolver"/> class.
        /// </summary>
        internal ConfiguredPluginResolver(Container owner, Type pluginType, IConfiguredFactoryInternal group, IScope scope, OpenedGenericPluginResolver creator = null )
        {
            _owner = owner;
            Creator = creator;
            group.Add(this);
            Factory = group;
            _pluginType = pluginType;
            
            _scope = scope;
            Creator = creator;
            


        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }






        /// <summary>
        /// Gets the owner.
        /// </summary>
        public Container Owner
        {
            get
            {
                return this._owner;
            }
        }

        /// <summary>
        /// Curenty used scope (Singleton, per Thread, per HttpContext...)
        /// </summary>
        public IScope Scope
        {
            get
            {
                return _scope;
            }
        }
        /// <summary>
        /// Type for what  this instance is defined
        /// </summary>
        public Type PluginType
        {
            get
            {
                return _pluginType;
            }
        }



        /// <summary>
        /// Factory used to create new instance.
        /// </summary>
        public IConfiguredFactoryInternal Factory { get; 
            internal set;}


        /// <summary>
        /// Factory used to create new instance.
        /// </summary>
        IConfiguredFactoryInternal IConfiguredPluginResolverInternal.Factory
        {
            get { return this.Factory; }
            
        }


        /// <summary>
        /// Gets the final plugin. (It can be proxied or not).
        /// </summary>
        /// <param name="ctx">The building context.</param>
        /// <returns></returns>
        public object Get(BuildingContext ctx)
        {
            ctx.CurrentResolver = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;

            return Factory.Get(ctx);
        }
        
        /// <summary>
        /// CZ:Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// (Porovnej s <see cref="Dispose()"/>, která naopak ruší sebe včetně toho, že volá Dispose na všech držených komponentách.)
        /// </summary>
        public void UnRegister()
        {
            _owner.UnRegister(this);
        }

        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get; private set; }
        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every scope-holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            Disposed = true;
            _owner.UnRegister(this);
            if (!Factory.Disposed)
             if (Factory.Remove(this) == 0) //pokud vyjmeme posledni, tak skupinu zrušíme celou
                Factory.Dispose();
            
            
            
        }

        ~ConfiguredPluginResolver()
        {
            Dispose(false);
        }
        #endregion
    }
}