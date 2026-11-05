using System.Collections.Generic;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H04 Oleaje (docs/05): una cresta viaja por el cauce (transform.forward) y, al tocar la balsa, la levanta
    // (amplitud 0.6 m) y la empuja (empuje 20) en el punto de contacto: de lado la hace girar, de frente solo la frena.
    // Modo automático (interval > 0): una cresta cada 'interval' s; modo manual: SpawnCrest() (estela de H28).
    // La cresta se ve venir mientras recorre la zona; Warn() avisa al nacer (evento para HUD, Melman y bots).
    public class WaveHazard : HazardBase
    {
        private const float GRAVITY = 9.81f;
        private const float CREST_THICKNESS = 0.8f;
        private const float CREST_HEIGHT = 1.2f;

        [Tooltip("m de ancho de la cresta (ancho del cauce)")] [SerializeField] private float width = 10f;
        [Tooltip("m que recorre la cresta")] [SerializeField] private float length = 24f;
        [Tooltip("s entre crestas; 0 = solo manual")] [SerializeField] private float interval = 5f;
        [Tooltip("s de desfase")] [SerializeField] private float phase;
        [SerializeField] private Material crestMaterial;

        private readonly List<WaveCrest> crests = new List<WaveCrest>();
        private float clock;
        private int spawned;

        public float Amplitude => Config.Get("amplitude", 0.6f);
        public float Push => Config.Get("push", 20f);
        // m/s de la cresta (provisional: docs/05 no lo fija).
        public float CrestSpeed => Config.Get("crestSpeed", 4f) * HazardSpeed;
        public int ActiveCrests => crests.Count;

        public void Configure(float crestWidth, float crestLength, float seconds, Material material)
        {
            width = crestWidth;
            length = crestLength;
            interval = seconds;
            crestMaterial = material;
        }

        // Impulso vertical que sube la balsa 'amplitude' metros: m * sqrt(2 g h).
        public static float LiftImpulse(float mass, float amplitude)
        {
            return mass * Mathf.Sqrt(2f * GRAVITY * amplitude);
        }

        protected override void Update()
        {
            base.Update();
            clock += Time.deltaTime;
            if (interval > 0f && clock + phase >= interval * (spawned + 1))
                SpawnCrest();

            for (int i = crests.Count - 1; i >= 0; i--)
            {
                var crest = crests[i];
                crest.Travelled += CrestSpeed * Time.deltaTime;
                crest.transform.position = transform.position + transform.forward * crest.Travelled;
                if (crest.Travelled >= length)
                {
                    crests.RemoveAt(i);
                    Destroy(crest.gameObject);
                }
            }
        }

        public void SpawnCrest()
        {
            spawned++;
            var go = new GameObject("Crest");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.layer = gameObject.layer;

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, CREST_HEIGHT, CREST_THICKNESS);
            box.center = new Vector3(0f, CREST_HEIGHT * 0.5f - 0.2f, 0f);

            // Visual: cresta de agua baja y ancha.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(width, Amplitude, CREST_THICKNESS);
            visual.transform.localPosition = new Vector3(0f, Amplitude * 0.5f - 0.1f, 0f);
            if (crestMaterial != null)
                visual.GetComponent<MeshRenderer>().sharedMaterial = crestMaterial;
            visual.layer = gameObject.layer;

            var crest = go.AddComponent<WaveCrest>();
            crest.Owner = this;
            crests.Add(crest);
            Warn();
        }

        public void ClearCrests()
        {
            foreach (var crest in crests)
            {
                if (crest != null)
                    Destroy(crest.gameObject);
            }
            crests.Clear();
        }

        // La cresta tocó una balsa (una vez por cresta y balsa).
        public void Hit(RaftController raft, WaveCrest crest)
        {
            var body = raft.Body;
            var contact = body.worldCenterOfMass;
            var raftCollider = raft.GetComponentInChildren<Collider>();
            if (raftCollider != null)
                contact = raftCollider.ClosestPoint(crest.transform.position);

            // Empuja en el sentido de la ola, en el punto de contacto; levanta por el centro de masa (sin giro).
            var direction = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            PushRaft(raft, direction * Push, contact);
            PushRaft(raft, Vector3.up * LiftImpulse(body.mass, Amplitude), body.worldCenterOfMass);
            if (Services.IsReady)
                Services.Audio.Play(SoundId.WaterSplashBig, contact);
        }
    }

    // Cresta en movimiento de un WaveHazard.
    public class WaveCrest : MonoBehaviour
    {
        private readonly HashSet<RaftController> hit = new HashSet<RaftController>();

        public WaveHazard Owner { get; set; }
        public float Travelled { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (Owner == null || other.attachedRigidbody == null)
                return;
            if (other.attachedRigidbody.TryGetComponent<RaftController>(out var raft) && hit.Add(raft))
                Owner.Hit(raft, this);
        }
    }
}
