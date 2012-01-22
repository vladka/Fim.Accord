namespace Fim.Accord.EventArgs
{
   

    /// <summary>
    /// Args contains information about resolving already used component from scope-cache.
    /// </summary>
    public class AfterReuseEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterReuseEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public AfterReuseEventArgs(object component) 
        {
            Component = component;
        }
        
        /// <summary>
        /// Just resolved  component from scope cache.
        /// This object will be returned to consumer (e.g. by calling <see cref="Container.GetService(System.Type)"/>).
        /// There is possibility to modify object just now.
        /// </summary>
        public  object Component { get; set; }
    }


    /// <summary>
    /// Args contains information about resolving already used component from scope-cache.
    /// </summary>
    public class AfterReuseEventArgs<T> : AfterReuseEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterReuseEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public AfterReuseEventArgs(T component)
            : base(component)
        {

        }

        /// <summary>
        /// Just resolved  component from scope cache.
        /// This object will be returned to consumer (e.g. by calling <see cref="Container.GetService{T}"/>).
        /// There is possibility to modify object just now.
        /// </summary>
        public new T Component
        {
            get { return (T)base.Component; }
            set { base.Component = value; }
        }
    }
}