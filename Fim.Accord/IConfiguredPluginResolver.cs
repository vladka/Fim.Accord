using System;

namespace Fim.Accord
{

   
    /// <summary>
    /// Configured component being ready to be resolved
    /// </summary>
    public interface IConfiguredPluginResolver : IDisposable
    {
        
        
        /// <summary>
        /// Curenty used scope (Singleton, per Thread, per HttpContext...)
        /// </summary>
        IScope Scope { get; }

        /// <summary>
        /// Type for what is this instance defined
        /// </summary>
        Type PluginType { get; }

       
        
        /// <summary>
        /// CZ: Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// (Porovnej s <see cref="Dispose"/>, která naopak ruší sebe včetně toho, že volá Dispose na všech držených komponentách.)
        /// </summary>
        void UnRegister();


        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every scope-holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        new void Dispose();

       
       
        

    }

    /// <summary>
    ///  Configured component being ready to be resolved. 
    /// </summary>
    /// <typeparam name="TPluginType">Plugin type - ussually interface for what is this factory registered</typeparam>
    /// <typeparam name="TImpType">>Concrete type for what is this factory registered</typeparam>
    public interface IConfiguredPluginResolver< TPluginType,TImpType> : IConfiguredPluginResolver
    {
      
    }

    
}