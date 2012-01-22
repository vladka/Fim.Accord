using System;
using System.Collections;
using System.Collections.Generic;
using Fim.Accord.EventArgs;

namespace Fim.Accord
{

    /// <summary>
    /// Configured factory
    /// </summary>
    internal class ConfiguredFactory : IConfiguredFactoryInternal
    {
        private readonly Func<BuildingContext, object> _factory;

        internal ConfiguredFactory(Func<BuildingContext, object> factory)
        {
            this._factory = factory;
        }
        protected ConfiguredFactory()
        { }

        private List<IConfiguredPluginResolverInternal> _all = new List<IConfiguredPluginResolverInternal>();

        /// <summary>
        /// Registers new resolver with this.
        /// </summary>
        /// <param name="resolver"></param>
        public void Add(IConfiguredPluginResolverInternal resolver)
        {
            _all.Add(resolver);
        }
        /// <summary>
        /// Removes the specified resolver.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>
        /// Returns remainings resolvers registered with this resolver
        /// </returns>
        public int Remove(IConfiguredPluginResolverInternal resolver)
        {
            _all.Remove(resolver);
            return _all.Count;
        }
        private Object _thisLock = new Object();
        private ScopeTable _scopedValues;
        /// <summary>
        /// Existing scoped values.
        /// </summary>
        public ScopeTable ScopedValues
        {
            get
            {
                if (_scopedValues != null)
                    return _scopedValues;
                else
                {
                    lock (_thisLock)
                    {
                        if (_scopedValues != null)
                            return _scopedValues;
                        _scopedValues = new ScopeTable(this._all[0].Owner.GlobalScopeTable, this);
                    }
                    return _scopedValues;
                }
            }
        }

        //private readonly object _objectLock = new Object();



        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IConfiguredPluginResolver> GetEnumerator()
        {
            return _all.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// Gets the <see cref="Agama.Resolver.IConfiguredPluginResolver"/> with the specified key.
        /// </summary>
        public IConfiguredPluginResolver this[Type key]
        {
            get
            {
                return _all.Find(x => x.PluginType == key);
            }
        }

        #region releasing component

        /// <summary>
        /// Creates the before release args.
        /// </summary>
        /// <param name="instanceToRelease">The instance to release.</param>
        /// <returns></returns>
        protected virtual BeforeReleaseEventArgs CreateBeforeReleaseArgs(object instanceToRelease)
        {
            return new BeforeReleaseEventArgs(instanceToRelease);
        }

        /// <summary>
        /// Releases the component.
        /// </summary>
        /// <param name="instanceToRelease">The instance to release.</param>
        public void ReleaseComponent(object instanceToRelease)
        {
            var args = CreateBeforeReleaseArgs(instanceToRelease);

            OnBeforeRelease(args);

            if (args.RunDispose)
            {
                var disposable = instanceToRelease as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

        }

        /// <summary>
        /// Occurs when component is releasing.
        /// For transient scope (<see cref="TransientScope.Instance"/>) this event dont' occurs, because this components are not tracked.
        /// </summary>
        public event EventHandler<BeforeReleaseEventArgs> BeforeRelease;

        protected virtual void OnBeforeRelease(BeforeReleaseEventArgs args)
        {
            if (BeforeRelease != null)
                BeforeRelease(this, args);
        }
        #endregion

        #region creating component

        public event EventHandler<AfterCreateEventArgs> AfterCreate;

        protected virtual AfterCreateEventArgs CreateAfterCreateEventArgs(BuildingContext ctx)
        {
            return new AfterCreateEventArgs(this._factory(ctx));
        }

        protected virtual void OnAfterCreate(AfterCreateEventArgs args)
        {
            if (AfterCreate != null)
                AfterCreate(this, args);
        }

        protected Container Container { get { return this._all[0].Owner; } }
        
        public object Get(BuildingContext ctx)
        {
            var scopeObj = ctx.CurrentResolver.Scope.Context;
            if (scopeObj == null)
            {

                //scope cache is not needed
                var args = CreateAfterCreateEventArgs(ctx);
                OnAfterCreate(args);
                return args.Component;
            }

            object result;
            if (FindExistingValue(scopeObj, out result))
                return result;
            lock (scopeObj)
            {
                if (FindExistingValue(scopeObj, out result))
                    return result;
                
                var args3 = CreateAfterCreateEventArgs(ctx);
                OnAfterCreate(args3);
                ScopedValues.RegisterScopedObject(scopeObj, args3.Component);
                return args3.Component;
            }
        }

        private bool FindExistingValue(object scopeObj, out object result)
        {
            result = ScopedValues.FindValueByScope(scopeObj);
            if (result!=null )
            {
                var args2 = new AfterReuseEventArgs(result);
                //OnAfterGetScopedInstance(args2);todo:
                result = args2.Component;
                return true;
            }
            return false;
        }
        #endregion



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

        private void ReleaseAllScoped()
        {
            this.ScopedValues.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            if (Disposed)
                return;


            Disposed = true;

            if (_scopedValues != null) //pokud byly ytvoreny scopovane hodnoty
                _scopedValues.Dispose();
            foreach (var i in _all)
                i.Dispose();


            AfterCreate = null;
            BeforeRelease = null;

        }

        ~ConfiguredFactory()
        {
            Dispose(false);
        }
        #endregion

    }
    
    /// <summary>
    /// Default Configured factory.
    /// </summary>
    /// <typeparam name="TImplType"></typeparam>
    internal class ConfiguredFactory<TImplType> : ConfiguredFactory, IConfiguredFactoryInternal<TImplType>
    {
        /// <summary>
        /// factory func.
        /// </summary>
        private readonly Func<BuildingContext, TImplType> _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredFactory{TImplType}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        internal ConfiguredFactory(Func<BuildingContext, TImplType> factory)
            : base()
        {
            _factory = factory;
        }

        #region releasing component
        protected override BeforeReleaseEventArgs CreateBeforeReleaseArgs(object instanceToRelease)
        {
            return new BeforeReleaseEventArgs<TImplType>((TImplType)instanceToRelease);
        }

        public new event EventHandler<BeforeReleaseEventArgs<TImplType>> BeforeRelease;

        protected override void OnBeforeRelease(BeforeReleaseEventArgs args)
        {
            base.OnBeforeRelease(args); //volani handleru registrovanych na predka

            if (BeforeRelease != null)  //volani handleru registrovanych na tento typ
                BeforeRelease(this, (BeforeReleaseEventArgs<TImplType>)args);


        }
        #endregion

        #region creating component

        protected override AfterCreateEventArgs CreateAfterCreateEventArgs(BuildingContext ctx)
        {
            return new AfterCreateEventArgs<TImplType>(_factory(ctx));
        }

        /// <summary>
        /// <see cref="IConfiguredFactory{T}"/>
        /// </summary>
        public new event EventHandler<AfterCreateEventArgs<TImplType>> AfterCreate;

        protected virtual void OnAfterCreate(AfterCreateEventArgs<TImplType> args)
        {
            base.OnAfterCreate(args);
            
            if (AfterCreate != null)
                AfterCreate(this, args);
        }
       
        public new TImplType Get(BuildingContext ctx)
        {


            var scopeObj = ctx.CurrentResolver.Scope.Context;
            if (scopeObj == null)
            {

                //scope cache is not needed
                var args = new AfterCreateEventArgs<TImplType>(_factory(ctx));
                OnAfterCreate(args);
                return args.Component;
            }
            TImplType result;
            if (FindExistingValue(scopeObj,out result))
                return result;
            lock (scopeObj)
            {
                if (FindExistingValue(scopeObj, out result))
                    return result;

                var args3 = new AfterCreateEventArgs<TImplType>(_factory(ctx));
                OnAfterCreate(args3);
                ScopedValues.RegisterScopedObject(scopeObj, args3.Component);

                return args3.Component;
            }
        }

        private bool FindExistingValue(object scopeObj,out TImplType result)
        {
             result = (TImplType) ScopedValues.FindValueByScope(scopeObj);
            if (!Object.Equals(result, default(TImplType))) //if not null
            {
                var args2 = new AfterReuseEventArgs<TImplType>(result);
                //OnAfterGetScopedInstance(args2);todo:
                result= args2.Component;
                return true;
            }
            return false;
        }

        #endregion
       
    }
}