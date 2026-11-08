using System.Collections;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Agua dentro de la balsa e inclinación por peso (docs/02 §14): el piso queda sobre el agua con 4 animales
    // y el cabeceo se mantiene sutil.
    public class RaftWaterTiltTests
    {
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };
        private static readonly float[] CrewSeats = { 120f, 150f, 210f, 240f };

        // Altura máxima de la ola visual del shader de agua (_WaveHeight 0.08 x 1.5 octavas) más margen.
        public const float WAVE_CLEARANCE = 0.15f;
        public const float MAX_TILT_PER_ANIMAL = 3f;
        public const float MIN_TILT_PER_ANIMAL = 0.6f;
        public const float MAX_PITCH_FULL_CREW = 6f;

        private RaftTestRig rig;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            rig?.Destroy();
            rig = null;
        }

        private static float FloorTop(GameObject raft)
        {
            var floor = raft.transform.Find("Model/Raft_Floor").GetComponent<Renderer>();
            return floor.bounds.max.y;
        }

        // Punto más bajo de la cara superior del piso (el que se acerca al agua cuando la balsa se inclina).
        private static float FloorLowestRim(GameObject raft)
        {
            var floor = raft.transform.Find("Model/Raft_Floor");
            var bounds = floor.GetComponent<MeshFilter>().sharedMesh.bounds;
            float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.95f;
            float lowest = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                var local = new Vector3(bounds.center.x + Mathf.Cos(a) * radius, bounds.max.y, bounds.center.z + Mathf.Sin(a) * radius);
                lowest = Mathf.Min(lowest, floor.TransformPoint(local).y);
            }
            return lowest;
        }

        private static float Pitch(Transform t) => Mathf.Asin(Mathf.Clamp(t.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        [UnityTest, Timeout(60000)]
        public IEnumerator Telemetry_FloorAndTilt_WithOneToFourAnimals()
        {
            for (int count = 0; count <= 4; count++)
            {
                rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
                for (int i = 0; i < count; i++)
                    rig.AddAvatar(CrewSeats[i], Crew[i]);
                yield return new WaitForSeconds(4f);

                var raft = rig.Raft;
                var buoyancy = raft.GetComponent<RaftBuoyancy>();
                Debug.Log($"RAFTWATER n={count} pisoTop={FloorTop(raft):F3} pisoBajo={FloorLowestRim(raft):F3} posY={raft.transform.position.y:F3} incl={Vector3.Angle(raft.transform.up, Vector3.up):F2} cabeceo={Pitch(raft.transform):F2} prof0={buoyancy.GetDepth(0):F3}");
                rig.Destroy();
                rig = null;
            }
        }

        // Un animal en cada asiento posible: inclinación por peso de 1 a 3 grados.
        [UnityTest, Timeout(120000)]
        public IEnumerator TiltPerAnimal_IsSubtle()
        {
            for (int i = 0; i < Crew.Length; i++)
            {
                foreach (float seat in new[] { 90f, 180f, 270f })
                {
                    rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
                    rig.AddAvatar(seat, Crew[i]);
                    yield return new WaitForSeconds(4f);
                    float tilt = Vector3.Angle(rig.Raft.transform.up, Vector3.up);
                    Debug.Log($"RAFTTILT {Crew[i]} asiento={seat} incl={tilt:F2}");
                    Assert.LessOrEqual(tilt, MAX_TILT_PER_ANIMAL, $"{Crew[i]} en {seat}°: {tilt:F2}°");
                    Assert.GreaterOrEqual(tilt, MIN_TILT_PER_ANIMAL, $"{Crew[i]} en {seat}°: {tilt:F2}° (la inclinación por peso debe notarse)");
                    rig.Destroy();
                    rig = null;
                }
            }
        }

        // 4 animales a bordo: el piso queda por encima del agua, también inclinado, y se ve el piso.
        [UnityTest, Timeout(60000)]
        public IEnumerator FullCrew_FloorStaysAboveWater()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, 0f));
            for (int i = 0; i < Crew.Length; i++)
                rig.AddAvatar(CrewSeats[i], Crew[i]);
            yield return new WaitForSeconds(4f);

            float waterY = rig.Water.transform.position.y;
            float lowest = FloorLowestRim(rig.Raft);
            Assert.GreaterOrEqual(lowest, waterY + WAVE_CLEARANCE, $"piso más bajo {lowest:F3} m, agua {waterY:F3} m");
        }

        // 4 animales empujando a fondo desde la popa: cabeceo máximo de 6 grados y la proa no se hunde.
        [UnityTest, Timeout(120000)]
        public IEnumerator FullCrewBlast_PitchStaysUnderLimit()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.3f, -450f));
            rig.Water.transform.localScale = new Vector3(20f, 1f, 100f);
            var sources = new ScriptedCommandSource[Crew.Length];
            for (int i = 0; i < Crew.Length; i++)
                sources[i] = rig.AddAvatar(CrewSeats[i] + 15f, Crew[i]);
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < sources.Length; i++)
                sources[i].Command.FartHeld = true;

            float maxPitch = 0f;
            float minBow = float.MaxValue;
            float t = 0f;
            while (t < 8f)
            {
                maxPitch = Mathf.Max(maxPitch, Mathf.Abs(Pitch(rig.Raft.transform)));
                minBow = Mathf.Min(minBow, FloorLowestRim(rig.Raft) - rig.Water.transform.position.y);
                t += Time.deltaTime;
                yield return null;
            }
            Debug.Log($"RAFTPITCH cabeceoMax={maxPitch:F2} pisoMinSobreAgua={minBow:F3} vel={rig.Raft.GetComponent<Rigidbody>().velocity.magnitude:F1}");
            Assert.LessOrEqual(maxPitch, MAX_PITCH_FULL_CREW, $"cabeceo {maxPitch:F2}°");
            Assert.Greater(minBow, 0f, "la proa no se mete bajo el agua");
        }
    }
}
