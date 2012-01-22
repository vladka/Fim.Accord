using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Fim.Accord.EventArgs;
using Fim.Accord.Resolvers;

namespace Fim.Accord
{
    /// <summary>
    /// DI container.
    /// </summary>
    public partial class Container : IScope, IDisposable
    {
        #region Fields....
        /// <summary>
        /// All registered resolvers
        /// </summary>
        internal readonly ConcurrentDictionary<Type, List<IConfiguredPluginResolverInternal>> AllResolvers = new ConcurrentDictionary<Type, List<IConfiguredPluginResolverInternal>>();

        /// <summary>
        /// Global scoped values. See <see cref="GlobalScopeTable"/>.
        /// </summary>
        internal GlobalScopeTable GlobalScopeTable = new GlobalScopeTable();

        /// <summary>
        /// Version of data, every registration or remove increments this number
        /// </summary>
        private long _serial = 0;

        #endregion

        #region Constructors...
        /// <summary>
        /// Creates new DI container. If <paramref name="configurationManager"/> is not specified, the default one is used.
        /// </summary>
        /// <param name="configurationManager"></param>
        public Container(ContainerConfigurationManager configurationManager = null)
        {
            ConfigurationManager = configurationManager ?? new ContainerConfigurationManager(this);

            ConfigurationManager.Configure();

            _serial = 0; //we set it to zero after configurationManager running.
        }
        #endregion
        
        #region Properties...
        /// <summary>
        /// Gets current configuration manager used for this container.
        /// </summary>
        /// <remarks>
        /// (This mannager is set when you create <see cref="Container"/>, see constructor)
        /// </remarks>
        public readonly ContainerConfigurationManager ConfigurationManager;

        /// <summary>
        /// Events is occured when 'GetService' method returns NULL.
        /// If this event is occured, you can register missing component/plugin, because
        /// container will try resolve component again.
        /// </summary>
        public event EventHandler<AfterMissedComponentEventArgs> AfterMissedComponent;

        /// <summary>
        /// Version of data, every registration or remove components increments this number.
        /// If this number is changed, something has been changed.
        /// </summary>
        public long Serial
        {
            get
            {
                return _serial;
            }
        }
        #endregion

        #region 'Query' methods
        /// <summary>
        /// Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public bool IsConfiguredFor(Type pluginType)
        {
            return AllResolvers.ContainsKey(pluginType);//todo: co genericke otevrene definice
        }

        /// <summary>
        ///  Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public bool IsConfiguredFor<T>()
        {
            return AllResolvers.ContainsKey(typeof(T));//todo: co genericke otevrene definice
        }
        #endregion
        
        #region GetService(s) Methods

        /// <summary>
        /// Returns default service determined by their pluginType
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        /// <summary>
        /// Get service by plugin-info.
        /// </summary>
        /// <param name="configured"></param>
        /// <returns></returns>
        public object GetService(IConfiguredPluginResolver configured)
        {

            var ob = configured as OpenedGenericPluginResolver;
            if (ob != null)
                throw new ArgumentException("Resolving service from opened generic definition is not possible.");
            var ctx = new BuildingContext(configured.PluginType, this);
            var result = ((IConfiguredPluginResolverInternal)configured).Get(ctx);
            return result;


        }

        

        /// <summary>
        /// Returns default service determined by their pluginType
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public object GetService(Type pluginType)
        {
            return GetService(pluginType, true);
        }

        private object GetService(Type pluginType, bool raiseEvent)
        {

            List<IConfiguredPluginResolverInternal> impls;
            if (!AllResolvers.TryGetValue(pluginType, out impls))
            {
                //resolving generic by opened generic deftype 
                if (pluginType.IsGenericType)
                {
                    var gd = pluginType.GetGenericTypeDefinition();
                    if (!AllResolvers.TryGetValue(gd, out impls))
                    {
                        if (raiseEvent)
                        {
                            var args = new AfterMissedComponentEventArgs(pluginType);
                            if (AfterMissedComponent != null)
                            {
                                AfterMissedComponent(this, args);
                                return GetService(pluginType, false);
                            }
                        }
                        return null;
                    }
                }
                else
                {
                    if (raiseEvent)
                    {
                        var args = new AfterMissedComponentEventArgs(pluginType);
                        if (AfterMissedComponent != null)
                        {
                            AfterMissedComponent(this, args);
                            return GetService(pluginType, false);
                        }
                    }
                    return null;
                }
            }
            var ctx = new BuildingContext(pluginType, this);
            var result = impls[0].Get(ctx); //first is default
            return result;
        }



        /// <summary>
        /// Returns all services determined by their pluginType
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetServices<T>()
        {
            return GetServices(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Returns all services determined by their pluginType
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public IEnumerable<object> GetServices(Type pluginType)
        {
            BuildingContext ctx = null;
            List<IConfiguredPluginResolverInternal> impls;
            List<OpenedGenericPluginResolver> implsToSkip = null;
            if (AllResolvers.TryGetValue(pluginType, out impls))
            {
                ctx = new BuildingContext(pluginType, this);
                //protoze zavolani 'i.Get(ctx)' muze zpusobit pridani dalsich definic do kolekce impls, 
                //pouzivame 'for' a vzdy znovuvyhodnocujeme celkovy pocet
                // ReSharper disable ForCanBeConvertedToForeach
                for (int index = 0; index < impls.Count; index++)
                {
                    var i = impls[index];
                    var rd = i as RedirectionToOpenedGenericPluginResolver;

                    //protoze budeme prochazet i otevrene definice, tak je nebudeme volat znovu, pokud jiz byly redirectorovány.
                    if (rd != null)
                        (implsToSkip ?? (implsToSkip = new List<OpenedGenericPluginResolver>())).Add(rd.Target);
                    else
                    {
                        //protoze budeme prochazet i otevrene definice, tak je nebudeme volat znovu, pokud tato definice vychazi z otevrene definice
                        var ib = i as ConfiguredPluginResolver;
                        if (ib != null && ib.Creator != null)
                            (implsToSkip ?? (implsToSkip = new List<OpenedGenericPluginResolver>())).Add(ib.Creator);
                    }
                    yield return i.Get(ctx);
                }
                // ReSharper restore ForCanBeConvertedToForeach
            }

            //resolving generic by opened generic deftype 
            if (pluginType.IsGenericType)
            {
                var gd = pluginType.GetGenericTypeDefinition();

                if (AllResolvers.TryGetValue(gd, out impls))
                {
                    if (ctx == null)
                        ctx = new BuildingContext(pluginType, this);

                    //protoze zavolani 'i.Get(ctx)' muze zpusobit pridani dalsich definic do kolekce impls, 
                    //pouzivame 'for' a vzdy znovuvyhodnocujeme celkovy pocet
                    // ReSharper disable ForCanBeConvertedToForeach
                    for (int index = 0; index < impls.Count; index++)
                    {
                        var i = impls[index];
                        if (implsToSkip != null && implsToSkip.Contains(i))
                            continue;
                        yield return i.Get(ctx);
                    }
                    // ReSharper restore ForCanBeConvertedToForeach
                }
            }
            yield break;

        }
        #endregion

        #region RegisterType Methods..

        /// <summary>
        /// Asociates plugin. As.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Resolver As(Type type)
        {
            var j = new Resolver(this, type);
            return j;
        }

        /// <summary>
        /// Asociates plugin. As.
        /// </summary>
        /// <typeparam name="TPluginType"></typeparam>
        /// <returns></returns>
        public Resolver<TPluginType> As<TPluginType>()
        {
            var j = new Resolver<TPluginType>(this);
            return j;
        }
       
        #endregion

        #region Eject & Remove & Dispose Methods

        /// <summary>
        /// Ejects all servises registered for this scope.
        /// </summary>
        /// <param name="scope"></param>
        public void EjectServices(IScope scope)
        {
            var scopeObject = scope.Context;
            if (scopeObject != null)
            {
                GlobalScopeTable.RemoveAll(x => x.Equals(scopeObject));

            }
        }

        public void EjectInstance(object instanceToRemove)
        {
            GlobalScopeTable.RemoveInstance(instanceToRemove);
        }

        /// <summary>
        /// Unregister all components by given pluginType.
        /// Every IDisposable component is disposed. 
        /// (<seealso cref="UnRegister"/> which keep components alive).
        /// If pluginType is set by opened generic definition (e.g. typeof(IList&lt;&gt;), this this plugin is kept.
        /// </summary>
        /// <param name="pluginType"></param>
        public void DisposeService(Type pluginType)
        {
            List<IConfiguredPluginResolverInternal> implementators;
            if (AllResolvers.TryGetValue(pluginType, out implementators))
            {
                var toDispose = (from i in implementators
                                 let ib = i as ConfiguredPluginResolver
                                 where ib == null || ib.Creator == null
                                 select i).ToList();
                //to avoid Ienumerable modified exception
                List<IConfiguredFactoryInternal> groups = new List<IConfiguredFactoryInternal>();
                foreach (IConfiguredPluginResolverInternal implementationBuilder in toDispose)
                {
                    if (!groups.Contains(implementationBuilder.Factory))
                        groups.Add(implementationBuilder.Factory);
                }
                groups.ForEach(x => x.Dispose());


            }

        }


        /// <summary>
        /// Tries to remove definition, all components keeps alive.
        /// CZ: Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech, jinak by doslo k zacykleni.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// Pokud builder je typu <see cref="OpenedGenericPluginResolver"/>, pak jsou odregistrovani i vsichni uzavrene genericke definice vychazejici z tohoto buildru.
        /// </summary>
        /// <param name="builder"></param>
        internal void UnRegister(IConfiguredPluginResolverInternal builder)
        {
            List<IConfiguredPluginResolverInternal> implementators;
            if (!this.AllResolvers.TryGetValue(builder.PluginType, out implementators))
                return;
            var ob = builder as OpenedGenericPluginResolver;
            if (ob != null)
            {
                //odregistrovani i konkretnich generickych potomku vychazejicich z otevrene genericke definice
                List<IConfiguredPluginResolverInternal> toRemoveList = (from i in implementators
                                                                        let ib = i as ConfiguredPluginResolver
                                                                        where ib != null && ib.Creator == builder
                                                                        select i).ToList();
                toRemoveList.Add(builder); //odregistrovani sama sebe
                toRemoveList.ForEach(x => implementators.Remove(x));
                Interlocked.Increment(ref _serial);
                return;
            }

            //=> it is ConfiguredPluginResolver, it is only once
            var toRemove = implementators.Find(x => builder == x);
            if (toRemove == null)
                return;
            implementators.Remove(toRemove);


            if (implementators.Count == 0)
            {
                List<IConfiguredPluginResolverInternal> tmp;
                if (this.AllResolvers.TryRemove(builder.PluginType, out tmp))
                    Interlocked.Increment(ref _serial);
            }


        }


        #endregion
        
        #region Register methods...

        internal IConfiguredPluginResolverInternal Register(Type interfaceType, IConfiguredPluginResolverInternal builder, Tuple<OpenedGenericPluginResolver, ConfiguredPluginResolver> callerToReplace = null)
        {
            List<IConfiguredPluginResolverInternal> implementators;
            while (true)
            {
                if (!AllResolvers.TryGetValue(interfaceType, out implementators))
                {
                    implementators = new List<IConfiguredPluginResolverInternal>();
                    if (!AllResolvers.TryAdd(interfaceType, implementators))
                        continue; //try again, parallel access

                    Interlocked.Increment(ref _serial);

                    if (callerToReplace == null && interfaceType.IsGenericType &&
                        (!interfaceType.IsGenericTypeDefinition))
                    {
                        //pokud definujeme genericky typ, ale uz je definovan predpis pro otevreny genericky typ, 
                        //tak tento otevreny musi zustat jako defaultni
                        //Tuto vetev vsak nevolame, pokud jde o zakladani volane otevrene definice (callerToReplace==null)
                        List<IConfiguredPluginResolverInternal> openedImplementators;
                        if (AllResolvers.TryGetValue(interfaceType.GetGenericTypeDefinition(), out openedImplementators))
                        {
                            implementators.Add(
                                new RedirectionToOpenedGenericPluginResolver(interfaceType,
                                                                  (OpenedGenericPluginResolver)openedImplementators[0]));
                        }
                    }
                }
                else
                {
                    //jakmile otevřená definice má svoji konkrétní implementaci, nahradíme puvodni 'redirector'
                    if (callerToReplace != null)
                    {
                        int indexToReplace = implementators.FindIndex(x =>
                                                                          {
                                                                              var openedRedirector =
                                                                                  x as RedirectionToOpenedGenericPluginResolver;
                                                                              return (openedRedirector != null &&
                                                                                      openedRedirector.Target ==
                                                                                      callerToReplace.Item1);

                                                                          }
                            );
                        if (indexToReplace >= 0)
                            implementators[indexToReplace] = callerToReplace.Item2;
                        return null; //neni potreba nic vracet pokud callerToReplace!=null
                    }
                }
                break;
            }

            implementators.Add(builder);//jinak ji pridame na konec
            return builder;
        }
        #endregion
        
        #region Helping stubs..
        /// <summary>
        /// Pomocná metoda, která má za ukol obalit puvodni funkci, tak aby zavisela na kontextu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerFunc"></param>
        /// <returns></returns>
        private Func<BuildingContext, T> CreateFunc<T>(Func<T> innerFunc)
        {
            return ctx => innerFunc(); //carrying 

        }

        /// <summary>
        /// Creates a Func for using with ctor.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ctor"></param>
        /// <returns></returns>
        private Func<BuildingContext, object> CreateFunc(Type type,ConstructorInfo ctor)
        {
            if (!type.IsGenericTypeDefinition)
            {
                //vytvoreni funkce na zaklade typu
                  return CreateFunc(GetBuildUpFunc<object>(type,ctor)); 
            }
            else
            {
                //TODO zrusit tutp vetev
                Func<BuildingContext, object> f = delegate(BuildingContext ctx)
                {
                    var genType = type.MakeGenericType(ctx.ResolvingType.GetGenericArguments());
                    if (ctor!=null && ctor.ContainsGenericParameters)
                        ctor = MakeGenericCtor(ctor, genType);
                    Func<object> createFunc = GetBuildUpFunc<object>(genType,ctor);
                    return createFunc;

                };
                return f;
            }

        }
        private Func<BuildingContext, Func<object>> CreateOpenedFunc(Type type, ConstructorInfo ctor)
        {
            if (!type.IsGenericTypeDefinition)
                throw new NotSupportedException();

            Func<BuildingContext, Func<object>> f = delegate(BuildingContext ctx)
                                                  {
                                                      var genType =
                                                          type.MakeGenericType(ctx.ResolvingType.GetGenericArguments());
                                                      if (ctor != null && ctor.ContainsGenericParameters)
                                                          ctor = MakeGenericCtor(ctor, genType);
                                                      Func<object> createFunc = GetBuildUpFunc<object>(genType, ctor);
                                                      return createFunc;

                                                  };
            return f;


        }


        private ConstructorInfo MakeGenericCtor(ConstructorInfo constructorInfo,Type genType)
        {

            var parameters =constructorInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var ctor = genType.GetConstructor(parameters);
            return ctor;
        }

        internal Func<TReal> CreateFunc<TReal>(ConstructorInfo ctor=null)
        {
            return this.GetBuildUpFunc<TReal>(typeof(TReal),ctor);
        }


        private ConstructorInfo SelectCtorWithMostArguments(Type typeToBeConstructed)
        {
            var ctors = typeToBeConstructed.GetConstructors();
            var selectedCtor = ctors.OrderByDescending(x => x.GetParameters().Length).FirstOrDefault(); //todo: what if there is any ctor !!?     
            return selectedCtor;
        }

        /// <summary>
        ///  Returns function, which builds instance of type (<paramref name="typeToBeConstructed"/>) 
        /// using constructor specified by <paramref name="constructor"/>.
        ///  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeToBeConstructed"></param>
        /// <param name="constructor"></param>
        /// <returns></returns>
        private Func<T> GetBuildUpFunc<T>(Type typeToBeConstructed, ConstructorInfo constructor)
        {
            var eThis = Expression.Constant(this);
            if (constructor==null)
                constructor = SelectCtorWithMostArguments(typeToBeConstructed);
            IEnumerable<Expression> exprs =
                constructor.GetParameters().Select(
                    x => Expression.Call(eThis, "GetService", new Type[] {x.ParameterType}));
            var createFunc = Expression.Lambda<Func<T>>(System.Linq.Expressions.Expression.New(constructor, exprs));
            return createFunc.Compile();
        }

        #endregion
        
        /// <summary>
        /// Container itself can be used as scope, where scope exists so long as this container.
        /// </summary>
        object IScope.Context
        {
            get { return this; }
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
            GlobalScopeTable.Dispose();

        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Container"/> is reclaimed by garbage collection.
        /// </summary>
        ~Container()
        {
            Dispose(false);
        }
        #endregion
    }
}
