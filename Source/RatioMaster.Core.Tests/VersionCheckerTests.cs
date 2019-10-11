using System;
using NUnit.Framework;
using RatioMaster.Core;

namespace RatioMaster.Tests
{
    [TestFixture]
    public class VersionCheckerTests
    {
        [Test]
        public void GetServerVersionIdShouldReturnExactlyFourCharacters()
        {
            var versionChecker = new VersionChecker(string.Empty);
            var serverVersion = versionChecker.GetServerVersionId();
            Console.WriteLine(serverVersion);
            Assert.AreEqual(4, serverVersion.Length);
        }
    }
}
