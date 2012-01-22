using System;

namespace Fim.Accord.EventArgs
{
    /// <summary>
    /// Args for event which occurs when some plugin-type has not been found.
    /// </summary>
    public class AfterMissedComponentEventArgs : System.EventArgs
    {
        private readonly Type _requieredPluginType;

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterMissedComponentEventArgs"/> class.
        /// </summary>
        /// <param name="requieredPluginType">Type of the requiered plugin.</param>
        public AfterMissedComponentEventArgs(Type requieredPluginType)
        {
            _requieredPluginType = requieredPluginType;
        }
        
        /// <summary>
        /// Gets the type of the requiered plugin.
        /// </summary>
        /// <value>
        /// The type of the requiered component.
        /// </value>
        public Type RequieredPluginType
        {
            get { return _requieredPluginType; }
        }
    }

    
}