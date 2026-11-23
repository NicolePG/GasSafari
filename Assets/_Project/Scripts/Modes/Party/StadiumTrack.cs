using System;
using UnityEngine;

namespace GasSafari.Modes
{
    // Pista ovalada de Carrera de Burbujas: dos rectas paralelas al eje X unidas por dos semicírculos. Coordenadas de pista:
    // 's' = metros a lo largo de la línea central (0 = inicio de la recta de abajo, se avanza hacia +X, sentido antihorario
    // visto desde arriba) y 'lat' = metros hacia afuera de la línea central (negativo = hacia la isla).
    [Serializable]
    public class StadiumTrack
    {
        public Vector3 center;
        [Tooltip("Mitad del largo de cada recta (m)")] public float halfStraight = 10f;
        [Tooltip("Radio de la línea central en las curvas (m)")] public float radius = 9f;
        [Tooltip("Mitad del ancho del carril (m)")] public float halfWidth = 3.5f;

        public StadiumTrack() { }

        public StadiumTrack(Vector3 trackCenter, float straightHalf, float turnRadius, float laneHalfWidth)
        {
            center = trackCenter;
            halfStraight = straightHalf;
            radius = turnRadius;
            halfWidth = laneHalfWidth;
        }

        public float Straight => 2f * halfStraight;
        public float Turn => Mathf.PI * radius;
        public float Perimeter => 2f * Straight + 2f * Turn;

        public float Wrap(float s) => Mathf.Repeat(s, Perimeter);

        public void Project(Vector3 position, out float s, out float lat)
        {
            float x = position.x - center.x;
            float z = position.z - center.z;
            float a = halfStraight;
            if (x >= -a && x <= a)
            {
                if (z < 0f)
                {
                    s = x + a;
                    lat = -z - radius;
                }
                else
                {
                    s = Straight + Turn + (a - x);
                    lat = z - radius;
                }
                return;
            }
            if (x > a)
            {
                float phi = Mathf.Atan2(z, x - a);
                s = Straight + (phi + Mathf.PI * 0.5f) * radius;
                lat = new Vector2(x - a, z).magnitude - radius;
                return;
            }
            float psi = Mathf.Atan2(z, x + a);
            if (psi < 0f)
                psi += 2f * Mathf.PI;
            s = 2f * Straight + Turn + (psi - Mathf.PI * 0.5f) * radius;
            lat = new Vector2(x + a, z).magnitude - radius;
        }

        public Vector3 Point(float s, float lat)
        {
            s = Wrap(s);
            float a = halfStraight;
            float r = radius + lat;
            Vector3 local;
            if (s < Straight)
                local = new Vector3(s - a, 0f, -r);
            else if (s < Straight + Turn)
            {
                float phi = (s - Straight) / radius - Mathf.PI * 0.5f;
                local = new Vector3(a + Mathf.Cos(phi) * r, 0f, Mathf.Sin(phi) * r);
            }
            else if (s < 2f * Straight + Turn)
                local = new Vector3(a - (s - Straight - Turn), 0f, r);
            else
            {
                float psi = (s - 2f * Straight - Turn) / radius + Mathf.PI * 0.5f;
                local = new Vector3(-a + Mathf.Cos(psi) * r, 0f, Mathf.Sin(psi) * r);
            }
            return center + local;
        }

        // Dirección de avance en 's' (horizontal, unitaria).
        public Vector3 Direction(float s)
        {
            var ahead = Point(s + 0.5f, 0f) - Point(s - 0.5f, 0f);
            ahead.y = 0f;
            return ahead.normalized;
        }

        // Diferencia s1 - s0 por el camino más corto de la vuelta (para seguir el avance cuadro a cuadro).
        public float Delta(float s0, float s1)
        {
            float d = s1 - s0;
            float p = Perimeter;
            if (d > p * 0.5f)
                d -= p;
            else if (d < -p * 0.5f)
                d += p;
            return d;
        }
    }
}
