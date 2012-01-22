using System;
using System.Collections.Generic;
using System.Reflection;
using Fim.Accord.Resolvers;
using Fim.Accord.Scopes;

namespace Fim.Accord
{
    
    public partial class Container
    {


        /// <summary>
        /// Abstract class used for all resolvers.
        /// </summary>
        public abstract class ResolverBase
        {
            protected ResolverBase(Container container)
            {
                Container = container;
            }

            /// <summary>
            /// Returns container, which is owner.
            /// </summary>
            public readonly Container Container;
        }

        /// <summary>
        /// Non-generic resolver
        /// </summary>
        public class Resolver : ResolverBase
        {
            private readonly List<Type> _pluginTypes = new List<Type>();
            private readonly bool IsGenericDefinition;

            public Resolver(Container owner,Type pluginType)
                : base(owner)
            {
                _pluginTypes.Add(pluginType);
                IsGenericDefinition = pluginType.IsGenericTypeDefinition;
            }

            /// <summary>
            /// Returns all types, for what is this registration
            /// </summary>
            public IList<Type> PluginTypes
            {
                get
                {
                    return _pluginTypes.AsReadOnly();//todo: in future should be r/w, not only read
                }
            }

            /// <summary>
            /// Prepares registration previous abstract type (or interface), and this abstract type or interface to one concrete type
            /// </summary>
            /// <param name="anotherPluginType"></param>
            /// <returns></returns>
            public Resolver AndAs(Type anotherPluginType)
            {
                if (anotherPluginType.IsGenericTypeDefinition != IsGenericDefinition)
                    throw new Exception("Both type must be generic definition or both not.");
                _pluginTypes.Add(anotherPluginType);
                return this;
            }


         

            /// <summary>
            /// Uses the specified impl type.
            /// </summary>
            /// <param name="implType">Type of the impl.</param>
            /// <param name="scope">The scope.</param>
            /// <param name="constructorInfo">if NULL is specified, the constructor with most arguments is selected</param>
            /// <returns></returns>
            public IConfiguredFactory Use(Type implType,IScope scope = null)
            {
                if (implType.IsInterface || implType.IsAbstract)
                    throw new ArgumentException("implType could not be interface or abstract type!","implType");
                
                if (!implType.IsGenericTypeDefinition)
                {
                    Func<BuildingContext, object> builder = Container.CreateFunc(implType,null);
                    return Use(builder, scope);
                }
                else
                {
                    Func<BuildingContext, Func<object>> builder = Container.CreateOpenedFunc(implType, null);
                    return UseOpened(builder,scope);
                }
            }

            /// <summary>
            /// Uses the concrete type specified by its constructoInfo
            /// </summary>
            /// <param name="scope">The scope.</param>
            /// <param name="constructorInfo"></param>
            /// <returns></returns>
            public IConfiguredFactory Use(ConstructorInfo constructorInfo, IScope scope = null )
            {
                Type implType = constructorInfo.DeclaringType;
                if (implType.ContainsGenericParameters != this.IsGenericDefinition)
                    throw new ArgumentException("constructorInfo is closed, but expected is opened, or vice versa.", "constructorInfo");

                if (!implType.IsGenericTypeDefinition)
                {
                    Func<BuildingContext, object> builder = Container.CreateFunc(implType, constructorInfo);
                    return Use(builder, scope);
                }
                else
                {
                    Func<BuildingContext, Func<object>> builder = Container.CreateOpenedFunc(implType, constructorInfo);
                    return UseOpened(builder,scope);
                }
            }

            /// <summary>
            /// Uses concrete type specified by generic parameter and  optionally with specified constructor.
            /// </summary>
            /// <typeparam name="TImplType"></typeparam>
            /// <param name="scope"></param>
            /// <param name="constructorInfo"></param>
            /// <returns></returns>
            public IConfiguredFactory<TImplType> Use<TImplType>(IScope scope = null, ConstructorInfo constructorInfo = null)
            {
                
                var builder = Container.CreateFunc<TImplType>(constructorInfo);
                var factory = new ConfiguredFactory<TImplType>(ctx => builder());
                foreach (Type pluginType in _pluginTypes)
                {
                    var impl = new ConfiguredPluginResolver(Container, pluginType, factory,
                                                            scope ?? TransientScope.Instance);
                    Container.Register(pluginType, impl, null);
                }

                return factory;

            }

         
            /// <summary>
            /// Uses specified factory method
            /// </summary>
            /// <typeparam name="TConcreteType"></typeparam>
            /// <param name="builder"></param>
            /// <param name="scope"></param>
            /// <returns></returns>
            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(Func<TConcreteType> builder, IScope scope = null) 
            {

                if (!IsGenericDefinition)
                {
                    var group = new ConfiguredFactory<TConcreteType>(ctx => builder());
                    foreach (Type pluginType in _pluginTypes)
                    {
                        var impl = new ConfiguredPluginResolver(Container, pluginType, group,
                                                                scope ?? TransientScope.Instance);
                        Container.Register(pluginType, impl, null);
                    }
                    return group;
                }
                else
                {
                   return UseOpened(ctx => builder, scope);
                }
            }
            
            /// <summary>
            /// Uses specified factory method
            /// </summary>
            /// <typeparam name="TConcreteType"></typeparam>
            /// <param name="factory"></param>
            /// <param name="scope"></param>
            /// <returns></returns>
            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(Func<BuildingContext, TConcreteType> factory, IScope scope = null)
            {
                  
                  if (!IsGenericDefinition)
                  {
                      var group = new ConfiguredFactory<TConcreteType>(factory);
                      foreach (Type pluginType in _pluginTypes)
                      {
                          IConfiguredPluginResolverInternal impl = new ConfiguredPluginResolver(Container, pluginType,
                                                                                                group,
                                                                                                scope ??
                                                                                                TransientScope.Instance);
                          Container.Register(pluginType, impl, null);
                      }
                      return group;
                  }
                throw new NotSupportedException();
                
            }
            /// <summary>
            /// Use this for open generic registration only.
            /// </summary>
            /// <typeparam name="TConcreteType"></typeparam>
            /// <param name="factory"></param>
            /// <param name="scope"></param>
            /// <returns></returns>
            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(Func<BuildingContext, Func<TConcreteType>> factory, IScope scope = null)
            {
                return UseOpened(factory, scope);
            }
            
            
            private IConfiguredFactory<TConcreteType> UseOpened<TConcreteType>(Func<BuildingContext, Func<TConcreteType>> factory, IScope scope = null)
            {
                
              
                IConfiguredFactoryInternal<TConcreteType> group;
                if (IsGenericDefinition)
                {
                    group = new OpenedConfiguredFactory<TConcreteType>(factory);
                    foreach (Type pluginType in _pluginTypes)
                    {
                        IConfiguredPluginResolverInternal impl = new OpenedGenericPluginResolver(Container, pluginType, group, scope ?? TransientScope.Instance);
                        Container.Register(pluginType, impl, null);
                    }
                    return group;

                }
                throw new NotSupportedException("Use only for opened type (e.g. List<>");
              
            }

           
        }

        /// <summary>
        /// Basic resolver, used when components is required just for one plugin
        /// </summary>
        /// <typeparam name="TPluginType"></typeparam>
        public class Resolver<TPluginType> : ResolverBase
        {

            public Resolver(Container owner)
                : base(owner)
            {
            }

            public Resolver<TAnotherTPluginType,TPluginType> AndAs<TAnotherTPluginType>() 
            {
                return new Resolver<TAnotherTPluginType,TPluginType>(Container);
            }

         
         

            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(IScope scope = null) where TConcreteType : TPluginType
            {
                Func<TConcreteType> builder = Container.CreateFunc<TConcreteType>();
                var group = new ConfiguredFactory<TConcreteType>(ctx => builder());
                var impl = new ConfiguredResolver<TPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                Container.Register(typeof(TPluginType), impl, null);
                return group;
            }

            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(Func<TConcreteType> builder, IScope scope = null) where TConcreteType : TPluginType
            {
                var group = new ConfiguredFactory<TConcreteType>(ctx => builder());
                var impl1 = new ConfiguredResolver<TPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1);
                return group;
            }
            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(Func<BuildingContext, TConcreteType> builder, IScope scope = null) where TConcreteType : TPluginType
            {
                var group = new ConfiguredFactory<TConcreteType>(builder);
                var impl1 = new ConfiguredResolver<TPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1);
                return group;
            }

        }

    
        /// <summary>
        /// Resolver factory based on 2 interfaces
        /// </summary>
        /// <typeparam name="TPluginType"></typeparam>
        /// <typeparam name="TPreviousPluginType"></typeparam>
        public class Resolver<TPluginType, TPreviousPluginType> : ResolverBase
        {


            public Resolver(Container owner)
                : base(owner)
            {
            }


            public Resolver<TAnotherTPluginType, TPluginType, TPreviousPluginType> AndAs<TAnotherTPluginType>()
            {
                return new Resolver<TAnotherTPluginType, TPluginType, TPreviousPluginType>(Container);
            }

            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(IScope scope = null) where TConcreteType : TPluginType, TPreviousPluginType
            {
                var builder = Container.CreateFunc<TConcreteType>();
                var group = new ConfiguredFactory<TConcreteType>(ctx => builder());
                var impl1 = new ConfiguredResolver<TPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                
                this.Container.Register(typeof(TPluginType), impl1);
                this.Container.Register(typeof(TPreviousPluginType), impl2);


                return group;
            }

            public IConfiguredFactory<TCommonType> Use<TCommonType>(Func<TCommonType> builder, IScope scope = null) where TCommonType : TPluginType, TPreviousPluginType
            {
                var group = new ConfiguredFactory<TCommonType>(ctx => builder());
                var impl1 = new ConfiguredResolver<TPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1 );
                this.Container.Register(typeof(TPreviousPluginType), impl2);

                return group;

            }
            public IConfiguredFactory<TCommonType> Use<TCommonType>(Func<BuildingContext, TCommonType> builder, IScope scope = null) where TCommonType : TPluginType, TPreviousPluginType
            {
                var group = new ConfiguredFactory<TCommonType>(builder);
                var impl1 = new ConfiguredResolver<TPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                
                this.Container.Register(typeof(TPluginType), impl1);
                this.Container.Register(typeof(TPreviousPluginType), impl2);
                
                return group;
            }



        }

        /// <summary>
        /// Resolver factory based on 3 interfaces
        /// </summary>
        public class Resolver<TPluginType, TPreviousPluginType, TPreviousPluginType2> : ResolverBase
        {


            public Resolver(Container owner)
                : base(owner)
            {
             
            }

            public IConfiguredFactory<TConcreteType> Use<TConcreteType>(IScope scope = null) where TConcreteType : TPluginType, TPreviousPluginType, TPreviousPluginType2
            {
                var builder = Container.CreateFunc<TConcreteType>();
                var group = new ConfiguredFactory<TConcreteType>(ctx => builder());
                var impl1 = new ConfiguredResolver<TPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                var impl3 = new ConfiguredResolver<TPreviousPluginType2, TConcreteType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1);
                this.Container.Register(typeof(TPreviousPluginType), impl2);
                this.Container.Register(typeof(TPreviousPluginType2), impl3);
                return group;
            }

            public IConfiguredFactory<TCommonType> Use<TCommonType>(Func<TCommonType> builder, IScope scope = null) where TCommonType : TPluginType, TPreviousPluginType, TPreviousPluginType2
            {
                var group = new ConfiguredFactory<TCommonType>(ctx => builder());
                var impl1 = new ConfiguredResolver<TPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl3 = new ConfiguredResolver<TPreviousPluginType2, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1 );
                this.Container.Register(typeof(TPreviousPluginType), impl2);
                this.Container.Register(typeof(TPreviousPluginType2), impl3);
                return group;
            }
            public IConfiguredFactory<TCommonType> Use<TCommonType>(Func<BuildingContext, TCommonType> builder, IScope scope = null) where TCommonType : TPluginType, TPreviousPluginType, TPreviousPluginType2
            {
                var group = new ConfiguredFactory<TCommonType>(builder);
                var impl1 = new ConfiguredResolver<TPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl2 = new ConfiguredResolver<TPreviousPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                var impl3 = new ConfiguredResolver<TPreviousPluginType, TCommonType>(Container, group, scope ?? TransientScope.Instance);
                this.Container.Register(typeof(TPluginType), impl1 );
                this.Container.Register(typeof(TPreviousPluginType), impl2);
                this.Container.Register(typeof(TPreviousPluginType2), impl3);

                return group;
            }


          
        }

       
    }
}
