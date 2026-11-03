using System;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Team
{
    // Detecta grupos de sincronía del equipo de una balsa y llena el medidor.
    [RequireComponent(typeof(TeamMeter), typeof(RaftSeats))]
    public class SyncDetector : MonoBehaviour
    {
        private static readonly int SourceCount = Enum.GetValues(typeof(SyncBonusSource)).Length;

        [SerializeField] private BalanceConfig balance;

        private SyncGroupTracker tracker;
        private TeamMeter meter;
        private RaftSeats seats;
        private float[] bonusAmounts;
        private float[] bonusTimers;
        private float partyTimer;
        private ISyncMember partyMember;

        public TeamMeter Meter => meter;
        // Fiesta Real de Julien: todo inicio de pedo cuenta como grupo completo.
        public bool IsPartyActive => partyTimer > 0f;
        public float Window => Services.Session.Difficulty.syncWindow + ExtraWindow;

        public float ExtraWindow
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < SourceCount; i++)
                {
                    if (bonusTimers[i] > 0f)
                        total += bonusAmounts[i];
                }
                return total;
            }
        }

        private void Awake()
        {
            meter = GetComponent<TeamMeter>();
            seats = GetComponent<RaftSeats>();
            tracker = new SyncGroupTracker(balance.syncMultipliers, balance.syncMeterGain);
            bonusAmounts = new float[SourceCount];
            bonusTimers = new float[SourceCount];
        }

        // Amplía la ventana durante 'duration' segundos. Repetir la misma fuente la renueva.
        public void SetWindowBonus(SyncBonusSource source, float amount, float duration)
        {
            bonusAmounts[(int)source] = amount;
            bonusTimers[(int)source] = duration;
        }

        public void StartParty(float duration, float meterMultiplier)
        {
            partyTimer = duration;
            meter.GainMultiplier = meterMultiplier;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < SourceCount; i++)
            {
                if (bonusTimers[i] > 0f)
                    bonusTimers[i] -= dt;
            }

            if (partyTimer <= 0f)
                return;
            partyTimer -= dt;
            if (partyTimer <= 0f)
            {
                meter.GainMultiplier = 1f;
                partyMember = null;
            }
        }

        // Si el miembro está en el grupo de sync actual (válido justo al recibir OnSync).
        public bool IsInGroup(ISyncMember member)
        {
            return (IsPartyActive && member == partyMember) || tracker.Contains(member);
        }

        // Lo llama cada inicio de pedo. Devuelve el multiplicador para ese pedo.
        public float Register(ISyncMember member, FartType type, float baseImpulse)
        {
            if (IsPartyActive)
                return RegisterParty(member);

            var result = tracker.Register(member, type, baseImpulse, Time.time, Window);
            if (result.GroupSize < 2)
                return result.Multiplier;

            meter.Add(result.MeterGain);
            GameEvents.RaiseSync(result.GroupSize, result.Multiplier);
            return result.Multiplier;
        }

        // Cada pedo es un grupo completo con todos los animales a bordo.
        private float RegisterParty(ISyncMember member)
        {
            int onBoard = Mathf.Clamp(seats.Seats.Count, 1, balance.syncMultipliers.Length - 1);
            float multiplier = balance.syncMultipliers[onBoard];
            if (onBoard < 2)
                return multiplier;

            partyMember = member;
            meter.Add(balance.syncMeterGain[onBoard]);
            GameEvents.RaiseSync(onBoard, multiplier);
            return multiplier;
        }
    }
}
