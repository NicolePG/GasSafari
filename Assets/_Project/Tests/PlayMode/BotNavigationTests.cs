using System.Collections;
using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M4-01: percepción y navegación de los bots sobre la balsa.
    public class BotNavigationTests
    {
        private const string AVATAR_PREFAB = "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab";

        private RaftTestRig rig;
        private GameObject flowObject;
        private readonly List<GameObject> bots = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var bot in bots)
                Object.Destroy(bot);
            bots.Clear();
            rig.Destroy();
            if (flowObject != null)
                Object.Destroy(flowObject);
            yield return null;
        }

        private BotBrain AddBot(float angle)
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(AVATAR_PREFAB));
#endif
            bots.Add(go);
            go.GetComponent<PlayerSeat>().Sit(rig.Get<RaftSeats>(), angle);
            var brain = go.AddComponent<BotBrain>();
            brain.Attach(new BotCommandSource(), null);
            return brain;
        }

        // Cauce recto de 40 m en +Z que gira a la derecha hacia +X.
        private FlowSpline BuildCurvedFlow(float acceleration)
        {
            flowObject = new GameObject("TestFlow");
            var container = flowObject.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var point in new[] { new float3(0, 0.2f, 0), new float3(0, 0.2f, 40), new float3(30, 0.2f, 70), new float3(70, 0.2f, 70) })
                spline.Add(new BezierKnot(point), TangentMode.AutoSmooth);
            var flow = flowObject.AddComponent<FlowSpline>();
            flow.Configure(12f, AnimationCurve.Constant(0f, 1f, acceleration));
            rig.Get<RaftFlowReceiver>().SetFlow(flow);
            return flow;
        }

        [UnityTest]
        public IEnumerator SingleBot_PushesTheRaftForward()
        {
            var start = rig.Raft.transform.position;
            AddBot(180f);
            yield return new WaitForSeconds(8f);

            var moved = rig.Raft.transform.position - start;
            Assert.Greater(moved.z, 12f, $"avanza (desplazamiento={moved})");
            Assert.Less(Mathf.Abs(moved.x), moved.z * 0.3f, $"sin irse de lado (desplazamiento={moved})");
        }

        [UnityTest]
        public IEnumerator ThreeBots_ShareTheRearSeats()
        {
            AddBot(120f);
            AddBot(180f);
            AddBot(240f);
            float maxTilt = 0f;
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForSeconds(0.1f);
                maxTilt = Mathf.Max(maxTilt, Vector3.Angle(rig.Raft.transform.up, Vector3.up));
            }
            Assert.Less(maxTilt, 55f, "la balsa no vuelca (capsizeAngle 70)");

            var seats = rig.Get<RaftSeats>().Seats;
            Assert.AreEqual(3, seats.Count);
            float min = rig.Balance.minSeatSeparation;
            for (int i = 0; i < seats.Count; i++)
            {
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(seats[i].Angle, 180f)), 110f, $"asiento {i} en la mitad trasera ({seats[i].Angle})");
                for (int j = i + 1; j < seats.Count; j++)
                    Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(seats[i].Angle, seats[j].Angle)), min - 1f, $"asientos {i} y {j} separados");
            }
        }

        [UnityTest]
        public IEnumerator ThreeBots_FollowACurvedFlowToTheEnd()
        {
            var flow = BuildCurvedFlow(1.5f);
            AddBot(120f);
            AddBot(180f);
            AddBot(240f);

            var raft = rig.Raft.transform;
            int hint = 0;
            float progress = 0f;
            float time = 0f;
            float worstOffset = 0f;
            float maxTilt = 0f;
            while (progress < 0.95f && time < 60f)
            {
                yield return new WaitForSeconds(0.5f);
                time += 0.5f;
                progress = flow.GetProgress(raft.position, ref hint);
                flow.GetFlow(raft.position, ref hint, out _, out _);
                worstOffset = Mathf.Max(worstOffset, DistanceToPath(flow, raft.position, ref hint));
                maxTilt = Mathf.Max(maxTilt, Vector3.Angle(raft.up, Vector3.up));
            }

            Assert.GreaterOrEqual(progress, 0.95f, $"llegó al final del cauce (progreso {progress:F2} en {time:F0} s)");
            Assert.Less(worstOffset, 9f, "nunca se alejó del cauce más de 9 m");
            Assert.Less(maxTilt, 55f, "la balsa no vuelca (capsizeAngle 70)");
        }

        private static float DistanceToPath(FlowSpline flow, Vector3 position, ref int hint)
        {
            var ahead = flow.GetPointAhead(position, ref hint, 0f);
            var offset = position - ahead;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
