using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class NullServicesTests
    {
        [Test]
        public void TheNullAudio_StartsOn_AndRemembersEachSwitch()
        {
            var audio = new NullAudioService();
            Assert.IsTrue(audio.IsEnabled(AudioChannel.Sound));
            Assert.IsTrue(audio.IsEnabled(AudioChannel.Music));

            audio.SetEnabled(AudioChannel.Sound, false);

            Assert.IsFalse(audio.IsEnabled(AudioChannel.Sound));
            Assert.IsTrue(audio.IsEnabled(AudioChannel.Music));
        }

        [Test]
        public void TheNullAnalytics_NeverThrows()
        {
            var analytics = new NullAnalyticsService();

            Assert.DoesNotThrow(() => analytics.Track("level_won", null));
            Assert.DoesNotThrow(() => analytics.Track("level_won", new Dictionary<string, string> { { "level", "3" } }));
            Assert.DoesNotThrow(() => analytics.Track(null, null));
        }
    }
}
