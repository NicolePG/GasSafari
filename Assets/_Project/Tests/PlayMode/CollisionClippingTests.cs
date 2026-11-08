using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Auditoría de "nada se traspasa" (docs/11): la balsa no atraviesa paredes finas a ninguna velocidad y los animales
    // no se meten en la balsa ni entre ellos, tampoco en las animaciones de celebración.
    public class CollisionClippingTests
    {
        private static readonly string[][] Crews =
        {
            new[] { "Gloria", "Alex", "Melman", "Marty" },
            new[] { "Julien", "Gloria", "Marty", "Alex" },
        };

        // Los cuatro asientos más juntos que permite minSeatSeparation (45°), en la popa.
        private static readonly float[] TightSeats = { 135f, 180f, 225f, 270f };

        // Las animaciones de celebración deben dejar al menos esto entre huesos de animales distintos (m).
        private const float MIN_BONE_DISTANCE = 0.08f;
        // Hueso por debajo de la cara superior del piso: el animal se metió en la balsa (m).
        private const float FLOOR_TOLERANCE = 0.12f;

        private RaftTestRig rig;
        private GameObject wall;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (wall != null)
                Object.Destroy(wall);
            rig?.Destroy();
            rig = null;
        }

        [Test]
        public void RaftBody_IsContinuousDynamic()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            Assert.AreEqual(CollisionDetectionMode.ContinuousDynamic, rig.Get<Rigidbody>().collisionDetectionMode);
        }

        // Una pared del grosor mínimo de diseño (0.3 m) no se atraviesa ni a 30 m/s (muy por encima de la velocidad máxima).
        [UnityTest, Timeout(60000)]
        public IEnumerator RaftDoesNotTunnelThroughMinimumWall([Values(12f, 30f)] float speed)
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(0f, 1f, 40f);
            wall.transform.localScale = new Vector3(20f, 3f, 0.3f);
            yield return new WaitForSeconds(0.3f);

            var body = rig.Get<Rigidbody>();
            body.velocity = new Vector3(0f, 0f, speed);
            float furthest = 0f;
            float end = Time.time + 4f;
            while (Time.time < end)
            {
                furthest = Mathf.Max(furthest, rig.Raft.transform.position.z);
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(furthest, 40f, $"a {speed} m/s la balsa no cruza la pared (llegó a z={furthest:F2})");
        }

        // Asientos a 45° con los 5 animales: cada par queda más lejos que la suma de sus radios.
        [Test]
        public void SeatsAtMinimumSeparation_LeaveRoomBetweenAnimals()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            float radius = rig.Get<RaftSeats>().LocalSeatPosition(0f).magnitude;
            float apart = 2f * radius * Mathf.Sin(rig.Balance.minSeatSeparation * 0.5f * Mathf.Deg2Rad);
            var radii = new List<(string, float)>();
            foreach (var name in new[] { "Alex", "Marty", "Melman", "Gloria", "Julien" })
            {
                var avatar = rig.AddAvatar(180f, name);
                radii.Add((name, CapsuleRadius(avatar.gameObject)));
                rig.Get<RaftSeats>().Seats[0].Leave();
            }
            for (int i = 0; i < radii.Count; i++)
            {
                for (int j = i + 1; j < radii.Count; j++)
                    Assert.LessOrEqual(radii[i].Item2 + radii[j].Item2, apart + 0.05f, $"{radii[i].Item1} y {radii[j].Item1} caben a {apart:F2} m");
            }
        }

        private static float CapsuleRadius(GameObject go)
        {
            var capsule = go.GetComponent<CapsuleCollider>();
            var scale = go.transform.lossyScale;
            return capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        // Celebración, golpe y aguante con los asientos más juntos: ningún hueso entra en otro animal ni bajo el piso.
        [UnityTest, Timeout(120000)]
        public IEnumerator Animations_DoNotClipIntoNeighboursOrRaft([Values(0, 1)] int crewIndex)
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            var crew = Crews[crewIndex];
            var avatars = new List<GameObject>();
            for (int i = 0; i < crew.Length; i++)
                avatars.Add(rig.AddAvatar(TightSeats[i], crew[i]).gameObject);
            yield return new WaitForSeconds(1f);

            var bones = new List<Transform>[avatars.Count];
            for (int i = 0; i < avatars.Count; i++)
            {
                bones[i] = new List<Transform>();
                foreach (var skinned in avatars[i].GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    foreach (var bone in skinned.bones)
                    {
                        if (bone != null && !bones[i].Contains(bone))
                            bones[i].Add(bone);
                    }
                }
            }

            foreach (var avatar in avatars)
                avatar.GetComponent<CharacterAnimator>().PlayCelebrate();

            var floor = rig.Raft.transform.Find("Model/Raft_Floor");
            float floorTop = floor.TransformPoint(new Vector3(0f, floor.GetComponent<MeshFilter>().sharedMesh.bounds.max.y, 0f)).y;
            float minBone = float.MaxValue;
            string minPair = "";
            float lowest = float.MaxValue;
            string lowestBone = "";
            float end = Time.time + 3.5f;
            while (Time.time < end)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    for (int j = i + 1; j < bones.Length; j++)
                    {
                        foreach (var a in bones[i])
                        {
                            foreach (var b in bones[j])
                            {
                                float d = Vector3.Distance(a.position, b.position);
                                if (d < minBone)
                                {
                                    minBone = d;
                                    minPair = $"{crew[i]}/{a.name} - {crew[j]}/{b.name}";
                                }
                            }
                        }
                    }
                    foreach (var bone in bones[i])
                    {
                        // Solo huesos sobre la huella de la balsa: los pies descansan sobre el borde.
                        var local = rig.Raft.transform.InverseTransformPoint(bone.position);
                        if (new Vector2(local.x, local.z).magnitude < rig.Balance.raftRadius - 0.4f && bone.position.y < lowest)
                        {
                            lowest = bone.position.y;
                            lowestBone = $"{crew[i]}/{bone.name}";
                        }
                    }
                }
                yield return null;
            }

            Debug.Log($"CLIPAUDIT tripulación {string.Join(",", crew)}: hueso-hueso mínimo {minBone:F2} m ({minPair}); hueso más bajo sobre la balsa {lowest - floorTop:F2} m respecto del piso ({lowestBone})");
            Assert.GreaterOrEqual(minBone, MIN_BONE_DISTANCE, $"huesos de animales distintos demasiado juntos: {minPair}");
            Assert.GreaterOrEqual(lowest - floorTop, -FLOOR_TOLERANCE, $"un hueso entra en el piso de la balsa: {lowestBone}");
        }
    }
}
