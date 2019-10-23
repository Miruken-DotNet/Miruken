namespace Miruken.Http.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Format;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ReadWriteFormUrlEncodedMediaTypeFormatterTests
    {
        [TestMethod]
        public async Task Shouls_Format_Form_Encodeded_Data()
        {
            var player = new RosteredPlayer
            {
                Id   = 11,
                Name = "Wayne Rooney",
                DOB  = new DateTime(1985, 10, 24)
            };

            var content = new ObjectContent<Player>(player,
                new ReadWriteFormUrlEncodedMediaTypeFormatter
                {
                    DateTimeFormat = "MM/dd/yyyy hh:mm tt"
                });

            var form = await content.ReadAsStringAsync();

            Assert.AreEqual("DOB=10%2F24%2F1985+12%3A00+AM&Id=11&Name=Wayne+Rooney", form);
        }

        public class RosteredPlayer : Player
        {
            public DateTime DOB { get; set; }
        }
    }
}
