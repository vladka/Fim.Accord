using System.Threading;

namespace Fim.Accord
{
    /// <summary>
    /// Interface for all scopes.
    /// </summary>
    public interface IScope  
    {
        /// <summary>
        /// Gets objects used to determine one scope, e.g. <see cref="Thread.CurrentThread"/>, or HttpContext.Current ...
        ///  </summary>
        /// <remarks>
        /// </remarks>
        object Context { get;}
    }
}