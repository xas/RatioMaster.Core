using BitTorrent;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            ValueString vs = new ValueString();
            Assert.That(vs.Bytes, Is.EqualTo(new byte[0]));
        }
    }
}
