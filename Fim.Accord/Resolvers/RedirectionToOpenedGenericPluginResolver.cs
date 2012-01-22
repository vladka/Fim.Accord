using System;
using Fim.Accord.EventArgs;

namespace Fim.Accord.Resolvers
{
    /// <summary>
    /// CZ: Pouze presmerovaci implementace, ktera deleguje volani na otevrenou definici
    /// </summary>
    public sealed class RedirectionToOpenedGenericPluginResolver : IConfiguredPluginResolverInternal
    {
        private readonly Type _pluginType;
        public readonly OpenedGenericPluginResolver Target;

        internal RedirectionToOpenedGenericPluginResolver(Type pluginType,OpenedGenericPluginResolver target)
        {
            _pluginType = pluginType;
            Target = target;
        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }


        IConfiguredFactoryInternal IConfiguredPluginResolverInternal.Factory { get { return ((IConfiguredPluginResolverInternal) Target).Factory; } }

        /// <summary>
        /// Gets the owner.
        /// </summary>
        public Container Owner
        {
            get { throw new NotSupportedException();}
        }

        public event EventHandler<AfterReuseEventArgs> AfterReuse;
        private void OnAfterGetScopedInstance(AfterReuseEventArgs args)
        {
            if (AfterReuse != null)
                AfterReuse(this, args);
        }
        public event EventHandler<AfterCreateEventArgs> AfterCreate;
        private void OnAfterBuiltNewComponent(AfterCreateEventArgs args)
        {
            if (AfterCreate != null)
                AfterCreate(this, args);
        }

        public event EventHandler<BeforeReleaseEventArgs> BeforeRelease;
        private void OnBeforeReleaseComponent(BeforeReleaseEventArgs args)
        {
            if (BeforeRelease != null)
                BeforeRelease(this, args);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="instanceToRelease"></param>
        /// <param name="runDispose"></param>
        public void ReleaseComponent(object instanceToRelease, bool runDispose)
        {
            //nemas smysl pro tento typ

        }



        /// <summary>
        /// Gets the final plugin. (It can be proxied or not).
        /// </summary>
        /// <param name="ctx">The building context.</param>
        /// <returns></returns>
        public object Get(BuildingContext ctx)
        {
            return  Target.Get(ctx);
        }

        /// <summary>
        /// Curenty used scope (Singleton, per Thread, per HttpContext...)
        /// </summary>
        public IScope Scope
        {
            get { return Target.Scope; }
        }

        /// <summary>
        /// Type for what is this instance defined
        /// </summary>
        public Type PluginType
        {
            get { return this._pluginType; }
        }

        /// <summary>
        /// CZ: Nedela nic v teto implementaci
        /// </summary>
        public void UnRegister()
        {
            //nedela nic, protoze RedirectImplementation nejde vyrobit primo uzivatelem, ale je vyrabena automaticky.
            //neni tedy potrena ji oderegistrovát.
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
            Target.Dispose();
        }

        ~RedirectionToOpenedGenericPluginResolver()
        {
            Dispose(false);
        }
        #endregion
    }
}