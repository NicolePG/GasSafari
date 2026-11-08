using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using GasSafari.UI;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Derrumbe que persigue (docs/04 W4-5): al entrar la balsa en la zona de salida el techo avisa durante 'warningSeconds':
    // sacudidas de cámara, un estruendo, un borde rojo que late en la pantalla y rocas que caen detrás de la balsa (sin daño).
    // Después avanza por 'path' a 'speed' m/s; si alcanza la balsa (a menos de 'catchDistance' m por detrás), reaparición.
    [RequireComponent(typeof(BoxCollider))]
    public class CollapseChaser : MonoBehaviour
    {
        private const float DEBRIS_HEIGHT = 12f;
        private const float DEBRIS_FALL_SECONDS = 0.8f;
        private const float DEBRIS_SPACING = 0.28f;

        [SerializeField] private Transform rubble;
        [SerializeField] private Vector3[] path;
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float catchDistance = 4f;
        [SerializeField] private float warningSeconds = 2.5f;
        [Tooltip("Rocas que caen detrás de la balsa durante el aviso (se reciclan, sin collider)")]
        [SerializeField] private Transform[] debris;

        private RaftController raft;
        private int index;
        private bool warning;
        private bool running;
        private float warningTimer;
        private float nextShake;
        private float debrisElapsed = -1f;
        private float[] debrisTime;
        private Vector3[] debrisTarget;
        private bool[] debrisLanded;

        public bool Running => running;
        public bool Warning => warning;
        public int DebrisLanded { get; private set; }

        public void Configure(Transform wall, Vector3[] waypoints, float metersPerSecond)
        {
            rubble = wall;
            path = waypoints;
            speed = metersPerSecond;
        }

        public void ConfigureWarning(Transform[] fallingRocks, float seconds)
        {
            debris = fallingRocks;
            warningSeconds = seconds;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Awake() => HideDebris();

        private void Reset()
        {
            running = false;
            warning = false;
            debrisElapsed = -1f;
            index = 0;
            if (rubble != null && path != null && path.Length > 0)
                rubble.position = path[0];
            ScreenEdgeWarning.Hide();
            HideDebris();
        }

        private void HideDebris()
        {
            if (debris == null)
                return;
            foreach (var rock in debris)
            {
                if (rock != null)
                    rock.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var hit = other.GetComponentInParent<RaftController>();
            if (hit == null || running || warning)
                return;
            raft = hit;
            warning = true;
            warningTimer = warningSeconds;
            nextShake = 0f;
            DebrisLanded = 0;
            debrisElapsed = 0f;
            PlanDebris();
            ScreenEdgeWarning.Show(new Color(0.9f, 0.05f, 0.05f), warningSeconds + 1f);
            Play(SoundId.RockWarn, raft.transform.position);
        }

        // Cada roca cae en un punto entre el derrumbe y la balsa (detrás de ella), una tras otra.
        private void PlanDebris()
        {
            int n = debris != null ? debris.Length : 0;
            debrisTime = new float[n];
            debrisTarget = new Vector3[n];
            debrisLanded = new bool[n];
            var back = rubble != null ? rubble.position - raft.transform.position : -raft.transform.forward;
            back.y = 0f;
            back = back.sqrMagnitude > 0.01f ? back.normalized : -raft.transform.forward;
            var side = Vector3.Cross(Vector3.up, back);
            for (int i = 0; i < n; i++)
            {
                debrisTime[i] = i * DEBRIS_SPACING;
                float behind = 6f + (i * 7.3f) % 16f;
                float lateral = ((i * 5.1f) % 7f) - 3.5f;
                debrisTarget[i] = raft.transform.position + back * behind + side * lateral;
                debrisTarget[i].y = raft.transform.position.y;
                debris[i].gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateDebris();
            if (warning)
            {
                UpdateWarning();
                return;
            }
            if (!running || rubble == null || path == null || index >= path.Length - 1)
                return;
            var target = path[index + 1];
            rubble.position = Vector3.MoveTowards(rubble.position, target, speed * Time.deltaTime);
            if ((rubble.position - target).sqrMagnitude < 0.01f)
                index++;
            if (raft != null && Vector3.Distance(rubble.position, raft.transform.position) <= catchDistance)
            {
                running = false;
                var checkpoints = FindFirstObjectByType<CheckpointSystem>();
                if (checkpoints != null)
                    checkpoints.RequestRespawn();
            }
        }

        // Rocas que caen: aparecen en lo alto, bajan con aceleración y suenan al tocar el agua. Terminan de caer aunque el
        // aviso ya haya acabado.
        private void UpdateDebris()
        {
            if (debrisElapsed < 0f || debris == null)
                return;
            debrisElapsed += Time.deltaTime;
            bool pending = false;
            for (int i = 0; i < debris.Length; i++)
            {
                if (debris[i] == null || debrisLanded[i])
                    continue;
                float t = (debrisElapsed - debrisTime[i]) / DEBRIS_FALL_SECONDS;
                pending = true;
                if (t < 0f)
                    continue;
                if (t >= 1f)
                {
                    debrisLanded[i] = true;
                    debris[i].gameObject.SetActive(false);
                    DebrisLanded++;
                    Play(SoundId.RockImpact, debrisTarget[i]);
                    continue;
                }
                debris[i].gameObject.SetActive(true);
                debris[i].position = debrisTarget[i] + Vector3.up * (DEBRIS_HEIGHT * (1f - t * t));
            }
            if (!pending)
                debrisElapsed = -1f;
        }

        private void UpdateWarning()
        {
            warningTimer -= Time.deltaTime;
            float elapsed = warningSeconds - warningTimer;

            // Sacudidas: una fuerte al empezar y más suaves cada 0.7 s.
            if (elapsed >= nextShake)
            {
                var rig = FindFirstObjectByType<GasSafari.VFX.RaftCameraRig>();
                if (rig != null)
                    rig.Shake(nextShake == 0f ? 1f : 0.6f);
                nextShake += 0.7f;
            }

            if (warningTimer <= 0f)
            {
                warning = false;
                running = true;
            }
        }

        private static void Play(SoundId id, Vector3 position)
        {
            if (Services.IsReady && Services.Audio != null)
                Services.Audio.Play(id, position);
        }
    }
}
