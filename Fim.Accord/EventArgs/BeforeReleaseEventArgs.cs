using System;

namespace Fim.Accord.EventArgs
{
    /// <summary>
    /// Args contains information about component which will be released from container.
    /// </summary>
    public class BeforeReleaseEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeReleaseEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public BeforeReleaseEventArgs(object component)
        {
            Component = component;
            RunDispose = true;
        }
      
        /// <summary>
        /// Component which will be released.
        /// </summary>
        public object Component { get; private set; }

        /// <summary>
        /// If should be called Dispose() on releasing component, just only for <see cref="IDisposable"/>
        /// </summary>
        public bool RunDispose { get; set; }
    }

    /// <summary>
    /// Args contains information about component which will be released from container.
    /// </summary>
    public class BeforeReleaseEventArgs<T> : BeforeReleaseEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeReleaseEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public BeforeReleaseEventArgs(T component) : base(component)
        {
            
            RunDispose = true;
        }

        /// <summary>
        /// Component which will be released.
        /// </summary>
        public new T Component { get { return (T) base.Component; } }

       
    }
}