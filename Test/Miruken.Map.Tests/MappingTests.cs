namespace Miruken.Map.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
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

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
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
         ExpectedException(typeof(InvalidOperationException))]
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
            public PlayerData MapToPlayerData(PlayerEntity entity)
            {
                return new PlayerData
                {
                    Id   = entity.Id,
                    Name = entity.Name
                };
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
        }

        private class OpenMapping : Handler
        {
            [Maps]
            public object Map(Mapping mapping)
            {
                var playerEntity = mapping.Source as PlayerEntity;
                if (playerEntity != null &&
                    Equals(mapping.Format, typeof(PlayerData)))
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
    }
}
