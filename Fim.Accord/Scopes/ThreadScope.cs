using System;
using System.Threading;

namespace Fim.Accord.Scopes
{
    /// <summary>
    /// Class providing life cycles per thread, 
    /// if you have 2 threads, ther will be only 2 instances in your application. Singleton.
    /// (Use this class is singleton (<see cref="Instance"/>))
    /// </summary>
    public class ThreadScope : IScope
    {

        private static readonly Lazy<ThreadScope> _instance
            = new Lazy<ThreadScope>(() => new ThreadScope());

        // private to prevent direct instantiation.
        private ThreadScope()
        {
        }

        /// <summary>
        ///  accessor for instance (singleton)
        /// </summary>
        public static ThreadScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        /// <summary>
        /// <see cref="IScope.Context"/>
        /// </summary>
        public object Context
        {
            get { return Thread.CurrentThread; }
        }
    }
}