using System.Collections;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class RaftStillWaterTests
    {
        private const float DURATION = 60f;
        private const float MAX_DRIFT = 0.5f;

        // T-RAFT-01 (docs/11 §2): balsa sin jugadores en agua quieta 60 s: no vuelca, deriva < 0.5 m.
        [UnityTest, Timeout(120000)]
        public IEnumerator TRaft01_StillWater60s_NoCapsizeAndSmallDrift()
        {
            var rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            var monitor = rig.Get<RaftStateMonitor>();
            yield return new WaitForSeconds(2f);
            var start = rig.Raft.transform.position;
            float maxTilt = 0f;

            float elapsed = 0f;
            while (elapsed < DURATION)
            {
                maxTilt = Mathf.Max(maxTilt, monitor.Tilt);
                elapsed += Time.deltaTime;
                yield return null;
            }

            var drift = rig.Raft.transform.position - start;
            drift.y = 0f;
            rig.Destroy();

            Assert.IsFalse(monitor.HasCapsized);
            Assert.Less(maxTilt, rig.Balance.capsizeAngle);
            Assert.Less(drift.magnitude, MAX_DRIFT, $"deriva {drift.magnitude:F3} m, inclinación máx. {maxTilt:F2}°");
        }
    }
}
