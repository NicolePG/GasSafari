using System.Collections;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class FartFeedbackTests
    {
        private RaftTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            // Asegura los pools precalentados (el runner entra a Play sin recargar el dominio).
            foreach (VfxId id in System.Enum.GetValues(typeof(VfxId)))
                Services.Vfx.Play(id, Vector3.down * 50f, Quaternion.identity, Color.white).Stop();
            yield return new WaitForSeconds(1.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Destroy();
            yield return new WaitForSeconds(1.2f);
        }

        [Test]
        public void AudioDirector_LimitsSimultaneousFarts()
        {
            for (int i = 0; i < 6; i++)
                Services.Audio.Play(SoundId.FartPuff, Vector3.zero);
            Assert.LessOrEqual(Services.Audio.ActiveFarts, 4);
        }

        [Test]
        public void AudioDirector_SyncGainIsPlus3Db()
        {
            int normal = Services.Audio.Play(SoundId.FartMega, Vector3.zero);
            int boosted = Services.Audio.Play(SoundId.FartMega, Vector3.zero, 3f);
            Assert.AreEqual(Mathf.Pow(10f, 3f / 20f), Services.Audio.GetVolume(boosted) / Services.Audio.GetVolume(normal), 0.01f);
        }

        [UnityTest]
        public IEnumerator VfxPool_ReturnsEffectsWithoutGrowing()
        {
            var pool = Services.Vfx;
            int total = pool.CountAll(VfxId.FartPuff);
            int idle = pool.CountInactive(VfxId.FartPuff);

            pool.Play(VfxId.FartPuff, Vector3.zero, Quaternion.identity, Color.green);
            Assert.AreEqual(idle - 1, pool.CountInactive(VfxId.FartPuff));

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(idle, pool.CountInactive(VfxId.FartPuff));
            Assert.AreEqual(total, pool.CountAll(VfxId.FartPuff));
        }

        [UnityTest]
        public IEnumerator Avatar_PuffAndBlast_EmitVfxAndSound()
        {
            var input = rig.AddAvatar(180f);
            var pool = Services.Vfx;
            int idlePuff = pool.CountInactive(VfxId.FartPuff);
            int idleBlast = pool.CountInactive(VfxId.FartBlast);
            yield return new WaitForSeconds(0.2f);

            input.Command.FartPresses++;
            yield return null;
            Assert.AreEqual(idlePuff - 1, pool.CountInactive(VfxId.FartPuff), $"puff libres antes={idlePuff}");
            Assert.GreaterOrEqual(Services.Audio.ActiveFarts, 1);

            yield return new WaitForSeconds(0.5f);
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(idleBlast - 1, pool.CountInactive(VfxId.FartBlast),
                $"blast libres antes={idleBlast} total={pool.CountAll(VfxId.FartBlast)}");

            input.Command.FartHeld = false;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(idleBlast, pool.CountInactive(VfxId.FartBlast), "la ráfaga vuelve al pool al terminar");
        }
    }
}
