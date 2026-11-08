using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace GasSafari.Raft
{
    // Herramienta de diagnóstico (vuelcos a velocidad): guarda por paso físico velocidad, inclinación,
    // velocidad angular, profundidad de cada punto de flotación y fuerzas que entraron por ApplyThrust.
    // Se agrega en pruebas o a mano; no forma parte del prefab.
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(RaftController), typeof(RaftBuoyancy))]
    public class RaftTelemetry : MonoBehaviour
    {
        public struct Sample
        {
            public float Time;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Tilt;
            public float Pitch;
            public float Roll;
            public Vector3 LocalAngularVelocity;
            public float Depth0, Depth1, Depth2, Depth3;
            public Vector3 ThrustForce;
            public Vector3 LocalThrustTorque;
            public Vector3 FlowForce;
            public Vector3 LocalRightingTorque;
            public bool Airborne;
        }

        public struct Contact
        {
            public float Time;
            public string Other;
            public float Impulse;
            public float RelativeSpeed;
            public Vector3 Point;
        }

        private readonly List<Sample> samples = new List<Sample>();
        private readonly List<Contact> contacts = new List<Contact>();
        private RaftController raft;
        private RaftBuoyancy buoyancy;
        private RaftStabilizer stabilizer;
        private Vector3 pendingForce;
        private Vector3 pendingTorque;
        private Vector3 pendingFlow;

        public IReadOnlyList<Sample> Samples => samples;
        public IReadOnlyList<Contact> Contacts => contacts;
        public float MaxTilt { get; private set; }
        public float MaxSpeed { get; private set; }

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            buoyancy = GetComponent<RaftBuoyancy>();
            stabilizer = GetComponent<RaftStabilizer>();
        }

        private void OnEnable() => raft.ThrustApplied += OnThrust;
        private void OnDisable() => raft.ThrustApplied -= OnThrust;

        private void OnThrust(Vector3 force, Vector3 point, ForceMode mode)
        {
            var body = raft.Body;
            float dt = Time.fixedDeltaTime;
            Vector3 newtons;
            switch (mode)
            {
                case ForceMode.Impulse: newtons = force / dt; break;
                case ForceMode.Acceleration: newtons = force * body.mass; break;
                case ForceMode.VelocityChange: newtons = force * body.mass / dt; break;
                default: newtons = force; break;
            }

            if (mode == ForceMode.Acceleration)
            {
                pendingFlow += newtons;
                return;
            }
            pendingForce += newtons;
            pendingTorque += Vector3.Cross(point - body.worldCenterOfMass, newtons);
        }

        // Corre antes que el resto: lo acumulado es del paso anterior (incluye impulsos lanzados en Update).
        private void FixedUpdate()
        {
            var body = raft.Body;
            var t = transform;
            var velocity = body.velocity;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            var sample = new Sample
            {
                Time = Time.time,
                Position = t.position,
                Velocity = velocity,
                Tilt = Vector3.Angle(t.up, Vector3.up),
                Pitch = SignedAngle(t.forward),
                Roll = SignedAngle(t.right),
                LocalAngularVelocity = t.InverseTransformDirection(body.angularVelocity) * Mathf.Rad2Deg,
                Depth0 = DepthOrZero(0),
                Depth1 = DepthOrZero(1),
                Depth2 = DepthOrZero(2),
                Depth3 = DepthOrZero(3),
                ThrustForce = pendingForce,
                LocalThrustTorque = t.InverseTransformDirection(pendingTorque),
                FlowForce = pendingFlow,
                LocalRightingTorque = stabilizer != null ? t.InverseTransformDirection(stabilizer.LastTorque) : Vector3.zero,
                Airborne = buoyancy.IsAirborne,
            };
            samples.Add(sample);
            MaxTilt = Mathf.Max(MaxTilt, sample.Tilt);
            MaxSpeed = Mathf.Max(MaxSpeed, flat.magnitude);
            pendingForce = Vector3.zero;
            pendingTorque = Vector3.zero;
            pendingFlow = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            contacts.Add(new Contact
            {
                Time = Time.time,
                Other = collision.collider.name,
                Impulse = collision.impulse.magnitude,
                RelativeSpeed = collision.relativeVelocity.magnitude,
                Point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position,
            });
        }

        // Cabeceo en el agua (sin las muestras en el aire): máximo y percentil 95 del valor absoluto, en grados.
        public void PitchStats(out float max, out float p95)
        {
            var values = new List<float>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                if (!samples[i].Airborne)
                    values.Add(Mathf.Abs(samples[i].Pitch));
            }
            if (values.Count == 0)
            {
                max = 0f;
                p95 = 0f;
                return;
            }
            values.Sort();
            max = values[values.Count - 1];
            p95 = values[Mathf.Min(values.Count - 1, (int)(values.Count * 0.95f))];
        }

        public void Clear()
        {
            samples.Clear();
            contacts.Clear();
            MaxTilt = 0f;
            MaxSpeed = 0f;
        }

        // Ángulo de un eje local respecto del plano horizontal (positivo = el eje apunta hacia arriba).
        private static float SignedAngle(Vector3 axis) => Mathf.Asin(Mathf.Clamp(axis.y, -1f, 1f)) * Mathf.Rad2Deg;

        private float DepthOrZero(int index)
        {
            if (index >= buoyancy.PointCount)
                return 0f;
            float d = buoyancy.GetDepth(index);
            return float.IsNaN(d) ? -99f : d;
        }

        public void WriteCsv(string path)
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("t,x,y,z,vx,vy,vz,speedXZ,tilt,pitch,roll,wx,wy,wz,d0,d1,d2,d3,fx,fy,fz,tqPitch,tqYaw,tqRoll,flowX,flowZ,rightPitch,rightRoll,air");
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                float speed = new Vector2(s.Velocity.x, s.Velocity.z).magnitude;
                sb.Append(string.Format(c,
                    "{0:F3},{1:F2},{2:F3},{3:F2},{4:F2},{5:F2},{6:F2},{7:F2},{8:F1},{9:F1},{10:F1},{11:F0},{12:F0},{13:F0},{14:F3},{15:F3},{16:F3},{17:F3},",
                    s.Time, s.Position.x, s.Position.y, s.Position.z, s.Velocity.x, s.Velocity.y, s.Velocity.z, speed,
                    s.Tilt, s.Pitch, s.Roll, s.LocalAngularVelocity.x, s.LocalAngularVelocity.y, s.LocalAngularVelocity.z,
                    s.Depth0, s.Depth1, s.Depth2, s.Depth3));
                sb.AppendLine(string.Format(c, "{0:F0},{1:F0},{2:F0},{3:F0},{4:F0},{5:F0},{6:F0},{7:F0},{8:F0},{9:F0},{10}",
                    s.ThrustForce.x, s.ThrustForce.y, s.ThrustForce.z,
                    s.LocalThrustTorque.x, s.LocalThrustTorque.y, s.LocalThrustTorque.z,
                    s.FlowForce.x, s.FlowForce.z, s.LocalRightingTorque.x, s.LocalRightingTorque.z, s.Airborne ? 1 : 0));
            }
            sb.AppendLine();
            sb.AppendLine("# contactos: t,otro,impulso,velRel,x,z");
            for (int i = 0; i < contacts.Count; i++)
            {
                var k = contacts[i];
                sb.AppendLine(string.Format(c, "# {0:F2},{1},{2:F1},{3:F1},{4:F1},{5:F1}", k.Time, k.Other, k.Impulse, k.RelativeSpeed, k.Point.x, k.Point.z));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
        }
    }
}
