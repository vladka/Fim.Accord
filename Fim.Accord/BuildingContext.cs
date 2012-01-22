using System;

namespace Fim.Accord
{
    /// <summary>
    /// Is created during reolving process.
    /// </summary>
    public class BuildingContext
    {
        /// <summary>
        /// Type being now constructed.
        /// </summary>
        public readonly Type ResolvingType;



        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingContext"/> class.
        /// </summary>
        /// <param name="resolvingType">Type of the resolving.</param>
        /// <param name="container">The container.</param>
        public BuildingContext(Type resolvingType, Container container)
        {
            ResolvingType = resolvingType;
            Container = container;
        }


        /// <summary>
        /// Resolver being resolving instance now.
        /// </summary>
        public IConfiguredPluginResolver  CurrentResolver 
        {
            get;internal set;
        }

        /// <summary>
        /// Current container.
        /// </summary>
        public Container Container 
        {
            get;
            private  set;
        }
    }
}