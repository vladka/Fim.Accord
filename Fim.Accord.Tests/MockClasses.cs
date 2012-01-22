using System;
using System.Threading;

namespace Fim.Accord.Tests
{
    /*This file contains Mock classes 
     * and testing interfaces */
    
    
    public interface ITestDisposable : IDisposable
    {
        /// <summary>
        /// Action called when instance is finalized or disposed
        /// </summary>
        Action BeforeDisposeAction { get; set; }
        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        bool Disposed { get; }
    }

    /// <summary>
    /// Demo interface
    /// </summary>
    public interface IDemoInterface
    {

    }

    /// <summary>
    /// Mock class used for
    /// creating event when is disposed.
    /// </summary>
    public class MockDisposableClass : ITestDisposable, IDemoInterface
    {
        public Action BeforeDisposeAction { get; set; }

        private readonly string name;

        public MockDisposableClass()
        {
            name = Guid.NewGuid().ToString();
        }



        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private bool disposed = false;
        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        public bool Disposed { get { return disposed; } }




        ///<summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// </summary>
        /// <param name="disposing">
        ///  If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Console.WriteLine(" Dispossing object " + this.name);
            else
            {
                Console.WriteLine(" Finalizing object " + this.name);
            }

            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                if (BeforeDisposeAction != null)
                    BeforeDisposeAction();
                BeforeDisposeAction = null;

                disposed = true;

            }
        }

        ~MockDisposableClass()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }


    /// <summary>
    /// Class with ctor taking long time.
    /// </summary>
    public class LongTimeConstructedClass
    {
        public static int Counter = 0;
        public LongTimeConstructedClass()
        {
            Thread.Sleep(1000);
            Interlocked.Increment(ref Counter);
        }
    }
}
