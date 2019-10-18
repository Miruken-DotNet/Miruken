namespace Miruken.Tests.Map
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Infrastructure;
    using Miruken.Map;

    [TestClass]
    public class MappingTests
    {
        private IHandlerDescriptorFactory _factory;

        [TestInitialize]
        public void TestInitialize()
        {
            _factory = new MutableHandlerDescriptorFactory();
            _factory.RegisterDescriptor<EntityMapping>();
            _factory.RegisterDescriptor<ExplicitMapping>();
            _factory.RegisterDescriptor<OpenMapping>();
            _factory.RegisterDescriptor<ExceptionMapping>();
            HandlerDescriptorFactory.UseFactory(_factory);
        }

        [TestMethod]
        public void Should_Map_Implicitly()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            var data = new EntityMapping().Map<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public void Should_Map_Existing_Target()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            var target = new PlayerData();
            var data   = new EntityMapping()
                .MapInto(entity, target, "ExistingTarget");
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
            var data = await new EntityMapping()
                .MapAsync<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public void Should_Map_Explicitly()
        {
            var handler = new ExplicitMapping();
            var player = new PlayerData
            {
                Id   = 3,
                Name = "Franz Beckenbauer"
            };
            var json = handler.Map<string>(player, "application/json");
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json);
            var data = handler.Map<PlayerData>(json, "application/json");
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("Franz Beckenbauer", player.Name);
        }

        [TestMethod]
        public async Task Should_Map_Explicitly_Async()
        {
            var handler = new ExplicitMapping();
            var player = new PlayerData
            {
                Id   = 3,
                Name = "Franz Beckenbauer"
            };
            var json = await handler.MapAsync<string>(player, "application/json");
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json);
            var data = handler.Map<PlayerData>(json, "application/json");
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
            var data   = new EntityMapping().MapInto(entity, player);
            Assert.AreSame(player, data);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
        }

        [TestMethod]
        public void Should_Map_All_Implicitly()
        {
            var entities = new[]
            {
                new PlayerEntity
                {
                    Id   = 1,
                    Name = "Tim Howard"
                },
                new PlayerEntity
                {
                    Id   = 9,
                    Name = "Josh Sargent"
                }
            };
            var data = new EntityMapping().MapAll<PlayerData>(entities);
            Assert.AreEqual(2, data.Length);
            for (var i = 0; i < data.Length; ++i)
            {
                Assert.AreEqual(data[i].Id, entities[i].Id);
                Assert.AreEqual(data[i].Name, entities[i].Name);
            }
        }

        [TestMethod]
        public async Task Should_Map_All_Implicitly_Async()
        {
            var entities = new[]
            {
                new PlayerEntity
                {
                    Id   = 2,
                    Name = "David Silva"
                },
                new PlayerEntity
                {
                    Id   = 9,
                    Name = "Marco Asensio"
                }
            };
            var data = await new EntityMapping()
                .MapAllAsync<PlayerData>(entities);
            Assert.AreEqual(2, data.Length);
            for (var i = 0; i < data.Length; ++i)
            {
                Assert.AreEqual(data[i].Id, entities[i].Id);
                Assert.AreEqual(data[i].Name, entities[i].Name);
            }
        }

        [TestMethod]
        public void Should_Map_All_Explicitly()
        {
            var handler = new ExplicitMapping();
            var players = new []
            {
                new PlayerData
                {
                    Id   = 3,
                    Name = "Franz Beckenbauer"
                },
                new PlayerData
                {
                    Id   = 8,
                    Name = "Toni Kroos"
                }
            };
            var json = handler.MapAll<string>(players, "application/json");
            Assert.AreEqual(2, json.Length); 
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json[0]);
            Assert.AreEqual("{id:8,name:'Toni Kroos'}", json[1]);
        }

        [TestMethod]
        public async Task Should_Map_All_Explicitly_Async()
        {
            var handler = new ExplicitMapping();
            var players = new[]
            {
                new PlayerData
                {
                    Id   = 3,
                    Name = "Franz Beckenbauer"
                },
                new PlayerData
                {
                    Id   = 8,
                    Name = "Toni Kroos"
                }
            };
            var json = await handler.MapAllAsync<string>(players, "application/json");
            Assert.AreEqual(2, json.Length);
            Assert.AreEqual("{id:3,name:'Franz Beckenbauer'}", json[0]);
            Assert.AreEqual("{id:8,name:'Toni Kroos'}", json[1]);
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Reject_Missing_Mapping()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            new Handler().Map<PlayerData>(entity);
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Reject_Missing_Mapping_Async()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Tim Howard"
            };
            await new Handler().MapAsync<PlayerData>(entity);
        }

        [TestMethod]
        public void Should_Map_To_Dictionary()
        {
            var entity = new PlayerEntity
            {
                Id   = 1,
                Name = "Marco Royce"
            };
            var data = new EntityMapping()
                .Map<IDictionary<string, object>>(entity);
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
            var entity = new EntityMapping().Map<PlayerEntity>(data);
            Assert.AreEqual(1, entity.Id);
            Assert.AreEqual("Geroge Best", entity.Name);
        }

        [TestMethod]
        public void Should_Map_From_Array()
        {
            var data = new Dictionary<string, object>
            {
                {"Id",   1},
                {"Name", "George Best"}
            };
            var entity = new EntityMapping()
                .Map<PlayerEntity>(data.ToArray());
            Assert.AreEqual(1, entity.Id);
            Assert.AreEqual("George Best", entity.Name);
        }

        [TestMethod]
        public void Should_Map_Resolving()
        {
            _factory.RegisterDescriptor<ExceptionMapping>();
            var exception = new ArgumentException("Value is bad");
            var value     = new ExceptionMapping().Map<object>(exception);
            Assert.AreEqual("System.ArgumentException: Value is bad", value);
        }

        [TestMethod]
        public void Should_Map_Simple_Results()
        {
            _factory.RegisterDescriptor<ExceptionMapping>();
            var handler   = new ExceptionMapping();
            var exception = new NotSupportedException("Close not found");
            var value     = handler.Map<object>(exception);
            Assert.AreEqual(500, value);
            value = handler.Map<object>(
                new InvalidOperationException("Operation not allowed"));
            Assert.AreEqual("Operation not allowed", value);
        }

        [TestMethod]
        public void Should_Map_Simple_Default_If_Best_Effort()
        {
            _factory.RegisterDescriptor<ExceptionMapping>();
            var value = new ExceptionMapping()
                .BestEffort()
                .Map<int>(new AggregateException());
            Assert.AreEqual(0, value);
        }

        [TestMethod]
        public async Task Should_Map_Simple_Default_If_Best_Effort_Async()
        {
            _factory.RegisterDescriptor<ExceptionMapping>();
            var value = await new ExceptionMapping()
                .BestEffort()
                .MapAsync<int>(new AggregateException());
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
            var data = new OpenMapping().Map<PlayerData>(entity);
            Assert.AreEqual(entity.Id, data.Id);
            Assert.AreEqual(entity.Name, data.Name);
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
                if (!(mapping.Target is PlayerData instance))
                    return new PlayerData
                    {
                        Id = entity.Id,
                        Name = entity.Name
                    };
                instance.Id   = entity.Id;
                instance.Name = entity.Name;
                return instance;
            }

            [Maps, Format("ExistingTarget")]
            public PlayerData MapToExistingPlayerData(PlayerEntity entity, PlayerData data)
            {
                if (data == null)
                    return new PlayerData
                    {
                        Id   = entity.Id,
                        Name = entity.Name
                    };
                data.Id   = entity.Id;
                data.Name = entity.Name;
                return data;
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

            [Maps]
            public PlayerEntity MapFromPlayerKeyValues(KeyValuePair<string, object>[] keyValues)
            {
                var entity = new PlayerEntity();
                foreach (var keyValue in keyValues)
                {
                    switch (keyValue.Key)
                    {
                        case "Id":
                            entity.Id = (int)keyValue.Value;
                            break;
                        case "Name":
                            entity.Name = keyValue.Value.ToString();
                            break;
                    }
                }
                return entity;
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
                if (mapping.Source is PlayerEntity playerEntity && 
                    (mapping.TypeOrTarget as Type).Is<PlayerData>())
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
