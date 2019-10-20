using BencodeNET.Objects;
using NUnit.Framework;
using System.Text;

namespace RatioMaster.Core.Tests
{
    [TestFixture]
    class BEncodeTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            EncodingProvider codePages = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(codePages);
        }

        [Test]
        public void ValueStringTest()
        {
            BString vs = new BString();
            Assert.That(vs.EncodeAsBytes(), Is.EqualTo(new byte[0]));
        }
    }
}
