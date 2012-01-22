using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fim.Accord.Scopes;

namespace Fim.Accord
{
    public partial class Container
    {


        /// <summary>
        /// Třída mající za ukol konfigurovat IoC kontejner
        /// </summary>
        public  class ContainerConfigurationManager
        {
            private readonly Container _owner;

            public ContainerConfigurationManager(Container owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// Volá se pro potřeby konfigurace.
            /// </summary>
            public virtual void Configure()
            {
                SupportForLazy = true;
                SupportForIEnumerable = true;
                SupportForFunc = true;
            }

            #region Properties...

            private bool _supportForLazy;
            /// <summary>
            /// Gets or sets , if container has provide support for automatic wiring and resolving for <see cref="Lazy{T}"/>
            /// </summary>
            public bool SupportForLazy
            {
                get
                {
                    return _supportForLazy;
                }
                set
                {
                    if (value == _supportForLazy)
                        return;
                    if (value)
                        AddSupportForLazy();
                    else
                      _owner.DisposeService(typeof(Lazy<>));
                    _supportForLazy = value;
                    
                }
            }

            private bool _supportForIEnumerable;
            /// <summary>
            /// Gets or sets , if container has provide support for automatic wiring and resolving for <see cref="IEnumerable{T}"/>
            /// </summary>
            public bool SupportForIEnumerable
            {
                get
                {
                    return _supportForIEnumerable;
                }
                set
                {
                    if (value == _supportForIEnumerable)
                        return;
                    if (value)
                        AddSupportForIEnumerable();
                    else
                        _owner.DisposeService(typeof(IEnumerable<>));
                    _supportForIEnumerable = value;

                }
            }


            private bool _supportForFunc;
            /// <summary>
            /// Gets or sets , if container has provide support for automatic wiring and resolving for <see cref="System.Func{T}"/>
            /// </summary>
            public bool SupportForFunc
            {
                get
                {
                    return _supportForFunc;
                }
                set
                {
                    if (value == _supportForFunc)
                        return;
                    if (value)
                        AddSupportForFunc();
                    else
                        _owner.DisposeService(typeof(Func<>));
                    _supportForFunc = value;

                }
            }
            #endregion
            #region Methods..



            protected virtual void AddSupportForLazy()
            {
                
                //_owner.As(typeof (Lazy<>)).Use(typeof (Lazy<>).GetConstructor(new Type[] {typeof(bool)}));...Where(true)
                
                _owner.As(typeof(Lazy<>)).Use(ctx =>
                {
                    var targetType = ctx.ResolvingType.GetGenericArguments()[0];

                    var expr = GetFuncExpressionForResolvingType(targetType);
                    var ci = ctx.ResolvingType.GetConstructor(new Type[] { typeof(Func<>).MakeGenericType(targetType), typeof(bool) });
                    Type fType = Expression.GetFuncType(ctx.ResolvingType);
                    var createdFunc = Expression.Lambda(fType, Expression.New(ci, expr, Expression.Constant(true)));
                    var func = (Func<object>)createdFunc.Compile();
                    return func;

                }, new InnerTypeDependencyScope());
                
            }

            protected virtual void AddSupportForIEnumerable()
            {
                _owner.As(typeof(IEnumerable<>)).Use(ctx =>
                {
                    var targetType = ctx.ResolvingType.GetGenericArguments()[0];

                    var ownerExp = Expression.Constant(_owner);
                    Type fType = Expression.GetFuncType(ctx.ResolvingType);
                    var exp = Expression.Call(ownerExp, "GetServices", new Type[] { targetType });
                    var result = Expression.Lambda(fType, exp);
                    return (Func<object>) result.Compile();

                }, TransientScope.Instance);
            }

             protected virtual void AddSupportForFunc()
            {
                
                
                 //rozsireni pro podporu Func
                _owner.As(typeof(Func<>)).Use(ctx =>
                {
                    var targetType = ctx.ResolvingType.GetGenericArguments()[0];
                    Func<object> res = () => GetFuncExpressionForResolvingType(targetType).Compile();
                    return res; //fce vracící fci

                }, new InnerTypeDependencyScope());
                
            }

            /// <summary>
            /// Vrací výraz pro resolvovací funkcí.
            /// </summary>
            /// <param name="pluginType"></param>
            /// <returns></returns>
            private LambdaExpression GetFuncExpressionForResolvingType(Type pluginType)
            {
                var container = Expression.Constant(_owner);
                Type fType = Expression.GetFuncType(pluginType);
                var exp = Expression.Call(container, "GetService", new Type[] {pluginType});
                var result = Expression.Lambda(fType, exp);
                return result;

            }

            #endregion
        }
    }
}
