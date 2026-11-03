using System.Collections;
using GasSafari.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class FartJuiceTests
    {
        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private Transform hips;
        private Transform head;
        private FartJuiceConfig config;
        private Vector3 hipsScale;
        private Vector3 hipsPosition;
        private Vector3 headScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            hips = input.transform.Find("Visual");
            head = input.transform.Find("Head");
#if UNITY_EDITOR
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<FartJuiceConfig>("Assets/_Project/Data/Balance/FartJuice.asset");
#endif
            hipsScale = hips.localScale;
            hipsPosition = hips.localPosition;
            headScale = head.localScale;
            yield return new WaitForSeconds(0.2f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Puff_SquashesThenRestores()
        {
            input.Command.FartPresses++;
            float minScale = float.MaxValue;
            float maxScale = float.MinValue;
            float t = 0f;
            while (t < config.puffDuration + 0.1f)
            {
                yield return null;
                t += Time.deltaTime;
                float ratio = hips.localScale.y / hipsScale.y;
                minScale = Mathf.Min(minScale, ratio);
                maxScale = Mathf.Max(maxScale, ratio);
            }
            Assert.Less(minScale, 0.95f, "se aplasta");
            Assert.Greater(maxScale, 1.03f, "y se estira");
            Assert.AreEqual(hipsScale.y, hips.localScale.y, 1e-4f, "vuelve a la escala base");
        }

        [UnityTest]
        public IEnumerator Charge_InflatesHead()
        {
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(1f);
            Assert.Greater(head.localScale.x, headScale.x * 1.05f);

            input.Command.HoldHeld = false;
            yield return null;
            yield return null;
            Assert.AreEqual(headScale.x, head.localScale.x, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Blast_VibratesWithinAmplitude()
        {
            input.Command.FartHeld = true;
            yield return new WaitForSeconds(0.3f);
            float maxOffset = 0f;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                maxOffset = Mathf.Max(maxOffset, (hips.localPosition - hipsPosition).magnitude);
            }
            Assert.Greater(maxOffset, config.blastAmplitude * 0.2f);
            Assert.LessOrEqual(maxOffset, config.blastAmplitude + 1e-4f);
        }

        [UnityTest]
        public IEnumerator ChargedFart_RecoilsForward()
        {
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(0.8f);
            input.Command.FartPresses++;
            float maxForward = 0f;
            float t = 0f;
            while (t < config.megaDuration + 0.1f)
            {
                yield return null;
                t += Time.deltaTime;
                maxForward = Mathf.Max(maxForward, hips.localPosition.z - hipsPosition.z);
            }
            Assert.Greater(maxForward, config.megaRecoil * 0.5f);
            Assert.LessOrEqual(maxForward, config.megaRecoil + 1e-4f);
        }
    }
}
