using System.Collections;
using System.IO;
using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Vuelco a velocidad (M4-04): 4 animales con ráfagas sincronizadas desde la popa en una recta larga.
    public class RaftHighSpeedTests
    {
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };
        private static readonly float[] RearSeats = { 135f, 165f, 195f, 225f };
        private const float RUN_TIME = 14f;
        private const float TARGET_SPEED = 10f;

        private RaftTestRig rig;
        private int capsizes;

        public static string TelemetryFolder => Path.Combine(Application.temporaryCachePath, "RaftTelemetry");

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameEvents.OnRaftCapsized -= CountCapsize;
            rig?.Destroy();
            rig = null;
        }

        private void CountCapsize() => capsizes++;

        // Medición: deja un CSV por escenario en TelemetryFolder (sin aserciones).
        [UnityTest, Category("Telemetry"), Explicit("medición; correr a mano")]
        public IEnumerator Telemetry_StraightSyncedBlasts([Values(0, 1, 2)] int scenario)
        {
            var telemetry = new RunResult();
            yield return Run(scenario, scenario == 2 ? 1.5f : 0f, telemetry, true);
            Debug.Log($"RAFTTEL recta escenario={scenario} vMax={telemetry.MaxSpeed:F1} inclMax={telemetry.MaxTilt:F0} vuelcos={telemetry.Capsizes} csv={telemetry.CsvPath}");
        }

        // docs/11 T-RAFT-05: a 10 m/s en recta no vuelca en 20 corridas (asientos, puntería, ritmo y corriente al azar).
        [UnityTest, Timeout(900000)]
        public IEnumerator TRaft05_StraightAt10ms_NoCapsizeIn20Runs()
        {
            Time.timeScale = 3f;
            int capsized = 0;
            float slowest = float.MaxValue;
            float worstTilt = 0f;
            for (int run = 0; run < 20; run++)
            {
                var result = new RunResult();
                float current = (run % 4) * 0.75f;
                yield return Run(100 + run, current, result, false);
                capsized += result.Capsizes;
                slowest = Mathf.Min(slowest, result.MaxSpeed);
                worstTilt = Mathf.Max(worstTilt, result.MaxTilt);
                Debug.Log($"RAFTTEL corrida={run} corriente={current:F2} vMax={result.MaxSpeed:F1} inclMax={result.MaxTilt:F0} vuelcos={result.Capsizes}");
            }
            Time.timeScale = 1f;

            Assert.AreEqual(0, capsized, "ninguna corrida vuelca");
            Assert.GreaterOrEqual(slowest, TARGET_SPEED, "todas llegan a 10 m/s");
            Assert.Less(worstTilt, 45f);
        }

        private class RunResult
        {
            public float MaxSpeed;
            public float MaxTilt;
            public int Capsizes;
            public string CsvPath;
        }

        // seed 0-2: escenarios fijos de medición; >= 100: variaciones al azar.
        private IEnumerator Run(int seed, float current, RunResult result, bool writeCsv)
        {
            var random = new System.Random(seed);
            rig = new RaftTestRig(new Vector3(0f, 0.2f, -450f));
            rig.Water.transform.localScale = new Vector3(20f, 1f, 100f);
            capsizes = 0;
            GameEvents.OnRaftCapsized += CountCapsize;

            var telemetry = rig.Raft.AddComponent<RaftTelemetry>();
            var sources = new ScriptedCommandSource[Crew.Length];
            var aims = new float[Crew.Length];
            for (int i = 0; i < Crew.Length; i++)
            {
                float jitter = seed >= 100 ? (float)(random.NextDouble() * 20.0 - 10.0) : 0f;
                sources[i] = rig.AddAvatar(RearSeats[i] + jitter, Crew[i]);
                aims[i] = seed >= 100 ? (float)(random.NextDouble() * 0.6 - 0.3) : 0f;
            }
            if (current > 0f)
                rig.Get<RaftFlowReceiver>().Wind = new Vector3(seed % 2 == 0 ? current : -current, 0f, current);

            yield return new WaitForSeconds(0.5f);
            telemetry.Clear();

            // Escenario 1 y aleatorios: ráfagas largas con pausas y giros; 0 y 2: ráfaga continua mientras haya gas.
            float t = 0f;
            float nextToggle = 0f;
            bool holding = false;
            float turnUntil = -1f;
            float turn = 0f;
            while (t < RUN_TIME)
            {
                if (t >= nextToggle)
                {
                    holding = !holding;
                    bool pulsed = seed == 1 || seed >= 100;
                    nextToggle = t + (holding ? (pulsed ? 2.5f : 4f) : (pulsed ? 0.6f : 1.5f));
                    for (int i = 0; i < sources.Length; i++)
                        sources[i].Command.FartHeld = holding;
                }
                if (seed >= 100 && t > turnUntil && random.NextDouble() < 0.01)
                {
                    turn = random.NextDouble() < 0.5 ? -1f : 1f;
                    turnUntil = t + 1f;
                }
                float steer = t < turnUntil ? turn : 0f;
                for (int i = 0; i < sources.Length; i++)
                    sources[i].Command.Aim = Mathf.Clamp(aims[i] + steer, -1f, 1f);

                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }

            result.MaxSpeed = telemetry.MaxSpeed;
            result.MaxTilt = telemetry.MaxTilt;
            result.Capsizes = capsizes;
            if (writeCsv)
            {
                result.CsvPath = Path.Combine(TelemetryFolder, $"recta_{seed}.csv");
                telemetry.WriteCsv(result.CsvPath);
            }

            GameEvents.OnRaftCapsized -= CountCapsize;
            rig.Destroy();
            rig = null;
            yield return null;
        }
    }
}
