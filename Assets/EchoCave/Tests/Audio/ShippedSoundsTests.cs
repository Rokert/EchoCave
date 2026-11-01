using System;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Tests
{
    // Checks the real assets, so a missing clip or an empty bank fails the suite instead of staying silent in the game.
    public class ShippedSoundsTests
    {
        private const string SfxFolder = "Assets/EchoCave/Audio/Sfx";

        [Test]
        public void EveryCue_HasItsClipInTheSfxFolder()
        {
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                string path = $"{SfxFolder}/{SoundCueNames.FileName(cue)}.wav";

                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), $"{cue}: no clip at {path}");
            }
        }

        [Test]
        public void TheSoundBank_ListsEveryCue_WithAClipAndAVolume()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundBank");
            Assert.AreEqual(1, guids.Length, "Expected exactly one SoundBank asset (Assets > Create > EchoCave > Sound Bank, then EchoCave > Audio > Fill sound bank).");
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Assert.IsEmpty(bank.FindProblems(), string.Join("\n", bank.FindProblems()));
        }
    }
}
