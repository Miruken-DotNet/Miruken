namespace Miruken.Map.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
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
        public void Should_Perform_Implicit_Mapping()
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
        public async Task Should_Perform_Implicit_Mapping_Async()
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

        private class PlayerEntity
        {
            public int    Id   { get; set; }
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
            public PlayerData MapFromEntity(PlayerEntity entity)
            {
                return new PlayerData
                {
                    Id   = entity.Id,
                    Name = entity.Name
                };
            }
        }

        private class DictionaryMapping : Handler
        {
            [Maps]
            public IDictionary<string, object> MapToDictionary(PlayerEntity entity)
            {
                return new Dictionary<string, object>
                {
                    { "Id",   entity.Id },
                    { "Name", entity.Name }
                };
            }

            [Maps]
            public PlayerEntity MapToEntity(IDictionary<string, object> keyValues)
            {
                return null;
            }
        }
    }
}
