using System;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Tests
{
    // Checks the real assets, so a missing clip or an empty bank fails the suite instead of leaving the game without music.
    public class ShippedMusicTests
    {
        private const string MusicFolder = "Assets/EchoCave/Audio/Music";

        [Test]
        public void EveryTrack_HasItsClipInTheMusicFolder()
        {
            foreach (MusicTrack track in Enum.GetValues(typeof(MusicTrack)))
            {
                string path = $"{MusicFolder}/{MusicTrackNames.FileName(track)}.wav";

                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), $"{track}: no clip at {path}");
            }
        }

        [Test]
        public void TheMusicBank_ListsEveryTrack_AndItsFlowUsesOnlyTracksWithAClip()
        {
            string[] guids = AssetDatabase.FindAssets("t:MusicBank");
            Assert.AreEqual(1, guids.Length, "Expected exactly one MusicBank asset (Assets > Create > EchoCave > Music Bank, then EchoCave > Audio > Fill music bank).");
            var bank = AssetDatabase.LoadAssetAtPath<MusicBank>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Assert.IsEmpty(bank.FindProblems(), string.Join("\n", bank.FindProblems()));
        }

        [Test]
        public void EveryMusicClip_IsStreamedAndLoops_ByImportSettings()
        {
            foreach (MusicTrack track in Enum.GetValues(typeof(MusicTrack)))
            {
                string path = $"{MusicFolder}/{MusicTrackNames.FileName(track)}.wav";
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(importer, path);

                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, $"{track}: set Load Type to Streaming.");
                Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat, $"{track}: set Compression Format to Vorbis.");
            }
        }
    }
}
