using System.Collections;
using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Cuándo habla cada personaje (docs/08 §2 "Voces"). El enfriamiento de 3 s se prueba en VoiceGateTests.
    public class CharacterVoiceTests
    {
        private RaftTestRig rig;
        private CharacterVoice voice;
        private PlayerAvatar avatar;
        private PlayerAvatar other;
        private float now;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            avatar = rig.AddAvatar(150f, "Alex").GetComponent<PlayerAvatar>();
            other = rig.AddAvatar(210f, "Marty").GetComponent<PlayerAvatar>();
            voice = avatar.GetComponent<CharacterVoice>();
            now = 100f;
            CharacterVoice.Clock = () => now;
            CharacterVoice.Random01 = () => 0.1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CharacterVoice.Clock = () => Time.unscaledTime;
            CharacterVoice.Random01 = () => Random.value;
            rig.Destroy();
            yield return null;
        }

        [Test]
        public void Prefab_HasVoiceClipsForEveryKind()
        {
            Assert.IsNotNull(voice);
            var sfx = avatar.Character.sfxSet;
            Assert.Greater(sfx.voiceEffort.clips.Length, 0);
            Assert.Greater(sfx.voiceFall.clips.Length, 0);
            Assert.Greater(sfx.voiceLaugh.clips.Length, 0);
            Assert.Greater(sfx.voiceCheer.clips.Length, 0);
        }

        [Test]
        public void SuperFart_MakesEffort()
        {
            GameEvents.RaiseSuperFart(1f);
            Assert.AreEqual(VoiceKind.Effort, voice.LastSpoken);
        }

        [Test]
        public void OwnFall_MakesTheFallVoice_EvenRightAfterAnotherVoice()
        {
            GameEvents.RaiseSuperFart(1f);
            GameEvents.RaisePlayerFell(avatar);
            Assert.AreEqual(VoiceKind.Fall, voice.LastSpoken, "caer siempre suena");
        }

        [Test]
        public void AnotherPlayersFall_LaughsOnly30PercentOfTheTime()
        {
            CharacterVoice.Random01 = () => 0.29f;
            GameEvents.RaisePlayerFell(other);
            Assert.AreEqual(VoiceKind.Laugh, voice.LastSpoken);

            var quiet = rig.AddAvatar(90f, "Gloria").GetComponent<CharacterVoice>();
            CharacterVoice.Random01 = () => 0.31f;
            GameEvents.RaisePlayerFell(other);
            Assert.IsNull(quiet.LastSpoken);
        }

        [Test]
        public void Sync_LaughsFromThreePlayers()
        {
            GameEvents.RaiseSync(2, 1.5f);
            Assert.IsNull(voice.LastSpoken, "x2 no hace reír");

            GameEvents.RaiseSync(3, 2f);
            Assert.AreEqual(VoiceKind.Laugh, voice.LastSpoken);
        }

        [Test]
        public void Checkpoint_Cheers()
        {
            GameEvents.RaiseCheckpoint(1);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken);
        }

        [Test]
        public void Finish_Cheers()
        {
            GameEvents.RaiseLevelFinished(42f);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken);
        }

        [Test]
        public void Achievement_Cheers()
        {
            GameEvents.RaiseAchievementUnlocked("first_sync");
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken);
        }

        [Test]
        public void Synergy_CheersOnlyTheFirstTime()
        {
            GameEvents.RaiseSynergy("duo_pesado", false);
            Assert.IsNull(voice.LastSpoken);

            GameEvents.RaiseSynergy("duo_pesado", true);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken);
        }

        [Test]
        public void SecondVoiceWithinCooldown_IsIgnored()
        {
            GameEvents.RaiseCheckpoint(1);
            now += 1f;
            GameEvents.RaiseSync(4, 3f);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken, "la risa espera el enfriamiento");
        }
    }
}
