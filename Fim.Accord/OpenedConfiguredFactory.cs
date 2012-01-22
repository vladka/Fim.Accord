using System;
using System.Collections;
using System.Collections.Generic;
using Fim.Accord.EventArgs;
using Fim.Accord.Resolvers;
using Fim.Accord.Scopes;

namespace Fim.Accord
{
    /// <summary>
    /// Factory for opened generic types.
    /// </summary>
    internal class OpenedConfiguredFactory<T> : IConfiguredFactoryInternal<T>
    {
        private readonly Func<BuildingContext, Func<T>> _factory;
        private readonly List<IConfiguredPluginResolverInternal> _allResolvers = new List<IConfiguredPluginResolverInternal>();

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedConfiguredFactory"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        internal OpenedConfiguredFactory(Func<BuildingContext, Func<T>> factory)
        {
            this._factory = factory;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedConfiguredFactory"/> class.
        /// </summary>
        protected OpenedConfiguredFactory()
        { }

        

        /// <summary>
        /// Registers new resolver with this.
        /// </summary>
        /// <param name="resolver"></param>
        public void Add(IConfiguredPluginResolverInternal resolver)
        {
            _allResolvers.Add(resolver);
        }
        /// <summary>
        /// Removes the specified resolver.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>
        /// Retuns remainings resolvers registered with this resolver
        /// </returns>
        public int Remove(IConfiguredPluginResolverInternal resolver)
        {
            _allResolvers.Remove(resolver);
            return _allResolvers.Count;
        }

        object IConfiguredFactoryInternal.Get(BuildingContext ctx)
        {
            return this.Get(ctx);
        }


        /// <summary>
        /// Existing scoped values.
        /// </summary>
        public ScopeTable ScopedValues
        {
            get { return null; }

        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IConfiguredPluginResolver> GetEnumerator()
        {
            return _allResolvers.GetEnumerator();
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
                return _allResolvers.Find(x => x.PluginType == key);
            }
        }

        event EventHandler<AfterCreateEventArgs<T>> IConfiguredFactory<T>.AfterCreate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<BeforeReleaseEventArgs<T>> IConfiguredFactory<T>.BeforeRelease
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        #region releasing component

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

        public event EventHandler<BeforeReleaseEventArgs> BeforeRelease;

        protected virtual void OnBeforeRelease(BeforeReleaseEventArgs args)
        {
            if (BeforeRelease != null)
                BeforeRelease(this, args);
        }
        #endregion

        #region creating component

        public event EventHandler<AfterCreateEventArgs> AfterCreate;

        protected virtual void OnAfterCreate(AfterCreateEventArgs args)
        {
            if (AfterCreate != null)
                AfterCreate(this, args);
        }

        private readonly Dictionary<MultiType, ConfiguredFactory> _myClosedBuilders = new Dictionary<MultiType, ConfiguredFactory>(); //todo: jak rusit pri disposu?

        private struct MultiType
        {
            public readonly string Value;
            public MultiType(Type[] types)
            {
                Value = string.Empty;
                foreach (var i in types)
                {
                    Value += i.AssemblyQualifiedName + ";";
                }    
            }
            public override string ToString()
            {
                return Value;
            }

        }

        
        /// <summary>
        /// CZ: tato metoda probiha pouze poprve pri prvni vyrobe konkretniho generickeho typu
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public T Get(BuildingContext ctx)
        {
            //todo vytvorit instanci a zaregistrovat do scopu
            
            Func<T> finalFunc = /*(Func<object>)*/ _factory(ctx);
            var lz = ctx.CurrentResolver.Scope as BindingContextScope;

            var genericParams = new MultiType(ctx.ResolvingType.GetGenericArguments()); //todo: opravdu je to tak? co generiky v generikach

            ConfiguredFactory group = null;
            if (!_myClosedBuilders.TryGetValue(genericParams,out group))//pokud uz pro dany genericky typ mame skupinu, tak ji pouzijeme
            {
                group = new ConfiguredFactory(c => finalFunc());
                group.AfterCreate += this.AfterCreate;
                group.BeforeRelease += this.BeforeRelease;
                _myClosedBuilders.Add(genericParams, group);
            }

            
            var closedBuilder = new ConfiguredPluginResolver(ctx.Container,ctx.ResolvingType,group,lz!=null ? lz.FinalScope :  ctx.CurrentResolver.Scope,(OpenedGenericPluginResolver)ctx.CurrentResolver);
            ctx.Container.Register(ctx.ResolvingType, closedBuilder,new Tuple<OpenedGenericPluginResolver,ConfiguredPluginResolver> ((OpenedGenericPluginResolver)ctx.CurrentResolver,closedBuilder));

            var result = ctx.Container.GetService(ctx.ResolvingType);
            return (T) result;
            
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

        protected void Dispose(bool disposing)
        {
            if (Disposed)
                return;
            Disposed = true;
            
            foreach (var i in _allResolvers)
                i.Dispose();
            _myClosedBuilders.Clear();
            AfterCreate = null;
            BeforeRelease = null;

        }

        ~OpenedConfiguredFactory()
        {
            Dispose(false);
        }
        #endregion

    }
}