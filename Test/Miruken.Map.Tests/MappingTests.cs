namespace Miruken.Map.Tests
{
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
                .Proxy<IMapping>().MapFrom<PlayerData>(entity);
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
            [MapsFrom]
            public PlayerData MapFromEntity(PlayerEntity entity)
            {
                return new PlayerData
                {
                    Id   = entity.Id,
                    Name = entity.Name
                };
            }
        }
    }
}
