using System.Collections.Generic;
using System.Threading.Tasks;
using Fim.Accord.Scopes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Fim.Accord.Tests
{
    /*
     * All tests of Accord features
     * 
     */ 

    
    /// <summary>
    /// Accord tests.
    /// Tests all featured of Accord container. (<see cref="Container"/>) 
    ///</summary>
    [TestClass()]
    public class AccordContainerTest
    {


        /// <summary>
        /// Basic usage. "As.AndAs" clasuse must return some instance
        /// </summary>
        [TestMethod()]
        public void AsAndAsUseTest()
        {
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().AndAs<IDemoInterface>().Use<MockDisposableClass>(ThreadScope.Instance);
                var instance1 = ioc.GetService<ITestDisposable>();
                var someInstance = ioc.GetService<IDemoInterface>();
                
                Assert.AreSame(instance1, someInstance);

            }
        }

        /// <summary>
        /// Test, if "AfterCreate" event occured.
        /// </summary>
        [TestMethod()]
        public void SimpleEventTest()
        {
            using (var ioc = new Container())
            {
                IConfiguredFactory<MockDisposableClass> configuredFactory
                    = ioc.As<ITestDisposable>().Use<MockDisposableClass>(ThreadScope.Instance);

                var created = false;
                configuredFactory.AfterCreate += (sender, args) =>
                                                     {
                                                         Console.Write("Instance created!");
                                                         created = true;
                                                     };
                
                var instance1 = ioc.GetService<ITestDisposable>();
                Assert.IsTrue(created);

            }

        }

        /// <summary>
        ///Test of method <see cref="IDisposable.Dispose"/> in the <see cref="IConfiguredFactory{T}"/>.
        /// This tests, if registration is disposed, all nested components must be released too.
        ///</summary>
        [TestMethod()]
        public void DisposeRegistration()
        {
            var disposed = false;
            var created = false;
            using (var ioc = new Container())
             {
                 IConfiguredFactory<MockDisposableClass> registration = ioc.As<ITestDisposable>().Use<MockDisposableClass>(ioc);
                 registration.AfterCreate += (sender, args) =>
                                                       {
                                                           
                                                           args.Component.BeforeDisposeAction =() => disposed = true;
                                                           //((AfterCreateEventArgs)args).Component = new MockDisposableClass2();
                                                       };
                 var myComponent = ioc.GetService<ITestDisposable>();
                 registration.Dispose();

                 Assert.IsTrue(disposed);
                 Assert.IsFalse(ioc.IsConfiguredFor(typeof(ITestDisposable)));
             }
        }

        /// <summary>
        ///Test of event AfterMissedComponent (<see cref="Container.AfterMissedComponent"/>)
        ///</summary>
        [TestMethod()]
        public void AfterMissedComponentTest()
        {
           
            using (var ioc = new Container())
            {
                ioc.AfterMissedComponent += (sender, args) =>
                                                {
                                                    if (args.RequieredPluginType == typeof(ITestDisposable))
                                                        ioc.As<ITestDisposable>().Use<MockDisposableClass>();
                                                };
                
                var myComponent = ioc.GetService<ITestDisposable>();
                Assert.IsNotNull(myComponent);

            }
        }

       

        /// <summary>
        ///It tests releasing for component being registered for more than one interface.
        ///</summary>
        [TestMethod()]
        public void OneComponent_MoreRoles_Test()
        {
            byte released = 0;
            using (var ioc = new Container())
            {

                var regInfo = ioc.As<ITestDisposable>().AndAs<IDemoInterface>().Use<MockDisposableClass>(ioc /*=singleton*/);
                
                regInfo.BeforeRelease += (sender, args) =>
                                                      {
                                                          released++; 
                                                      };


                var myComponent2 = ioc.GetService<IDemoInterface>();
                var myComponent = ioc.GetService<ITestDisposable>();
                Assert.AreSame(myComponent, myComponent2);
            }
            Assert.IsTrue(released==1); //can't be 2!!
        }

      
        
        /// <summary>
        /// Test for methods AfterCreate and BeforeRelease
        ///  </summary>
        [TestMethod()]
        public void  EventTest()
        {
            byte released = 0;
            byte created = 0;
            using (var ioc = new Container())
            {
                //todo: zavest IConfiguredPluginGroup<TCommonType>
                var expr = ioc.As<ITestDisposable>().AndAs<IDemoInterface>().AndAs<IDisposable>();
                IConfiguredFactory<MockDisposableClass> regInfo = expr.Use<MockDisposableClass>(ioc
                    /*=singleton*/);

                regInfo.AfterCreate += (sender, args) =>
                                           {
                                               created++; //must be called only once!
                                           };
                regInfo.BeforeRelease += (sender, args) =>
                                             {
                                                 released++; //must be called only once!
                                                 
                                                 args.RunDispose = true; //it is by default, only for demonstration.
                                             };

                var myComponent1 = ioc.GetService<IDemoInterface>();
                var myComponent2 = ioc.GetService<ITestDisposable>();
                var myComponent3 = ioc.GetService<IDisposable>();
                
                Assert.AreSame(myComponent1, myComponent2);
                Assert.AreSame(myComponent1, myComponent3);
                Assert.IsTrue(created == 1); //was called only once!
            }
            Assert.IsTrue(released == 1);//was called only once!
        }

        /// <summary>
        /// Tests registration with specified ConstructorInfo.
        /// </summary>
        [TestMethod]
        public void UsingConstructorTest()
        {
            using (var ioc = new Container())
            {

                ioc.As(typeof(IList<string>)).Use(typeof(List<string>).GetConstructor(new Type[] { }));//we are using empty constructor for List<>
                var list = ioc.GetService<IList<string>>();
                Assert.IsNotNull(list);
            }
        }

        /// <summary>
        /// Tests  registration with specified ConstructorInfo for opened generic type. It is some as for closed type.
        /// </summary>
        [TestMethod]
        public void UsingConstructorForOpenGenericTest()
        {
            using (var ioc = new Container())
            {

                ioc.As(typeof(IList<>)).Use(typeof(List<>).GetConstructor(new Type[] {}));//we are using empty constructor for List<>
                var list = ioc.GetService<IList<string>>();
                Assert.IsNotNull(list);
            }
        }


        /// <summary>
        /// Tests registration with open generic type.
        /// </summary>
        [TestMethod()]
        public void OpenGeneric_Simple_Test()
        {
            
            using (var ioc = new Container())
            {
                
                IConfiguredFactory regInfo = ioc.As(typeof(IList<>)).Use(typeof(List<>).GetConstructor(Type.EmptyTypes));
                var list = ioc.GetService<IList<string>>();
                Assert.IsNotNull(list);
                
            }
        }

        /// <summary>
        /// Event test - event must occur for opened generic type too.
        /// </summary>
        [TestMethod()]
        public void OpenGeneric_Events_Test()
        {
            bool released = false;
            using (var ioc = new Container())
            {
                bool created = false;

                IConfiguredFactory regInfo =
                    ioc.As(typeof (IList<>)).Use(typeof (List<>).GetConstructor(Type.EmptyTypes), ioc /*=singleton*/);
                regInfo.AfterCreate += (sender, args) =>
                                           {
                                               created = true;
                                              
                                           };
                regInfo.BeforeRelease += (sender, args) =>
                                             {
                                                 released = true;
                                             };
                
                var list = ioc.GetService<IList<string>>();
                Assert.AreEqual(true, created);
                Assert.AreEqual(false, released);
            }
            Assert.AreEqual(true, released);
        }

        /// <summary>
        /// CZ: Test zjistuje, zda-li násobné zaregistrování pomocí otevřené generiky vrací stejnou instanci v případě singletonu.
        /// </summary>
        [TestMethod()]
        public void OpenGeneric_Shared_Test()
        {

            using (var ioc = new Container())
            {
                ioc.ConfigurationManager.SupportForIEnumerable = false;//protoze predefinováváme IEnumerable<>
                
                ioc.As<int>().Use(() => 2); //list potrebuje pocet polozek
                ioc.As(typeof(IList<>)).AndAs(typeof(IEnumerable<>)).Use(typeof(List<>),ioc/*=singleton*/);
                var a = ioc.GetService<IList<string>>();
                Assert.IsNotNull(a);
                var b = ioc.GetService<IEnumerable<string>>();
                Assert.AreSame(a,b);
                
            }
        }

      
      


        

        /// <summary>
        /// CZ: Test, zda pri vyjmuti pluginu  (po zavolání DisposeService) dojde k disposnutí držených instancí.
        /// </summary>
         [TestMethod()]
        public virtual void DisposeServiceTest()
        {
            bool disposed = false;
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().Use<MockDisposableClass>(ioc); //as singleton /* AccordConatiner implements IPersunScope*/
                var a = ioc.GetService<ITestDisposable>();
                a.BeforeDisposeAction = 
                    delegate 
                    { 
                        disposed = true; 
                    };

                Assert.IsTrue(ioc.IsConfiguredFor(typeof(ITestDisposable)));

                ioc.DisposeService(typeof(ITestDisposable));

                Assert.IsTrue(disposed);
                Assert.IsFalse(ioc.IsConfiguredFor(typeof(ITestDisposable)));
            }
        }

         /// <summary>
         /// CZ: Test, zda pri vyjmuti pluginu (po zavolání DisposeService) nedojde k dojde k disposnutí držených instancí, 
         /// pokud je komponenta nastavena jako externě vlastněná
         /// </summary>
         [TestMethod()]
         public virtual void DisposeService2Test()
         {
             bool disposed = false;
             
             using (var ioc = new Container())
             {
                 var registration = ioc.As<ITestDisposable>().Use<MockDisposableClass>(ioc); //as singleton /* AccordConatiner implements IPersunScope*/
                 registration.BeforeRelease += (sender, args) => {args.RunDispose = false;}; //=externě vlastněná

                 ITestDisposable a = ioc.GetService<ITestDisposable>();
                 a.BeforeDisposeAction =
                     delegate
                     {
                         disposed = true;
                     };

                 Assert.IsTrue(ioc.IsConfiguredFor(typeof(ITestDisposable)));

                 ioc.DisposeService(typeof(ITestDisposable));

                 Assert.IsFalse(disposed); //nesmí být disposnuta, i byl plugin zrušen
                 Assert.IsFalse(ioc.IsConfiguredFor(typeof(ITestDisposable)));
             }
            
         }

         /// <summary>
         /// CZ: Test, zda pri uvolnění celého kontejneru dojde k disposnutí držených instancí. 
         /// </summary>
         [TestMethod()]
         public virtual void Dispose1Test()
         {
             bool disposed = false;
             ITestDisposable a;
             using (var ioc = new Container())
             {
                 var registration = ioc.As<ITestDisposable>().Use<MockDisposableClass>(ioc); //as singleton /* AccordConatiner implements IPersunScope*/
                 

                 a = ioc.GetService<ITestDisposable>();
                 a.BeforeDisposeAction =
                     delegate
                     {
                         disposed = true;
                     };

             }
             Assert.IsTrue(disposed);
         }

         /// <summary>
         /// CZ: Test, zda pri uvolněné celého kontejneru nedojde k disposnutí držených instancí, 
         /// pokud je komponenta nastavena jako externě vlastněná
         /// </summary>
         [TestMethod()]
         public virtual void Dispose2Test()
         {
             bool disposed = false;
             ITestDisposable a = null;
             using (var ioc = new Container())
             {
                 var registration = ioc.As<ITestDisposable>().Use<MockDisposableClass>(ioc); //as singleton /* AccordConatiner implements IPersunScope*/
                 registration.BeforeRelease += (sender, args) => { args.RunDispose = false; };

                 a = ioc.GetService<ITestDisposable>();
                 a.BeforeDisposeAction =
                     delegate
                     {
                         disposed = true;
                     };

                 Assert.IsTrue(ioc.IsConfiguredFor(typeof(ITestDisposable)));
             }
             Assert.IsFalse(disposed);
         }

        /// <summary>
         /// CZ: test zkouší, zda-li pokud je typ registrovan jako singleton, tak zda i Func&lt;&gt; varinata i Lazy&lt;&gt; varianta jsou také singletony
        /// </summary>
        [TestMethod]
         public virtual void Advanced1bTest()
         {
             using (var ioc = new Container())
             {
                 //tvorba listu vyzaduje v konstruktoru číslo (kapacita listu)
                 ioc.As(typeof (int)).Use(() => 5);

                

                 ioc.As(typeof (IList<>)).Use(typeof (List<>),ioc/*singleton*/);
                 
                 Assert.AreEqual(5, ioc.GetService<int>()); //jen pro kontrolu

                 var f1 = (Func<IList<string>>) ioc.GetService(typeof (Func<IList<string>>));
                 var f2 = (Func<IList<string>>) ioc.GetService(typeof (Func<IList<string>>));

                 //protoze IList je definovan jako singleton, tak list vraceny pomocí teto fce musí byt vzdy stejný
                 Assert.AreSame(f1(), f2());
                 //a dokonce by to mela byt i stejná fce
                 Assert.AreSame(f1, f2);

                 var l1 = ioc.GetService<Lazy<IList<string>>>();
                 var l2 = ioc.GetService<Lazy<IList<string>>>();

                 //protoze IList je definovan jako singleton, tak list vraceny pomocí teto fce musí byt vzdy stejný
                 Assert.AreSame(l1.Value, l2.Value);
                 //a dokonce by to mela byt i stejné instance
                 Assert.AreSame(l1, l2);

                 var list = ioc.GetService<IList<string>>();
                 //jednoduse vraceny list musí byt stejný jako vraceny pomocí Lazy
                 Assert.AreSame(list, l1.Value);

                 //jednoduse vraceny list musí byt stejný jako list vraceny pomoci funkce 
                 Assert.AreSame(list, f1());
             }
         }

       

        /// <summary>
        /// Test for multithreading.
        /// </summary>
        [TestMethod]
        public void MultiThreadTest()
        {
            using (var ioc = new Container())
            {
                ioc.As<LongTimeConstructedClass>().Use<LongTimeConstructedClass>(ioc);
                List<Task> all = new List<Task>();
                for (int n = 0;n<100;n++)
                {
                    Task t = new Task(()=>ioc.GetService<LongTimeConstructedClass>());
                    all.Add(t);
                    t.Start();
                }
                all.ForEach(t=>t.Wait());

            }
            Assert.AreEqual(1,LongTimeConstructedClass.Counter);
        }

        /// <summary>
        /// Tests registration of one instance
        /// </summary>
        [TestMethod]
        public void RegisterInstanceTest() //todo: tests with opened generics types
        {
            var anyInstance = new MockDisposableClass();
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().Use(anyInstance);
                var myInstance = ioc.GetService<ITestDisposable>(); //creating isntance
                Assert.AreSame(anyInstance,myInstance); //should be some instance
            }
            
        }

        /// <summary>
        /// Test registration existing instance only for current thread
        /// </summary>
        [TestMethod]
        [Ignore] //This feature is not implemented yet.
        public void RegisterInstanceScopedTest() //todo: tests with opened generics types
        {
            var anyInstance = new MockDisposableClass();
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().Use(anyInstance,true,ThreadScope.Instance); //just for current thread
                Task.Factory.StartNew(() =>
                                          {
                                              var myInstance = ioc.GetService<ITestDisposable>();
                                              Assert.IsNull(myInstance); //should be null, because this is another thread
                                          }).Wait();
            }

        }

        /// <summary>
        /// Test registration instance which is externally owned 
        /// </summary>
        [TestMethod]
        public void RegisterInstanceExternallyOwnedTest() //todo: tests with opened generics types
        {
            var anyInstance = new MockDisposableClass();
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().Use(anyInstance, externallyOwned: true);
                ioc.GetService<ITestDisposable>(); //creating isntance
            }
            Assert.AreEqual(false, anyInstance.Disposed);//instance is externally owned -> dispose is not called
        }

        /// <summary>
        /// Test registration instance which is not externally owned 
        /// </summary>
        [TestMethod]
        public void RegisterInstanceNotExternallyOwnedTest() //todo: tests with opened generics types
        {
            var anyInstance = new MockDisposableClass();
            using (var ioc = new Container())
            {
                ioc.As<ITestDisposable>().Use(anyInstance, externallyOwned: false);
                ioc.GetService<ITestDisposable>();//creating isntance
            }
            Assert.AreEqual(true, anyInstance.Disposed);//instance is not externally owned -> dispose has been called
        }
         
    }
}
