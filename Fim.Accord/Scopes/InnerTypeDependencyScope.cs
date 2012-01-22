using System.Collections.Generic;

namespace Fim.Accord.Scopes
{

    /// <summary>
    /// Abstract base for scopes based on inner scope.
    /// </summary>
    public abstract class BindingContextScope : IScope
    {
        private BuildingContext _bindingContext;

        /// <summary>
        /// Gets or sets the final scope.
        /// </summary>
        /// <value>
        /// The final scope.
        /// </value>
        public IScope FinalScope { get; set; }

        /// <summary>
        /// Gets or sets the binding context.
        /// </summary>
        /// <value>
        /// The binding context.
        /// </value>
        public BuildingContext BindingContext
        {
            get { return _bindingContext; }
            set
            {
                _bindingContext = value;
                OnSetBindingContext();
            }
        }

        /// <summary>
        /// Called when [set binding context].
        /// </summary>
        protected void OnSetBindingContext()
        {
            Context = UpdateContext();
        }

        /// <summary>
        /// Metoda se volá jakmile je znám  kontext (<see cref="BindingContext"/>).
        /// Metoda vrací značkovací objekt Contextu (<see cref="IScope.Context"/>).
        /// </summary>
        /// <returns></returns>
        protected abstract object UpdateContext();

        /// <summary>
        /// Gets objects used to determine one scope by inner type.
        /// </summary>
        public virtual object Context
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Scope, který závisí na vnitřním typu
    /// </summary>
    public class InnerTypeDependencyScope : BindingContextScope
    {
        
        
        protected override object UpdateContext()
        {
            List<IConfiguredPluginResolverInternal> innerImpls;

            var type = BindingContext.ResolvingType.GetGenericArguments()[0];
            if (!BindingContext.Container.AllResolvers.TryGetValue(type, out innerImpls))
            {
                if (!BindingContext.Container.AllResolvers.TryGetValue(type.GetGenericTypeDefinition(), out innerImpls))
                    return null;
            }
            FinalScope = innerImpls[0].Scope;
            return FinalScope.Context;

        }

    }
}