using System;
using System.Web;

namespace Fim.Accord.Scopes
{
    /// <summary>
    /// Lifecycle per web request: There will be one instance per one  <see cref="HttpContext.Current"/>. Singleton.
    /// </summary>
    public class HttpContextScope : IScope
    {

        private static readonly Lazy<HttpContextScope> _instance
            = new Lazy<HttpContextScope>(() => new HttpContextScope());

        // private to prevent direct instantiation.
        private HttpContextScope()
        {
        }

        /// <summary>
        /// Value
        /// </summary>
        public static HttpContextScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        /// <summary>
        /// Gets objects used to determine one scope which is <see cref="HttpContext.Current"/>.
        /// </summary>
        public object Context
        {
            get { return HttpContext.Current; }
        }
    }
}