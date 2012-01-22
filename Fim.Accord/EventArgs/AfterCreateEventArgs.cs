namespace Fim.Accord.EventArgs
{
    
    
    /// <summary>
    /// Args contains information about just now constructed (or resolved by any Func&lt;&gt;) component.
    /// </summary>
    public class AfterCreateEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterCreateEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public AfterCreateEventArgs(object component) 
        {
            Component = component;
        }
        /// <summary>
        /// Just now constructed (or resolved by any Func&lt;&gt;) component.
        /// This object will be returned to consumer (e.g. by calling <see cref="Container.GetService(System.Type)"/>).
        /// There is possibility to modify object just now.
        /// </summary>
        public  object Component { get; set; }
    }


    /// <summary>
    /// Args contains information about just now constructed (or resolved by any Func&lt;&gt;) component.
    /// </summary>
    public class AfterCreateEventArgs<T> : AfterCreateEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterCreateEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="component">The component.</param>
        public AfterCreateEventArgs(T component) : base(component)
        {
            
        }
        /// <summary>
        /// Just now constructed (or resolved by any Func&lt;&gt;) component.
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