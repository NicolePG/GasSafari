using System.Collections;
using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Cuándo habla cada personaje (docs/08 §2 y §4, provisional). El enfriamiento de 3 s se prueba en VoiceGateTests.
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

        [UnityTest]
        public IEnumerator OwnPuff_MakesEffort()
        {
            yield return new WaitForSeconds(0.3f);
            avatar.GetComponent<ScriptedCommandSource>().Command.FartPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(VoiceKind.Effort, voice.LastSpoken, "lanzar un pedo");
        }

        [Test]
        public void OwnFall_MakesTheFallVoice_EvenRightAfterAnotherVoice()
        {
            GameEvents.RaiseSuperFart(1f);
            GameEvents.RaisePlayerFell(avatar);
            Assert.AreEqual(VoiceKind.Fall, voice.LastSpoken, "caer siempre suena");
        }

        [Test]
        public void RaftSinking_MakesTheFallVoice()
        {
            GameEvents.RaiseCheckpoint(1);
            GameEvents.RaiseRaftSunk();
            Assert.AreEqual(VoiceKind.Fall, voice.LastSpoken, "hundirse corta lo que sonaba");
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
        public void AnotherPlayersFailedFart_Laughs_OwnDoesNot()
        {
            GameEvents.RaiseFartFailed(avatar);
            Assert.IsNull(voice.LastSpoken, "no se ríe de su propia tos");
            GameEvents.RaiseFartFailed(other);
            Assert.AreEqual(VoiceKind.Laugh, voice.LastSpoken);
        }

        [Test]
        public void Sync_Cheers_EvenRightAfterTheEffortOfThePuff()
        {
            GameEvents.RaiseSuperFart(1f);
            Assert.AreEqual(VoiceKind.Effort, voice.LastSpoken);
            GameEvents.RaiseSync(2, 1.5f);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken, "el festejo corta el esfuerzo");
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
        public void AchievementAndSynergy_NoLongerCheer()
        {
            GameEvents.RaiseAchievementUnlocked("first_sync");
            GameEvents.RaiseSynergy("duo_pesado", true);
            Assert.IsNull(voice.LastSpoken);
        }

        [Test]
        public void SecondVoiceWithinCooldown_IsIgnored()
        {
            GameEvents.RaiseCheckpoint(1);
            now += 1f;
            CharacterVoice.Random01 = () => 0f;
            GameEvents.RaisePlayerFell(other);
            Assert.AreEqual(VoiceKind.Cheer, voice.LastSpoken, "la risa espera el enfriamiento");
        }
    }
}
