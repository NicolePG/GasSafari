using System;
using GasSafari.Characters;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Efecto temporal del Cuello Gancho: la balsa pivota alrededor del ancla con distancia fija
    // hasta maxTime o hasta soltar el botón. Al soltar conserva la velocidad.
    public class HookTether : MonoBehaviour
    {
        private ConfigurableJoint joint;
        private Rigidbody body;
        private PlayerAvatar owner;
        private float maxTime;
        private float elapsed;

        public Anchor Anchor { get; private set; }
        public PlayerAvatar Owner => owner;
        // Honda de la Selva (sinergia) la sube al soltar.
        public float ReleaseSpeedMultiplier { get; set; } = 1f;

        public static event Action<HookTether> Attached;
        public static event Action<HookTether> Released;

        public static HookTether Attach(RaftController raft, Anchor anchor, PlayerAvatar owner, float maxTime)
        {
            var tether = raft.gameObject.AddComponent<HookTether>();
            tether.body = raft.Body;
            tether.owner = owner;
            tether.maxTime = maxTime;
            tether.Anchor = anchor;
            tether.CreateJoint(anchor.transform.position);
            Attached?.Invoke(tether);
            return tether;
        }

        private void CreateJoint(Vector3 anchorPosition)
        {
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = transform.InverseTransformPoint(body.worldCenterOfMass);
            joint.connectedAnchor = anchorPosition;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit { limit = Vector3.Distance(body.worldCenterOfMass, anchorPosition) };
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= maxTime || !owner.Command.AbilityHeld)
                Release();
        }

        public void Release()
        {
            if (joint == null)
                return;
            Destroy(joint);
            joint = null;
            body.velocity *= ReleaseSpeedMultiplier;
            Released?.Invoke(this);
            Destroy(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Attached = null;
            Released = null;
        }
    }
}
