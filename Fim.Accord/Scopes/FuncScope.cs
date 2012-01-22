using System;

namespace Fim.Accord.Scopes
{
    /// <summary>
    /// Scope based on func. Use this class for easy implementation custom scope based on func&lt;T&gt;
    /// </summary>
    public class FuncScope : IScope
    {
        private readonly Func<object> _funcReturningScopeObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncScope"/> class.
        /// </summary>
        /// <param name="funcReturningScopeObject">The func returning scope object.</param>
        public FuncScope(Func<object> funcReturningScopeObject)
        {
            _funcReturningScopeObject = funcReturningScopeObject;
        }

        /// <summary>
        /// Gets the func returning scope object.
        /// </summary>
        public Func<object> FuncReturningScopeObject
        {
            get { return _funcReturningScopeObject; }
        }

        /// <summary>
        /// Gets objects used to determine one scope which is provided by <see cref="FuncReturningScopeObject"/>.
        /// </summary>
        public object Context
        {
            get { return FuncReturningScopeObject(); }
        }
    }
}