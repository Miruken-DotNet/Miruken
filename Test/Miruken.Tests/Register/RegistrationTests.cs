﻿// ReSharper disable ClassNeverInstantiated.Local
namespace Miruken.Tests.Register
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Context;
    using Miruken.Register;

    [TestClass]
    public class RegistrationTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _handler = new ServiceCollection()
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.FromAssemblyOf<RegistrationTests>())
                ).Build();
        }

        [TestMethod]
        public void Should_Register_Handlers()
        {
            var action = new Action();
            Assert.IsTrue(_handler.Handle(action));
            Assert.AreEqual(1, action.Handled);
        }

        [TestMethod]
        public void Should_Register_Handlers_As_Singleton_By_Default()
        {
            var handler = _handler.Resolve<PrivateHandler>();
            Assert.IsNotNull(handler);
            Assert.AreSame(handler, _handler.Resolve<PrivateHandler>());
        }

        [TestMethod]
        public void Should_Register_Instances()
        {
            var service = new Service1();
            using var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddMiruken()
                .Build();
            Assert.AreSame(service, handler.Resolve<IService>());
        }

        [TestMethod]
        public void Should_Register_Singleton()
        {
            using var handler = new ServiceCollection()
                .AddSingleton<Service1>()
                .AddMiruken()
                .Build();
            var service = handler.Resolve<IService>();
            Assert.AreSame(service, handler.Resolve<IService>());
            var contextual = service as IContextual;
            Assert.IsNotNull(contextual?.Context);
            Assert.IsNull(contextual.Context.Parent);
        }

        [TestMethod]
        public void Should_Register_Scoped()
        {
            using var handler = new ServiceCollection()
                .AddScoped<Service1>()
                .AddMiruken()
                .Build();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.IsNotNull((service as IContextual)?.Context);
        }

        [TestMethod]
        public void Should_Register_Scoped_Fluently()
        {
            using var handler = new ServiceCollection()
                .AddMiruken(configure => configure
                    .Sources(sources => sources
                        .AddTypes(typeof(Service1)))
                    .Select((selector, publicOnly) => selector
                        .AddClasses(x => x.AssignableTo<IService>(), publicOnly)
                            .AsSelf().WithScopedLifetime()
                )).Build();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.IsNotNull((service as IContextual)?.Context);
        }

        [TestMethod]
        public void Should_Use_Last_Explicitly_Registered_Service()
        {
            using (var handler = new ServiceCollection()
                .AddSingleton<Service1>()
                .AddSingleton<Service2>()
                .AddMiruken()
                .Build())
            {
                var service = handler.Resolve<IService>();
                Assert.IsInstanceOfType(service, typeof(Service2));
            }

            using (var handler = new ServiceCollection()
                .AddSingleton<Service2>()
                .AddSingleton<Service1>()
                .AddMiruken()
                .Build())
            {
                var service = handler.Resolve<IService>();
                Assert.IsInstanceOfType(service, typeof(Service1));
            }
        }

        [TestMethod]
        public void Should_Register_Transient_Service_Factory()
        {
            using var handler = new ServiceCollection()
                .AddTransient(sp => new Service1())
                .AddMiruken()
                .Build();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreNotSame(service, handler.Resolve<IService>());
            Assert.IsNotNull(handler.Resolve<Service1>());
            Assert.IsNull((service as IContextual)?.Context);
        }

        [TestMethod]
        public void Should_Register_Singleton_Service_Factory()
        {
            using var handler = new ServiceCollection()
                .AddSingleton(sp => new Service1())
                .AddMiruken()
                .Build();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreSame(service, handler.Resolve<IService>());
            Assert.AreSame(service, handler.Resolve<Service1>());
            var contextual = service as IContextual;
            Assert.IsNotNull(contextual?.Context);
            Assert.IsNull(contextual.Context.Parent);
        }

        [TestMethod]
        public void Should_Register_Scoped_Service_Factory()
        {
            using var handler = new ServiceCollection()
                .AddScoped(sp => new Service1())
                .AddMiruken()
                .Build();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreSame(service, handler.Resolve<IService>());
            Assert.AreSame(service, handler.GetService<IService>());
            using var scope   = handler.CreateScope();
            var scopedService = scope.ServiceProvider.GetService<IService>();
            var context       = (scopedService as IContextual)?.Context;
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Parent);
            Assert.AreNotSame(service, scopedService);
            Assert.AreSame(service, handler.Resolve<IService>());
        }

        [TestMethod]
        public void Should_Dispose_Singletons()
        {
            Service2 service;
            using (var handler = new ServiceCollection()
                .AddSingleton<Service2>()
                .AddMiruken()
                .Build())
            {
                service = handler.Resolve<Service2>();
                Assert.AreSame(service, handler.Resolve<Service2>());
            }

            Assert.IsTrue(service.Disposed);
        }

        [TestMethod]
        public void Should_Dispose_Scoped()
        {
            Service2 service;
            using (var handler = new ServiceCollection()
                .AddScoped<Service2>()
                .AddMiruken()
                .Build())
            {
                service = handler.Resolve<Service2>();
                Assert.AreSame(service, handler.Resolve<Service2>());
            }

            Assert.IsTrue(service.Disposed);
        }

        [TestMethod]
        public void Should_Override_Handlers()
        {
            var service  = new Service1();
            var services = new ServiceCollection();
            using var handler = services
                .AddTransient<Service1>()
                .AddMiruken(configure => configure.With(service))
                .Build();
            Assert.AreSame(service, handler.Resolve<IService>());
        }

        [TestMethod]
        public void Should_Handle_Composition()
        {
            var service = new Service1();
            using var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddTransient<Service2>()
                .AddTransient<CompositeService>()
                .AddMiruken()
                .Build();
            var c = handler.Resolve<CompositeService>();
            Assert.AreEqual(2, c.Services.Count());
            Assert.IsTrue(c.Services.Contains(service));
            Assert.IsTrue(c.Services.OfType<Service2>().Any());
        }

        [TestMethod]
        public void Should_Handle_Composition_With_Factories()
        {
            var service = new Service1();
            using var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddTransient<Service2>()
                .AddTransient(sp =>
                {
                    var s = sp.GetService<IEnumerable<IService>>();
                    return new CompositeService(s);
                })
                .AddMiruken()
                .Build();
            var c = handler.Resolve<CompositeService>();
            Assert.AreEqual(2, c.Services.Count());
            Assert.IsTrue(c.Services.Contains(service));
            Assert.IsTrue(c.Services.OfType<Service2>().Any());
        }

        [TestMethod]
        public void Should_Register_Ctor_With_Func()
        {
            using var handler = new ServiceCollection()
                .AddSingleton<ServiceWithFuncCtor>()
                .AddSingleton<Func<IService>>(() => new Service1())
                .AddMiruken()
                .Build();
            var s = handler.Resolve<ServiceWithFuncCtor>();
            Assert.IsNotNull(s);
            Assert.IsNotNull(s.Service);
        }

        [TestMethod]
        public void Should_Use_Root_Context_For_Singleton_ServiceProvider_And_ScopeFactory()
        {
            using var context = new ServiceCollection()
                .AddSingleton<ServiceWithServiceProvider>()
                .AddMiruken()
                .Build();
            using var child = context.CreateChild();
            var service = child.Resolve<ServiceWithServiceProvider>();
            Assert.IsNotNull(service);
            Assert.AreSame(context, service.Services);
            Assert.AreSame(context, service.ScopeFactory);
        }

        [TestMethod]
        public void Should_Work_With_Creates()
        {
            using var handler = new ServiceCollection()
                .AddSingleton<Service1>()
                .AddMiruken()
                .Build();
            var service1 = handler.Resolve<Service1>();
            Assert.IsNotNull(service1);
            Assert.AreSame(service1, handler.Resolve<Service1>());
            Assert.AreNotSame(service1, handler.Create<Service1>());
        }

        [TestMethod]
        public void Should_Work_With_Constrained_Open_Generics()
        {
            using var handler = new ServiceCollection()
                .AddSingleton(typeof(UserInfoValidator<>))
                .AddSingleton<UserDataValidator>()
                .AddMiruken()
                .Build();
            var validators = handler.ResolveAll<IValidator<UserData>>();
            Assert.AreEqual(2, validators.Length);
            Assert.IsTrue(validators.OfType<UserInfoValidator<UserData>>().Any());
            Assert.IsTrue(validators.OfType<UserDataValidator>().Any());
            Assert.IsNull(handler.Resolve<IValidator<PersonData>>());
        }


        public class Action
        {
            public int Handled { get; set; }
        }

        public class PrivateHandler : Handler
        {
            [Handles]
            public void Process(Action action)
            {
                ++action.Handled;
            }
        }

        public interface IService
        {
            void DoSomething();
        }

        public class Service1 : IService, IContextual
        {
            private Context _context;

            [Creates]
            public Service1()
            {
            }
            public event ContextChangingDelegate ContextChanging;
            public event ContextChangedDelegate  ContextChanged;
            
            public void DoSomething()
            {
            }

            public Context Context
            {
                get => _context;
                set
                {
                    if (_context == value) return;
                    var newContext = value;
                    ContextChanging?.Invoke(this, _context, ref newContext);
                    var oldContext = _context;
                    _context = newContext;
                    ContextChanged?.Invoke(this, oldContext, _context);
                }
            }
        }

        public class Service2 : IService, IDisposable
        {
            public bool Disposed { get; private set; }

            public void DoSomething()
            {
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public class CompositeService : IService
        {
            public IEnumerable<IService> Services { get; }

            public CompositeService(IEnumerable<IService> services)
            {
                Services = services;
            }

            public void DoSomething()
            {
                foreach (var service in Services)
                    service.DoSomething();
            }
        }

        public class ServiceWithFuncCtor
        {
            public ServiceWithFuncCtor(Func<IService> service)
            {
                Service = service();
            }

            public IService Service { get; }
        }

        public class ServiceWithServiceProvider
        {
            public ServiceWithServiceProvider(
                IServiceProvider services, IServiceScopeFactory scopeFactory)
            {
                Services     = services;
                ScopeFactory = scopeFactory;
            }

            public IServiceProvider     Services     { get; }
            public IServiceScopeFactory ScopeFactory { get; }
        }

        public interface IValidator<in T>
        {
            bool Validate(T instance);
        }

        public interface IContainUserInfo
        {
        }

        public class UserInfoValidator<T> : IValidator<T>
            where T : IContainUserInfo
        {
            public bool Validate(T instance)
            {
                return true;
            }
        }

        public class UserDataValidator : IValidator<UserData>
        {
            public bool Validate(UserData instance)
            {
                return true;
            }
        }

        public class UserData : IContainUserInfo
        {
        }

        public class PersonData
        {
        }
    }
}
