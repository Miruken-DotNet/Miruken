namespace Miruken.Map.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Infrastructure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MappingTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void Setup()
        {
            _handler = new MappingHandler();
        }

        [TestMethod]
        public void Should_Map_Implicitly()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            var data = (new EntityMapping() + _handler)
                .Proxy<IMapping>().Map<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public async Task Should_Map_Implicitly_Async()
        {
            var entity = new PlayerEntity
            {
                Id   = 2,
                Name = "David Silva"
            };
            var data = await (new EntityMapping() + _handler)
                .Proxy<IMapping>().MapAsync<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public void Should_Map_Explicitly()
        {
            var handler = (new ExplicitMapping() + _handler);
            var player = new PlayerData
            {
                Id   = 3,
                Name = "Franz Beckenbauer"
            };
            var json = handler.Proxy<IMapping>().Map<string>(player, "application/json");
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json);
            var data = handler.Proxy<IMapping>().Map<PlayerData>(json, "application/json");
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("Franz Beckenbauer", player.Name);
        }

        [TestMethod]
        public async Task Should_Map_Explicitly_Async()
        {
            var handler = (new ExplicitMapping() + _handler);
            var player = new PlayerData
            {
                Id   = 3,
                Name = "Franz Beckenbauer"
            };
            var json = await handler.Proxy<IMapping>().MapAsync<string>(player, "application/json");
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json);
            var data = handler.Proxy<IMapping>().Map<PlayerData>(json, "application/json");
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("Franz Beckenbauer", player.Name);
        }

        [TestMethod]
        public void Should_Map_Using_Existing_Instance()
        {
            var entity = new PlayerEntity
            {
                Id   = 9,
                Name = "Diego Maradona"
            };
            var player = new PlayerData();
            var data   = (new EntityMapping() + _handler)
                .Proxy<IMapping>().MapInto(entity, player);
            Assert.AreSame(player, data);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod,
         ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Missing_Mapping()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            _handler.Proxy<IMapping>().Map<PlayerData>(entity);
        }

        [TestMethod,
         ExpectedException(typeof(MissingMethodException))]
        public async Task Should_Reject_Missing_Mapping_Async()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            await _handler.Proxy<IMapping>().MapAsync<PlayerData>(entity);
        }

        [TestMethod]
        public void Should_Map_To_Dictionary()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Marco Royce"
            };
            var data = (new EntityMapping() + _handler)
                .Proxy<IMapping>().Map<IDictionary<string, object>>(entity);
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual(1, data["Id"]);
            Assert.AreEqual("Marco Royce", data["Name"]);
        }

        [TestMethod]
        public void Should_Map_From_Dictionary()
        {
            var data = new Dictionary<string, object>
            {
                {"Id",   1},
                {"Name", "Geroge Best"}
            };
            var entity = (new EntityMapping() + _handler)
                .Proxy<IMapping>().Map<PlayerEntity>(data);
            Assert.AreEqual(1, entity.Id);
            Assert.AreEqual("Geroge Best", entity.Name);
        }

        [TestMethod]
        public void Should_Map_Resolving()
        {
            HandlerDescriptor.GetDescriptor<ExceptionMapping>();
            var exception = new ArgumentException("Value is bad");
            var value     = (new ExceptionMapping() + _handler).Resolve()
                .Proxy<IMapping>().Map<object>(exception);
            Assert.AreEqual("System.ArgumentException: Value is bad", value);
        }

        [TestMethod]
        public void Should_Map_Simple_Results()
        {
            HandlerDescriptor.GetDescriptor<ExceptionMapping>();
            var handler   = (new ExceptionMapping() + _handler).Resolve();
            var exception = new NotSupportedException("Close not found");
            var value     = handler.Proxy<IMapping>().Map<object>(exception);
            Assert.AreEqual(500, value);
            value = handler.Proxy<IMapping>().Map<object>(
                new InvalidOperationException("Operation not allowed"));
            Assert.AreEqual("Operation not allowed", value);
        }

        [TestMethod]
        public void Should_Map_Simple_Default_If_Best_Effort()
        {
            HandlerDescriptor.GetDescriptor<ExceptionMapping>();
            var value = (new ExceptionMapping() + _handler)
                .Resolve().BestEffort()
                .Proxy<IMapping>().Map<int>(new AggregateException());
            Assert.AreEqual(0, value);
        }

        [TestMethod]
        public async Task Should_Map_Simple_Default_If_Best_Effort_Async()
        {
            HandlerDescriptor.GetDescriptor<ExceptionMapping>();
            var value = await (new ExceptionMapping() + _handler)
                .Resolve().BestEffort()
                .Proxy<IMapping>().MapAsync<int>(new AggregateException());
            Assert.AreEqual(0, value);
        }

        [TestMethod]
        public void Should_Perform_Open_Mapping()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            var data = (new OpenMapping() + _handler)
                .Proxy<IMapping>().Map<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public void Should_Bundle_Mapping()
        {
            var entity = new PlayerEntity
            {
                Id   = 4,
                Name = "Michel Platini"
            };
            var player = new PlayerData
            {
                Id   = 12,
                Name = "Roberto Carlose"
            };
            var handler = new EntityMapping() + new ExplicitMapping() + _handler;
            var handled = handler.All(bundle =>
                bundle.Add(h =>
                {
                    var data = h.Proxy<IMapping>().Map<PlayerData>(entity);
                    Assert.AreEqual(entity.Id, data.Id);
                    Assert.AreEqual(entity.Name, data.Name);
                }).Add(h =>
                {
                    var json = handler.Proxy<IMapping>().Map<string>(player, "application/json");
                    Assert.AreEqual("{id:12,name:'Roberto Carlose'}", json);

                }));
            Assert.IsTrue(handled);
        }

        private class Entity
        {
            public int Id { get; set; }
        }

        private class PlayerEntity : Entity
        {
            public string Name { get; set; }
        }

        private class PlayerData
        {
            public int    Id   { get; set; }
            public string Name { get; set; }
        }

        private class EntityMapping : Handler
        {
            [Maps]
            public PlayerData MapToPlayerData(PlayerEntity entity, Mapping mapping)
            {
                var instance = mapping.Target as PlayerData;
                if (instance == null)
                    return new PlayerData
                    {
                        Id   = entity.Id,
                        Name = entity.Name
                    };
                instance.Id   = entity.Id;
                instance.Name = entity.Name;
                return instance;
            }

            [Maps]
            public IDictionary<string, object> MapToPlayerDictionary(PlayerEntity entity)
            {
                return new Dictionary<string, object>
                {
                    { "Id",   entity.Id },
                    { "Name", entity.Name }
                };
            }

            [Maps]
            public PlayerEntity MapFromPlayerDictionary(IDictionary<string, object> keyValues)
            {
                return new PlayerEntity
                {
                    Id   = (int)keyValues["Id"],
                    Name = (string)keyValues["Name"]
                };
            }
        }

        public class ExceptionMapping : Handler
        {
            [Maps]
            public string MapArgumentException(ArgumentException ex)
            {
                return ex.ToString();
            }

            [Maps]
            public int MapArgumentException(NotSupportedException ex)
            {
                return 500;
            }

            [Maps]
            public string MapException(Exception ex)
            {
                return ex.Message;
            }
        }

        private class OpenMapping : Handler
        {
            [Maps]
            public object Map(Mapping mapping)
            {
                var playerEntity = mapping.Source as PlayerEntity;
                if (playerEntity != null && 
                    (mapping.TypeOrInstance as Type).Is<PlayerData>())
                {
                    return new PlayerData
                    {
                        Id   = playerEntity.Id,
                        Name = playerEntity.Name
                    };
                }
                return null;
            }
        }

        private class ExplicitMapping : Handler
        {
            [Maps, Format("application/json")]
            public string ToJsonPlayer(PlayerData player)
            {
                return $"{{id:{player.Id},name:'{player.Name}'}}";
            }

            [Maps, Format("application/json")]
            public PlayerData FromJsonPlayer(string json)
            {
                var player = new PlayerData();
                var parts  = json.Split(new [] { '{', '}', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var keyValue = part.Split(':');
                    switch (keyValue[0].ToLower())
                    {
                        case "id":
                            player.Id = int.Parse(keyValue[1]);
                            break;
                        case "name":
                            player.Name = keyValue[1].TrimEnd('\'');
                            break;
                    }
                }
                return player;
            }
        }
    }
}
