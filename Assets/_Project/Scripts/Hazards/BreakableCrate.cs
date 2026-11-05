using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H07 Caja rompible (docs/05): frena a la balsa y se rompe con el Rugido (IBreakable, etiqueta Fragile) o con un
    // impacto mayor a 20 (impulso del choque en N·s, misma unidad que los golpes de docs/05).
    public class BreakableCrate : HazardBase, IBreakable
    {
        private const int DEBRIS = 6;
        private const float DEBRIS_LIFETIME = 2.5f;

        public float ImpactToBreak => Config.Get("impactToBreak", 20f);
        public bool Broken { get; private set; }

        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (!Broken && collision.impulse.magnitude > ImpactToBreak)
                Break(raft.transform.position);
        }

        protected override void OnRaftCollisionStay(RaftController raft, Collision collision)
        {
            OnRaftCollision(raft, collision);
        }

        public void Break(Vector3 source)
        {
            if (Broken)
                return;
            Broken = true;
            SpawnDebris(source);
            if (Services.IsReady)
                Services.Audio.Play(SoundId.CrateBreak, transform.position);
            gameObject.SetActive(false);
        }

        // Reaparición en un checkpoint: la caja vuelve.
        public void Restore()
        {
            Broken = false;
            gameObject.SetActive(true);
        }

        private void SpawnDebris(Vector3 source)
        {
            var renderer = GetComponentInChildren<MeshRenderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            for (int i = 0; i < DEBRIS; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "CrateDebris";
                piece.layer = GameLayers.Ghost; // no estorba a la balsa
                piece.transform.position = transform.position + Random.insideUnitSphere * 0.3f + Vector3.up * 0.4f;
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = Vector3.one * Random.Range(0.18f, 0.3f);
                if (material != null)
                    piece.GetComponent<MeshRenderer>().sharedMaterial = material;
                var body = piece.AddComponent<Rigidbody>();
                body.AddExplosionForce(4f, source, 3f, 0.5f, ForceMode.Impulse);
                Destroy(piece, DEBRIS_LIFETIME);
            }
        }
    }
}
