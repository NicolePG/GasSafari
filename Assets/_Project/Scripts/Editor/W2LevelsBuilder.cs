using GasSafari.Core;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 2 (docs/04): mar abierto sin paredes (boyas y corriente que devuelve).
    // Cada nivel es una función de este script: se rehace entero al repetirlo. Distancias en metros desde la salida.
    public static class W2LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";

        // ---------------------------------------------------------------- W2-1 Cajas a la Deriva
        [MenuItem("Gas Safari/Setup/Greybox W2-1 Cajas a la Deriva (M5-09)")]
        public static void BuildW2_1()
        {
            var level = GreyboxLevel.Create("W2-1", "L_W2_1_CajasDeriva");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(170f, 18f);                    // S1 campo de cajas flotantes
            float barrels = level.Length;
            level.Arc(60f, 35f, 18f).Straight(110f, 18f);                             // S2 barriles con mecha
            float nets = level.Length;
            level.Straight(130f, 14f);                                                // S3 pasillo entre redes
            float mountain = level.Length;
            level.Arc(25f, -45f, 16f).Straight(30f, 16f).Arc(25f, 90f, 16f).Straight(30f, 16f).Arc(25f, -45f, 16f); // S4 rodeo
            float join = level.Length;
            level.Straight(60f, 18f);
            level.Branch(mountain, join, 10f);                                        // atajo tras la montaña de cajas
            level.BuildChannel(0.4f);                                                 // "corriente" del mar (provisional)

            level.Section(1, "Cajas", 0f);
            level.Section(2, "Barriles", barrels);
            level.Section(3, "Redes", nets);
            level.Section(4, "Montana", mountain);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(barrels);
            level.AddCheckpoint(nets);
            level.AddCheckpoint(mountain - 10f);

            var random = new System.Random(21);
            // S1: cajas flotantes (H07) repartidas; una tiene el mango dentro (romper con Rugido o golpe fuerte).
            for (int i = 0; i < 16; i++)
            {
                float d = 20f + i * 9f;
                float lateral = (float)(random.NextDouble() * 14.0 - 7.0);
                var crate = level.Place(HZ + "H07_Crate.prefab", d, lateral, (float)(random.NextDouble() * 90.0));
                crate.name = "Caja_" + i;
                if (i == 9)
                    level.Mango(0, d, lateral, 0.6f);
            }
            level.PowerUp("P01", 60f, 0f);
            level.PowerUp("P04", 130f, -3f);

            // S2: barriles con mecha (H06); mango entre dos barriles.
            for (int i = 0; i < 8; i++)
            {
                float d = barrels + 20f + i * 16f;
                level.Place(HZ + "H06_ExplosiveBarrel.prefab", d, i % 2 == 0 ? 4f : -4f).name = "Barril_" + i;
            }
            level.Mango(1, barrels + 76f, 0f, 1f);
            level.PowerUp("P01", barrels + 100f, 0f);

            // S3: pasillo entre redes de pesca (H32) que ocupan la mitad del carril, alternadas.
            for (int i = 0; i < 5; i++)
            {
                float d = nets + 18f + i * 22f;
                level.Place(HZ + "H32_FishingNet.prefab", d, i % 2 == 0 ? 3.5f : -3.5f).name = "Red_" + i;
            }
            level.PowerUp("P02", nets + 6f, 0f);
            level.PowerUp("P01", nets + 120f, 0f);

            // S4: montaña de cajas H07 que tapa el atajo; si se derrumba (Rugido, Terremoto o golpe fuerte), atajo y mango.
            var a = level.PointAt(mountain);
            var b = level.PointAt(join);
            var chord = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, chord);
            var mid = (a + b) * 0.5f;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5 - row; col++)
                {
                    var crate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H07_Crate.prefab"), level.Scene);
                    crate.name = "Montana_" + row + "_" + col;
                    crate.transform.SetPositionAndRotation(mid + right * ((col - (4 - row) * 0.5f) * 1.9f) + Vector3.up * (row * 1.8f),
                        Quaternion.LookRotation(chord));
                }
            }
            var mango = level.Mango(2, 0f, 0f, 1f);
            mango.transform.position = mid + chord * 8f + Vector3.up;

            LevelDresser.Dress(level, LevelDresser.W2, 21, level.PointAt(nets, 70f), -90f);
            level.Finish();
            Debug.Log($"M5-09 W2-1 listo: {level.Length:F0} m, atajo {Vector3.Distance(a, b):F0} m");
        }
    }
}
