using GasSafari.Bots;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Greybox de los niveles del Mundo 1 (docs/04). Cada nivel es una función de este script: se rehace entero al repetirlo.
    // Distancias en metros a lo largo del cauce desde la salida.
    public static class W1LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private static readonly Color StatueColor = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color BushColor = new Color(0.2f, 0.55f, 0.25f);
        private static readonly Color FenceColor = new Color(0.4f, 0.4f, 0.5f);

        // ---------------------------------------------------------------- W1-1 Fuentes del Zoo
        [MenuItem("Gas Safari/Setup/Greybox W1-1 Fuentes del Zoo (M5-03)")]
        public static void BuildW1_1()
        {
            var level = GreyboxLevel.Create("W1-1", "L_W1_1_FuentesZoo");

            // Trayecto: canal ancho, rotonda de 270° a la izquierda con salida a la derecha (así el cauce no se cruza), zigzag,
            // aproximación a la fuente central y canal de salida.
            level.StartPath(Vector3.zero, 0f).Straight(300f, 12f);              // S1
            float rotonda = level.Length;
            level.Straight(30f, 10f).Arc(22f, -270f, 10f);                      // S2: giro cerrado alrededor de la estatua
            float rotondaEnd = level.Length;
            level.Arc(11f, 90f, 10f);                                           // salida hacia el sur, junto a S1
            float zigzag = level.Length;
            level.Straight(200f, 12f);                                         // S3: zigzag entre fuentes
            float fuente = level.Length;
            level.Straight(90f, 8f);                                           // S4: aproximación a la fuente central
            level.Straight(120f, 12f);                                         // canal de salida
            level.BuildChannel(0f); // docs/04 no pide corriente en W1-1

            level.Section(1, "CanalBancas", 0f);
            level.Section(2, "Rotonda", rotonda);
            level.Section(3, "Zigzag", zigzag);
            level.Section(4, "FuenteCentral", fuente);

            // S1: canal ancho entre bancas (H01): bancas de 4 m que sobresalen de cada orilla, alternadas.
            float[] benches = { 45f, 90f, 135f, 180f, 225f, 270f };
            for (int i = 0; i < benches.Length; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var bench = level.Place(HZ + "H01_Wall.prefab", benches[i], side * 5.1f, 0f, 0f, "Obstacles");
                bench.name = "H01_Bench_" + i;
                bench.transform.localScale = new Vector3(0.4f, 0.5f, 1f);
            }
            level.PowerUp("P01", 120f, 0f);

            // S2: rotonda. El centro del arco queda a 22 m a la izquierda del punto donde empieza.
            var center = level.PointAt(rotonda + 30f, -22f);
            level.Cylinder("Estatua", center, 14f, 6f, StatueColor);
            level.Mango(0, rotonda + 30f + 52f, -3.5f, 1f); // ruta: pegado a la estatua, a mitad de la rotonda

            level.AddCheckpoint(0f);
            level.AddCheckpoint(rotonda);

            // S3: zigzag entre fuentes que empujan hacia el centro; un chorro que eleva con el mango de riesgo.
            for (int i = 0; i < 8; i++)
            {
                float d = zigzag + 25f + i * 22f;
                float side = i % 2 == 0 ? 1f : -1f;       // +1 = derecha
                var jet = level.Place(HZ + "Jet_Lateral.prefab", d, side * 4.6f, side > 0f ? 180f : 0f, 0f, "Obstacles");
                jet.name = "Fountain_" + i;
            }
            level.PowerUp("P01", zigzag + 10f, 0f);
            var lift = level.Place(HZ + "H27_Jet.prefab", zigzag + 165f, 0f, 0f, 0f, "Obstacles");
            lift.name = "Jet_Lift";
            level.Mango(1, zigzag + 165f, 0f, 1.2f); // riesgo: sobre un chorro, hay que dejarse elevar

            level.AddCheckpoint(fuente);

            // S4: la fuente central lanza la balsa por encima de la reja hasta el canal de salida.
            level.PowerUp("P01", fuente + 10f, 0f);
            level.Hint(BotHintType.Ramp, fuente + 44f, 0f, 4f);
            var fountain = level.Place(HZ + "H27_Fountain.prefab", fuente + 50f, 0f, 0f, 0f, "Obstacles");
            fountain.name = "H27_FuenteCentral";
            // Reja a 8 m de la fuente: queda dentro del arco de vuelo llegue la balsa lenta o rápida (a 12 m la balsa caía encima).
            float fence = fuente + 58f;
            // Resaca entre la fuente y la reja: si el salto falla, devuelve la balsa al chorro (que rebota de nuevo).
            var undertow = level.Place(HZ + "Jet_Lateral.prefab", fence - 1.5f, 0f, 90f, 0f, "Obstacles"); // yaw 90: empuja hacia atrás
            undertow.name = "Fountain_Undertow";
            level.Block("Reja", level.PointAt(fence), new Vector3(level.WidthAt(fence) + 3f, 3f, 2f), FenceColor, Quaternion.LookRotation(level.DirectionAt(fence)).eulerAngles.y);

            // Mango escondido tras un arbusto en el canal de salida.
            level.Block("Arbusto", level.PointAt(fence + 28f, 4.6f), new Vector3(3f, 2.2f, 3f), BushColor);
            level.Mango(2, fence + 33f, 4.6f, 1f);

            level.Finish();
            Debug.Log($"M5-03 W1-1 greybox listo: {level.Length:F0} m");
        }
    }
}
