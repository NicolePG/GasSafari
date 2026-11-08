using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H13 Tronco giratorio (docs/05): un tronco que gira en el agua a 90°/s alrededor de su centro y golpea con 25 de
    // impulso. Se pasa por el hueco entre vueltas. Si está podrido lleva la etiqueta Fragile (el Rugido lo rompe).
    [RequireComponent(typeof(Rigidbody))]
    public class SpinningLog : HazardBase, IBreakable
    {
        private Rigidbody body;
        private float angle;
        private float startAngle;

        public float DegreesPerSecond => Config.Get("spin", 90f);
        public float Hit => Config.Get("hit", 25f);
        public bool Broken { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            startAngle = transform.eulerAngles.y;
            angle = startAngle;
        }

        private void FixedUpdate()
        {
            if (Broken)
                return;
            angle += DegreesPerSecond * HazardSpeed * Time.fixedDeltaTime;
            body.MoveRotation(Quaternion.Euler(0f, angle, 0f));
        }

        // Golpe: empuja la balsa en el sentido en que se mueve el punto del tronco que la toca.
        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (Broken || collision.contactCount == 0)
                return;
            var point = collision.GetContact(0).point;
            var radial = point - transform.position;
            radial.y = 0f;
            var tangent = Vector3.Cross(Vector3.up, radial).normalized * Mathf.Sign(DegreesPerSecond);
            PushRaft(raft, tangent * Hit, point);
        }

        // Rugido sobre un tronco podrido.
        public void Break(Vector3 source)
        {
            if (Broken)
                return;
            Broken = true;
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        private void OnEnable() => Core.GameEvents.OnRespawn += Restore;
        private void OnDisable() => Core.GameEvents.OnRespawn -= Restore;

        private void Restore()
        {
            Broken = false;
            angle = startAngle;
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = true;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }
    }
}
