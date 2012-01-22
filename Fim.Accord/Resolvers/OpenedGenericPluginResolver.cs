using System;
using Fim.Accord.EventArgs;
using Fim.Accord.Scopes;

namespace Fim.Accord.Resolvers
{
   
    /// <summary>
    /// Factory for opened generics types.
    /// It is used only for creating concrete builder
    ///  (<see cref="ConfiguredPluginResolver"/> or <see cref="ConfiguredPluginResolver{TPluginType,TImplType}"/>).
    /// 
    /// </summary>
    public sealed class OpenedGenericPluginResolver : IConfiguredPluginResolverInternal
    {

        
        private readonly Container _container;
        private readonly Type _pluginType;
        private readonly IScope _scope;



        internal OpenedGenericPluginResolver(Container container, Type pluginType, IConfiguredFactoryInternal group, IScope scope)
        {
            _container = container;
            _pluginType = pluginType;
            _scope = scope;
            group.Add(this);
            Factory = group;
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
            get { return this._container; }
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
        /// Type for what is this instance defined
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
        public IConfiguredFactoryInternal Factory { get; internal set; }


        /// <summary>
        /// Occurs when component is reused by any other client (when component is already cached)
        /// </summary>
        public event EventHandler<AfterReuseEventArgs> AfterReuse;
        private void OnAfterGetScopedInstance(AfterReuseEventArgs args)
        {
            if (AfterReuse != null)
                AfterReuse(this, args);
        }
        /// <summary>
        /// Occurs after component is creating
        /// </summary>
        public event EventHandler<AfterCreateEventArgs> AfterCreate;
        private void OnAfterBuiltNewComponent(AfterCreateEventArgs args)
        {
            if (AfterCreate != null)
                AfterCreate(this, args);
        }
        /// <summary>
        /// Occurs before component is releasing.
        /// </summary>
        public event EventHandler<BeforeReleaseEventArgs> BeforeRelease;
        private void OnBeforeReleaseComponent(BeforeReleaseEventArgs args)
        {
            if (BeforeRelease != null)
                BeforeRelease(this, args);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="instanceToRelease"></param>
        /// <param name="runDispose"></param>
        public void ReleaseComponent(object instanceToRelease, bool runDispose)
        {
          //nemas smysl pro tento typ
        }

        /// <summary>
        /// Gets or sets the scoped values.
        /// </summary>
        /// <value>
        /// The scoped values.
        /// </value>
        internal ScopeTable ScopedValues
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        //tato metoda probiha pouze poprve pri prvni vyrobe konkretniho generickeho typu
        public object Get(BuildingContext ctx)
        {
            //todo vytvorit instanci a zaregistrovat do scopu
            ctx.CurrentResolver = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;
            var result = this.Factory.Get(ctx);
            return result;
        }

        /// <summary>
        /// Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// (Porovnej s <see cref="Dispose()"/>, která naopak ruší sebe včetně toho, že volá Dispose na všech držených komponentách.)
        /// </summary>
        public void UnRegister()
        {
            _container.UnRegister(this);
            
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

            _container.UnRegister(this);
            //todo: najit vsechny uzavrene implementatory (vznikle na zaklade teto definice) a ty take Disposovat ?

            Disposed = true;
        }

        ~OpenedGenericPluginResolver()
        {
            Dispose(false);
        }
        #endregion
    }
}