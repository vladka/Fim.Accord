using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Fim.Accord
{
    /// <summary>
    /// Tato instance existuje vzdy jedna pro cely kontejner.
    /// Udrzuje slovnik, kde klicem je WeakReference na scopovaný objekt,
    /// a hodnoty jsou odkazy na hodnoty evidované pro jednotlivé typy pluginů.
    /// </summary>
    public class GlobalScopeTable : IDisposable
    {
        /// <summary>
        /// Defaultni konstruktor.
        /// </summary>
        public GlobalScopeTable()
        {
            _async = new Thread(AsyncCleanup);
            _async.Start();
            
        }

        private readonly Thread _async;
        private bool _disposed;
        private readonly ConcurrentDictionary<WeakReference, List<ScopeTable>> _all = new ConcurrentDictionary<WeakReference, List<ScopeTable>>();

        public bool RemoveInstance(object instanceToRemove)
        {
            var removed = 0;
            foreach (KeyValuePair<WeakReference, List<ScopeTable>> pair in _all)
            {
                
                foreach (ScopeTable scopedValues in pair.Value)
                {
                    if (scopedValues.Remove(instanceToRemove))
                        removed++;
                }
                //toDel.Add(pair.Key);
            }
            if (removed>1) 
                throw new Exception("Not expectet exception. E111");
            return removed == 1;

        }

        

        /// <summary>
        /// spouští asynchronní čístící vlákno.
        /// </summary>
        private void AsyncCleanup()
        {
            
            while (!_disposed)
            {
                Thread.Sleep(1000);
                var toDel = new List<WeakReference>();
                foreach (KeyValuePair<WeakReference, List<ScopeTable>> pair in _all)
                {
                    if (pair.Key.IsAlive)
                        continue;
                    foreach (var scopedValues in pair.Value)
                    {
                        scopedValues.Remove(pair.Key);
                    }
                    toDel.Add(pair.Key);
                }
                foreach (var key in toDel)
                {
                    List<ScopeTable> tmp;
                    _all.TryRemove(key, out tmp);
                    
                }
            }
        }

      
        /// <summary>
        /// Odstrani vsechny instance drzene objektem scopu splnujici <paramref name="match"/>.
        /// </summary>
        /// <param name="match">Optional. If null, every instances from every scopes are removed.</param>
        public void RemoveAll(Predicate<object> match ) 
        {
            

            bool runAgain = true;
            while (runAgain)
            {
                runAgain = false;
                var toDelete = new List<WeakReference>();
                int currentCount = _all.Count;
                foreach (KeyValuePair<WeakReference, List<ScopeTable>> pair in _all)
                {
                    if ((!pair.Key.IsAlive) || (match != null && (!match(pair.Key.Target))))
                        continue;
                    foreach (var scopedValues in pair.Value)
                    {
                        scopedValues.Remove(pair.Key);
                    }
                    toDelete.Add(pair.Key);
                    runAgain = (_all.Count != currentCount);
                    if (runAgain)
                        //enumeration was changed, becasue during disposing some component was some new component created.
                        break;

                }
                List<ScopeTable> tmp;
                toDelete.ForEach(x => _all.TryRemove(x,out tmp));
            }
        }
        
      
        /// <summary>
        /// Přidá novou kolekci pro daný objkt scopu.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void Add(WeakReference key, ScopeTable value)
        {
            List<ScopeTable> refs;
            if (!_all.TryGetValue(key, out refs))
            {
                refs = new List<ScopeTable>();
                _all.TryAdd(key,refs);
                
            }
            refs.Add(value);
        }
        /// <summary>
        /// Odstraní z kolekce objekty pro daný scope.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="scopeTable"></param>
        internal void RemoveFor(WeakReference key, ScopeTable scopeTable)
        {
            List<ScopeTable> refs;
            if (_all.TryGetValue(key, out refs))
            {
                refs.Remove(scopeTable);
                if (refs.Count == 0)
                {
                    List<ScopeTable> tmp ;
                    _all.TryRemove(key,out tmp);//zruseni celeho paru
                }
            }
        }

      
        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get { return _disposed; } }
        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every scope-holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            _async.Join();
            RemoveAll(x => true);
            
            
            
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="GlobalScopeTable"/> is reclaimed by garbage collection.
        /// </summary>
        ~GlobalScopeTable()
        {
            Dispose(false);
        }
        #endregion

        
    }
}