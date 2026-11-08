using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Vuelco y aterrizajes (docs/02 §1 y §15).
    [RequireComponent(typeof(RaftController), typeof(RaftSeats))]
    public class RaftStateMonitor : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private readonly List<PlayerSeat> buffer = new List<PlayerSeat>();
        private RaftController raft;
        private RaftBuoyancy buoyancy;
        private RaftSeats seats;
        private Vector3 airVelocity;
        private float capsizeTimer;
        private bool tippedSideFell;

        public float Tilt { get; private set; }
        public bool HasCapsized { get; private set; }

        // crooked: aterrizó con más de landingTiltAngle.
        public event Action<bool> Landed;

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            buoyancy = GetComponent<RaftBuoyancy>();
            seats = GetComponent<RaftSeats>();
        }

        private void OnEnable()
        {
            buoyancy.AirborneChanged += OnAirborneChanged;
        }

        private void OnDisable()
        {
            buoyancy.AirborneChanged -= OnAirborneChanged;
        }

        // Tras reaparecer en un checkpoint.
        public void ResetState()
        {
            capsizeTimer = 0f;
            tippedSideFell = false;
            HasCapsized = false;
        }

        private void FixedUpdate()
        {
            if (buoyancy.IsAirborne)
                airVelocity = raft.Body.velocity;

            Tilt = Vector3.Angle(transform.up, Vector3.up);
            if (Tilt <= balance.capsizeAngle)
            {
                capsizeTimer = 0f;
                tippedSideFell = false;
                return;
            }

            if (!tippedSideFell)
            {
                tippedSideFell = true;
                DropLowSide();
            }

            capsizeTimer += Time.fixedDeltaTime;
            if (!HasCapsized && capsizeTimer >= balance.capsizeTime)
            {
                HasCapsized = true;
                GameEvents.RaiseRaftCapsized();
            }
        }

        // Al volcar, caen los animales del lado que se hunde.
        private void DropLowSide()
        {
            var down = transform.up;
            down.y = 0f;
            Collect();
            for (int i = 0; i < buffer.Count; i++)
            {
                var offset = buffer[i].transform.position - transform.position;
                if (Vector3.Dot(offset, down) > 0f)
                    buffer[i].GetComponent<AvatarFall>().Fall(Vector3.zero);
            }
        }

        private void OnAirborneChanged(bool airborne)
        {
            if (airborne)
                return;

            bool crooked = Vector3.Angle(transform.up, Vector3.up) > balance.landingTiltAngle;
            if (crooked)
                CrookedLanding();
            Landed?.Invoke(crooked);
        }

        private void CrookedLanding()
        {
            if (balance.landingBounceImpulse > 0f)
                raft.ApplyThrust(Vector3.up * balance.landingBounceImpulse, raft.Body.worldCenterOfMass, ForceMode.Impulse);

            // Cada animal no agarrado recibe el golpe del impacto vertical (misma regla de velocidad que los choques).
            float fallSpeed = Mathf.Max(0f, -airVelocity.y);
            Collect();
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].GetComponent<AvatarFall>().Knockoff(fallSpeed, fallSpeed);
        }

        private void Collect()
        {
            buffer.Clear();
            buffer.AddRange(seats.Seats);
        }
    }
}
