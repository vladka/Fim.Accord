using System;
using System.Collections.Generic;

namespace Fim.Accord
{
    /// <summary>
    /// CZ: Kolekce hodnot pro jednotlivé scopy patrici jedne skupine instanci <see cref="IConfiguredFactoryInternal"/>
    /// </summary>
    public class ScopeTable  : IDisposable
    {
        
        private readonly Dictionary<WeakReference,object> _all = new Dictionary<WeakReference, object>();
        private readonly GlobalScopeTable _globalScopeTable;
        private readonly IConfiguredFactoryInternal _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeTable"/> class.
        /// </summary>
        internal ScopeTable(GlobalScopeTable globalScopeTable, IConfiguredFactoryInternal owner)
        {
            // TODO: Complete member initialization
            this._globalScopeTable = globalScopeTable;
            this._owner = owner;
        }

        /// <summary>
        /// Gets the owner.
        /// </summary>
        internal IConfiguredFactoryInternal Owner
        {
            get
            {
                return _owner;
            }
        }

        /// <summary>
        /// Returns instance of object holded by scope determined by scope context object.
        /// </summary>
        /// <param name="scopeValue">scope's object (value of <see cref="IScope.Context"/>)</param>
        /// <returns></returns>
        public object FindValueByScope(object scopeValue)
        {
            //todo: musí to být cyklus?
            foreach (KeyValuePair<WeakReference, object> keyValuePair in _all)
            {
                if (keyValuePair.Key.IsAlive && keyValuePair.Key.Target == scopeValue)
                    return keyValuePair.Value;
            }
            return null;
        }

        ///// <summary>
        ///// Returns instances of weakreference (with scope) holding given instance.
        ///// </summary>
        ///// <param name="instance"></param>
        ///// <returns></returns>
        //public IList<WeakReference> FindScopesByValue(object instance)
        //{
        //    List<WeakReference> res = new List<WeakReference>();
        //    foreach (KeyValuePair<WeakReference, object> keyValuePair in _all)
        //    {
        //        if (object.ReferenceEquals(instance, keyValuePair.Value))
        //        res.Add(keyValuePair.Key);
        //    }
        //    return res;
        //}

        /// <summary>
        /// Removes instance from evidence and calls default 'ReleaseComponent' behaviour.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool Remove(object instance)
        {
            WeakReference res = null;
            foreach (KeyValuePair<WeakReference, object> keyValuePair in _all)
            {
                if (object.ReferenceEquals(instance, keyValuePair.Value))
                {
                    res = keyValuePair.Key;
                    break;
                }
            }
            if (res != null)
            {
                Remove(res);
                return true;
            }
            return false;

        }

        /// <summary>
        /// Zaregistruje instanci k prislusnemu scopu. 
        /// </summary>
        /// <param name="scopeValue"></param>
        /// <param name="instance "></param>
        public void RegisterScopedObject(object scopeValue,object instance)
        {
            var a = new WeakReference(scopeValue);
            _all.Add(a, instance);
            _globalScopeTable.Add(a,this);
        }

        /// <summary>
        /// CZ: Odstrani konkretni instanci z evidence pro tento typ scopu. Zavolani dispose, pokud je objekt typu <see cref="IDisposable"/>.
        /// Vola se jakmile konci nejaky scope.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(WeakReference key)
        {
            object value;
            if (_all.TryGetValue(key, out  value))
            {
                _all.Remove(key);
                _owner.ReleaseComponent(value);
                
            }
        }


         

      
        /// <summary>
        /// Removes all components for all scopes.
        /// </summary>
        public void RemoveAll()
        {
            foreach (KeyValuePair<WeakReference, object> pair in _all)
            {
                
                this._globalScopeTable.RemoveFor(pair.Key,this);
                _owner.ReleaseComponent(pair.Value);
            }
        }

        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get; private set; }
        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;
            
            RemoveAll();
            Disposed = true;
        }

        ~ScopeTable()
        {
            Dispose(false);
        }
        #endregion
    }
}