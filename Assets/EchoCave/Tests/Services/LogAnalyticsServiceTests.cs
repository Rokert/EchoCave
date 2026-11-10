using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LogAnalyticsServiceTests
    {
        [Test]
        public void AnEvent_BecomesOneLine_WithItsParameters()
        {
            var lines = new List<string>();
            var service = new LogAnalyticsService(lines.Add);

            service.Track("level_win", new Dictionary<string, string> { { "level", "20" }, { "moves", "7" } });

            Assert.AreEqual(new[] { "[Analytics] level_win level=20 moves=7" }, lines);
        }

        [Test]
        public void NoParameters_IsFine()
        {
            var lines = new List<string>();

            new LogAnalyticsService(lines.Add).Track("tutorial_end", null);

            Assert.AreEqual(new[] { "[Analytics] tutorial_end" }, lines);
        }

        [Test]
        public void ANullLogger_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new LogAnalyticsService(null));
        }
    }
}
