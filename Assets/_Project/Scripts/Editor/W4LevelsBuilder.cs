using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 4 (docs/04): cuevas y volcán, morado/cian/magma, poca luz y niebla cercana.
    // Cada nivel es una función de este script. OJO: GreyboxLevel.Create reutiliza la escena si ya existe y le añade el
    // contenido encima; para rehacer un nivel hay que borrar antes su escena.
    public static class W4LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W4/";
        private static readonly Color RockColor = new Color(0.22f, 0.15f, 0.27f);
        private static readonly Color LavaColor = new Color(0.82f, 0.1f, 0.05f);

        // ---------------------------------------------------------------- W4-1 Cascadas Escalonadas
        [MenuItem("Gas Safari/Setup/Greybox W4-1 Cascadas Escalonadas (M5-21)")]
        public static void BuildW4_1()
        {
            var level = GreyboxLevel.Create("W4-1", "L_W4_1_CascadasEscalonadas");
            var falls = new float[3];
            level.StartPath(Vector3.zero, 0f).Straight(60f, 12f);          // S1 tres cascadas pequeñas
            for (int i = 0; i < 3; i++)
            {
                falls[i] = level.Length;
                level.Straight(14f, 12f, -3f).Straight(30f, 12f);
            }
            float pools = level.Length;
            level.Straight(130f, 14f);                                                // S2 pozas con corriente circular
            float dbl = level.Length;
            level.Straight(30f, 14f, -6f).Straight(40f, 14f);                         // S3 cascada doble: elegir lado
            float big = level.Length;
            level.Straight(14f, 12f).Straight(70f, 12f, -20f).Straight(40f, 12f);     // S4 cascada de 20 m
            level.BuildChannel(0.3f);

            level.Section(1, "Cascadas", 0f);
            level.Section(2, "Pozas", pools);
            level.Section(3, "Doble", dbl);
            level.Section(4, "Grande", big);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(pools);
            level.AddCheckpoint(dbl);
            level.AddCheckpoint(big);

            // Avisos antes de cada borde (H17) y cortinas de agua.
            for (int i = 0; i < 3; i++)
                Edge(level, falls[i] - 10f, 3f);
            Edge(level, dbl - 10f, 6f);
            Edge(level, big + 4f, 20f);
            level.PowerUp("P01", 30f, 0f);

            // S2: dos pozas con corriente circular (remolino suave); mango en una poza lateral.
            for (int i = 0; i < 2; i++)
            {
                var pool = level.Place(HZ + "H09_Whirlpool.prefab", pools + 40f + i * 60f, i % 2 == 0 ? 3.5f : -3.5f);
                pool.name = "Poza_" + i;
                pool.transform.localScale = Vector3.one * (5f / 8f);
                level.Hint(Bots.BotHintType.Whirlpool, pools + 40f + i * 60f, i % 2 == 0 ? 3.5f : -3.5f, 5f);
            }
            level.WallGap(pools + 95f, pools + 105f);
            Alcove(level, pools + 100f, 1f);
            level.Mango(0, pools + 100f, 8.4f, 1f);
            level.PowerUp("P01", pools + 20f, 0f);
            level.PowerUp("P04", pools + 115f, 0f);

            // S3: roca que parte la cascada doble en dos lados; mango detrás de la cortina del lado izquierdo.
            Solid(level, "W4_CaveRock_B", dbl + 12f, 4.2f, Quaternion.identity, 1.1f, -0.4f).name = "RocaDivisoria";
            Curtain(level, dbl + 6f, -3.5f, 5f);
            level.Mango(1, dbl + 8f, -5.6f, 1f);
            level.PowerUp("P01", dbl + 55f, 0f);

            // S4: la cascada de 20 m con una cornisa a mitad de caída (mango, exige control en el aire).
            var ledge = Solid(level, "W4_CaveRock_A", big + 48f, 6f, Quaternion.identity, 0.9f, 0f);
            ledge.name = "Cornisa";
            level.Mango(2, big + 48f, 4.2f, 1.2f);
            Anchor(level, big + 16f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W4, LevelDresser.W4.Decor, 0.7f, new Color(0.18f, 0.1f, 0.24f)), 41,
                level.PointAt(big - 20f, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W4-1");
            level.Finish();
            Debug.Log($"M5-21 W4-1 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W4-2 Cueva de Cristal
        [MenuItem("Gas Safari/Setup/Greybox W4-2 Cueva de Cristal (M5-22)")]
        public static void BuildW4_2()
        {
            var level = GreyboxLevel.Create("W4-2", "L_W4_2_CuevaCristal");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 12f);                    // S1 entrada con luciérnagas
            float crystals = level.Length;
            level.Straight(150f, 12f);                                                // S2 cristales con pinchos
            float forks = level.Length;
            level.Arc(35f, 45f, 12f).Arc(35f, -90f, 12f).Arc(35f, 45f, 12f);         // S3 bifurcaciones sin luz
            float join = level.Length;
            level.Straight(20f, 12f);
            float dark = level.Length;
            level.Straight(110f, 12f);                                                // S4 las luciérnagas se apagan 20 s
            level.Branch(forks, join, 7f);
            level.BuildChannel(0.25f);

            level.Section(1, "Entrada", 0f);
            level.Section(2, "Cristales", crystals);
            level.Section(3, "Bifurcaciones", forks);
            level.Section(4, "Apagon", dark);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(crystals);
            level.AddCheckpoint(forks);
            level.AddCheckpoint(dark);

            // Luciérnagas (luces cian) en la entrada y los cristales.
            var lights = new List<Light>();
            int li = 0;
            for (float d = 12f; d < forks - 10f; d += 20f, li++)
                lights.Add(Firefly(level, d, li % 2 == 0 ? 3f : -3f));
            level.PowerUp("P01", 40f, 0f);

            // S2: pinchos de cristal (H10) alternados; cristal gigante con el mango a su lado.
            for (float d = crystals + 14f; d < forks - 8f; d += 14f)
            {
                int i = Mathf.RoundToInt((d - crystals) / 14f);
                level.Place(HZ + "H10_CrystalSpikes.prefab", d, i % 2 == 0 ? 4.6f : -4.6f, i * 41f).name = "PinchosCristal";
            }
            Solid(level, "W4_GiantCrystal", crystals + 60f, -8.2f, Quaternion.identity, 0.45f, 0f).name = "CristalGigante";
            level.Mango(0, crystals + 60f, -4.4f, 1.6f);
            level.PowerUp("P04", crystals + 75f, 0f);
            level.PowerUp("P01", crystals + 120f, 0f);

            // S3: bifurcaciones a oscuras (zona oscura H18 de 6 m de visibilidad); mango en la oscura; estalagmita (salto de Julien).
            var branchA = level.PointAt(forks);
            var branchB = level.PointAt(join);
            var chord = (branchB - branchA).normalized;
            var mid = (branchA + branchB) * 0.5f;
            var branchMango = level.Mango(1, 0f, 0f, 1f);
            branchMango.transform.position = mid + Vector3.up;
            Solid(level, "W4_Stalagmite_A", forks + 40f, -5.2f, Quaternion.identity, 0.8f, 0f).name = "Estalagmita";
            level.Mango(2, forks + 40f, -5.2f, 2.6f);
            level.PowerUp("P04", forks + 6f, 0f);
            level.PowerUp("P01", join + 6f, 0f);

            DarkSpan(level, forks, dark, null, 0f);
            // S4: luciérnagas que se apagan 20 s al entrar.
            var last = new List<Light>();
            int lj = 0;
            for (float d = dark + 10f; d < dark + 100f; d += 18f, lj++)
                last.Add(Firefly(level, d, lj % 2 == 0 ? 3f : -3f));
            // Pinchos de cristal en la oscuridad: hay que fiarse de los avisos y los pings.
            for (int k = 0; k < 4; k++)
                level.Place(HZ + "H10_CrystalSpikes.prefab", dark + 30f + k * 18f, k % 2 == 0 ? 3.4f : -3.4f, k * 53f).name = "PinchosCristalOscuros";
            level.PowerUp("P04", dark + 22f, 0f);
            DarkSpan(level, dark, level.Length, last.ToArray(), 20f);
            Anchor(level, dark + 55f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W4, LevelDresser.W4.Decor, 0.45f, new Color(0.12f, 0.06f, 0.18f)), 42,
                level.PointAt(crystals, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W4-2");
            level.Finish();
            Debug.Log($"M5-22 W4-2 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W4-3 Géiseres de Vapor
        [MenuItem("Gas Safari/Setup/Greybox W4-3 Géiseres de Vapor (M5-23)")]
        public static void BuildW4_3()
        {
            var level = GreyboxLevel.Create("W4-3", "L_W4_3_GeiseresVapor");
            level.StartPath(Vector3.zero, 0f).Straight(110f, 14f);                    // S1 géiseres como trampolín
            float pools = level.Length;
            level.Straight(130f, 16f);                                                // S2 aterrizajes en pozas
            float chain = level.Length;
            level.Straight(32f, 14f);                                                 // S3 cadena de 3 géiseres sobre un hueco
            float gap1 = level.Length;
            level.Straight(6f, 14f);
            float gap1End = level.Length;
            level.Straight(70f, 14f);
            float abyss = level.Length;
            level.Straight(9f, 14f);                                                  // S4 abismo: Súper Pedo
            float abyssEnd = level.Length;
            level.Straight(60f, 14f);
            // La cadena de géiseres queda sobre agua (los bots no la cruzan con precisión sobre un hueco); el abismo es el del final.
            // El abismo final (H22 + WaterGap) está desactivado: los bots no lo cruzan con el Súper Pedo (pendiente de diseño).
            level.BuildChannel(0.2f);

            level.Section(1, "Geiseres", 0f);
            level.Section(2, "Pozas", pools);
            level.Section(3, "Cadena", chain);
            level.Section(4, "Abismo", gap1End + 20f);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(pools);
            level.AddCheckpoint(chain);
            level.AddCheckpoint(gap1End + 20f);

            // S1: géiseres de trampolín (H19); mango en lo alto del primero.
            for (int i = 0; i < 3; i++)
            {
                var geyser = level.Place(HZ + "H19_Geyser.prefab", 30f + i * 32f, 0f);
                geyser.name = "Geiser_" + i;
                geyser.GetComponent<Geyser>().Configure(geyser.transform.Find("Vapor"), i * 1f);
                EditorUtility.SetDirty(geyser.GetComponent<Geyser>());
            }
            level.Mango(0, 30f, 0f, 5.5f);
            level.PowerUp("P01", 15f, 0f);

            // S2: pozas de aterrizaje con géiseres a los lados; mango bajo un puente de roca.
            for (int i = 0; i < 3; i++)
            {
                var geyser = level.Place(HZ + "H19_Geyser.prefab", pools + 25f + i * 35f, i % 2 == 0 ? 4f : -4f);
                geyser.name = "GeiserPoza_" + i;
                geyser.GetComponent<Geyser>().Configure(geyser.transform.Find("Vapor"), i * 0.7f);
                EditorUtility.SetDirty(geyser.GetComponent<Geyser>());
            }
            var bridge = level.Block("PuenteRoca", level.PointAt(pools + 95f, 0f, 7f), new Vector3(18f, 0.7f, 3f), RockColor,
                Quaternion.LookRotation(level.DirectionAt(pools + 95f)).eulerAngles.y);
            bridge.layer = GameLayers.Environment;
            level.Mango(2, pools + 95f, -3f, 1f);
            level.PowerUp("P01", pools + 10f, 0f);
            level.PowerUp("P03", pools + 118f, 0f);

            // S3: cadena de 3 géiseres antes del hueco: lanzan a la balsa sobre él; mango en el aire.
            for (int i = 0; i < 3; i++)
            {
                var geyser = level.Place(HZ + "H19_Geyser.prefab", chain + 6f + i * 9f, 0f);
                geyser.name = "GeiserCadena_" + i;
                geyser.GetComponent<Geyser>().Configure(geyser.transform.Find("Vapor"), i * 0.9f);
                EditorUtility.SetDirty(geyser.GetComponent<Geyser>());
            }
            level.Mango(1, gap1 + 8f, 0f, 3.8f);
            Anchor(level, chain + 20f, 0f);

            // S4: abismo (H22) que solo cruza el Súper Pedo.
            level.PowerUp("P01", abyss - 20f, 0f);
            level.PowerUp("P03", gap1End + 12f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W4, LevelDresser.W4.Decor, 0.6f, new Color(0.2f, 0.1f, 0.22f)), 43,
                level.PointAt(chain, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W4-3");
            level.Finish();
            Debug.Log($"M5-23 W4-3 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W4-4 Río Subterráneo
        [MenuItem("Gas Safari/Setup/Greybox W4-4 Río Subterráneo (M5-24)")]
        public static void BuildW4_4()
        {
            var level = GreyboxLevel.Create("W4-4", "L_W4_4_RioSubterraneo");
            level.StartPath(Vector3.zero, 0f).Straight(120f, 14f);                    // S1 dos corrientes que chocan
            float field = level.Length;
            level.Straight(30f, 14f);                                                 // S2 campo de pinchos bajos
            float fieldStart = level.Length;
            level.Straight(50f, 14f);
            float fieldEnd = level.Length;
            level.Straight(40f, 14f);
            float bats = level.Length;
            level.Straight(140f, 14f);                                                // S3 colonia de murciélagos
            float crack = level.Length;
            level.Straight(110f, 12f);                                                // S4 salida por una grieta iluminada
            level.Branch(fieldStart, fieldEnd, 6f);
            level.BuildChannel(0.2f);

            level.Section(1, "Corrientes", 0f);
            level.Section(2, "Pinchos", field);
            level.Section(3, "Murcielagos", bats);
            level.Section(4, "Grieta", crack);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(field);
            level.AddCheckpoint(bats);
            level.AddCheckpoint(crack);

            // S1: corrientes cruzadas (H31) en pares perpendiculares; mango en el cruce.
            for (int i = 0; i < 2; i++)
            {
                float d = 35f + i * 45f;
                float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
                level.PlaceAt(HZ + "H31_Current.prefab", level.PointAt(d, 0f), yaw + (i % 2 == 0 ? 0f : 180f)).name = "CorrienteLateral_" + i;
                level.PlaceAt(HZ + "H31_Current.prefab", level.PointAt(d, 0f), yaw + 270f).name = "CorrienteAdelante_" + i;
            }
            level.Mango(0, 80f, 0f, 1f);
            level.PowerUp("P01", 20f, 0f);
            level.PowerUp("P05", fieldStart - 6f, 0f);

            // S2: campo de pinchos bajos en el lado izquierdo (el resto del cauce queda libre para rodearlo; la gaseosa pasa por encima); mango encima.
            for (float d = fieldStart + 6f; d < fieldEnd - 4f; d += 6f)
            {
                for (float lateral = -6.5f; lateral <= -4.4f; lateral += 2.1f)
                    level.Place(HZ + "H10_CrystalSpikes.prefab", d, lateral, d * 7f).name = "PinchosBajos";
            }
            var topMango = level.Mango(1, 0f, 0f, 1f);
            topMango.transform.position = level.PointAt(fieldStart + 25f, -5.4f, 2.4f);
            level.PowerUp("P05", fieldStart + 14f, -3f);
            level.PowerUp("P01", fieldEnd + 20f, 0f);

            // S3: colonia de murciélagos (H26) en las dos orillas; mango escondido en el techo, a un costado.
            for (int i = 0; i < 5; i++)
                level.Place(HZ + "H26_Bats.prefab", bats + 20f + i * 25f, i % 2 == 0 ? 4.6f : -4.6f).name = "Murcielagos_" + i;
            level.WallGap(bats + 108f, bats + 118f);
            Alcove(level, bats + 113f, 1f);
            level.Mango(2, bats + 113f, 8.4f, 2.4f);
            level.PowerUp("P01", bats + 10f, 0f);
            level.PowerUp("P05", bats + 90f, 0f);
            level.PowerUp("P06", bats + 60f, 0f);
            Anchor(level, fieldStart + 25f, 0f);

            // S4: grieta iluminada (luz cian detrás del final).
            Firefly(level, crack + 90f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W4, LevelDresser.W4.Decor, 0.5f, new Color(0.14f, 0.07f, 0.2f)), 44,
                level.PointAt(bats, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W4-4");
            level.Finish();
            Debug.Log($"M5-24 W4-4 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W4-5 Corazón del Volcán (jefe)
        [MenuItem("Gas Safari/Setup/Greybox W4-5 Corazón del Volcán (M5-25)")]
        public static void BuildW4_5()
        {
            var level = GreyboxLevel.Create("W4-5", "L_W4_5_VolcanJefe");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 12f);                    // S1 río subterráneo con temblores
            float rocks = level.Length;
            level.Straight(130f, 12f);                                                // S2 rocas cayendo en patrón
            float chase = level.Length;
            level.Straight(150f, 12f);                                                // S3 derrumbe que persigue
            float exit = level.Length;
            level.Straight(90f, 14f);                                                 // S4 salida por la grieta, géiser final
            level.BuildChannel(0.4f);

            level.Section(1, "Temblores", 0f);
            level.Section(2, "Rocas", rocks);
            level.Section(3, "Derrumbe", chase);
            level.Section(4, "Grieta", exit);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(rocks);
            level.AddCheckpoint(rocks + 70f);
            level.AddCheckpoint(chase);
            level.AddCheckpoint(exit);

            // S1: temblores (cámara) y un desvío lateral con el mango (riesgo con el derrumbe).
            level.PowerUp("P01", 30f, 0f);
            level.PowerUp("P04", 70f, 0f);
            var quake = new GameObject("Temblores");
            SceneManager.MoveGameObjectToScene(quake, level.Scene);
            quake.layer = GameLayers.Trigger;
            quake.transform.SetPositionAndRotation(level.PointAt(60f, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(60f)));
            var quakeBox = quake.AddComponent<BoxCollider>();
            quakeBox.isTrigger = true;
            quakeBox.size = new Vector3(level.WidthAt(60f) + 2f, 4f, 100f);
            quake.AddComponent<CameraQuake>().Configure(6f, 0.5f);
            level.WallGap(chase + 60f, chase + 70f);
            Alcove(level, chase + 65f, 1f);
            level.Mango(0, chase + 65f, 8.4f, 1f);

            // S2: rocas que caen (H20) con sombra de aviso 1 s, en patrón fijo a lo largo del tramo; mango entre ellas.
            var rockFall = level.Place(HZ + "H20_RockFall.prefab", rocks + 60f, 0f);
            rockFall.name = "RocasQueCaen";
            var targets = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                var t = new GameObject("Objetivo_" + i).transform;
                t.SetParent(rockFall.transform, true);
                t.position = level.PointAt(rocks + 15f + i * 20f, i % 2 == 0 ? 3.5f : -3.5f, 0f);
                targets.Add(t);
            }
            var rf = rockFall.GetComponent<RockFall>();
            rf.Configure(targets.ToArray(), 2.5f, rockFall.transform.Find("Sombra"));
            EditorUtility.SetDirty(rf);
            level.Mango(1, rocks + 55f, 2.4f, 1f);
            level.PowerUp("P01", rocks + 8f, 0f);
            level.PowerUp("P04", rocks + 100f, 0f);

            // S3: el derrumbe persigue a la balsa por el cauce desde el inicio del tramo.
            var rubble = level.Block("Derrumbe", level.PointAt(chase - 14f, 0f, 1.5f), new Vector3(level.WidthAt(chase) + 4f, 3.6f, 4f), LavaColor,
                Quaternion.LookRotation(level.DirectionAt(chase)).eulerAngles.y);
            Object.DestroyImmediate(rubble.GetComponent<Collider>());
            var waypoints = new List<Vector3>();
            for (float d = chase - 14f; d <= exit + 20f; d += 10f)
                waypoints.Add(level.PointAt(d, 0f, 1.5f));
            var zone = new GameObject("ZonaDerrumbe");
            SceneManager.MoveGameObjectToScene(zone, level.Scene);
            zone.layer = GameLayers.Trigger;
            zone.transform.SetPositionAndRotation(level.PointAt(chase, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(chase)));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(chase) + 2f, 4f, 2f);
            var chaser = zone.AddComponent<CollapseChaser>();
            chaser.Configure(rubble.transform, waypoints.ToArray(), 4.5f);
            EditorUtility.SetDirty(chaser);
            level.PowerUp("P01", chase + 20f, 0f);
            level.PowerUp("P04", chase + 110f, 0f);

            // S4: géiser final que lanza la balsa fuera del volcán (momento estrella); mango en la grieta.
            var geyser = level.Place(HZ + "H19_Geyser.prefab", exit + 60f, 0f);
            geyser.name = "GeiserFinal";
            geyser.transform.localScale = Vector3.one * 1.4f;
            geyser.GetComponent<Geyser>().Configure(geyser.transform.Find("Vapor"), 0f);
            var finalSo = new SerializedObject(geyser.GetComponent<Geyser>());
            finalSo.FindProperty("armOnEnter").boolValue = true;
            finalSo.ApplyModifiedPropertiesWithoutUndo();
            level.Mango(2, exit + 62f, 0f, 5.5f);
            level.PowerUp("P01", exit + 20f, 0f);
            Anchor(level, exit + 38f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W4, LevelDresser.W4.Decor, 0.55f, new Color(0.25f, 0.08f, 0.1f)), 45,
                level.PointAt(exit, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W4-5");
            level.Finish();
            Debug.Log($"M5-25 W4-5 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- utilidades
        private static void Edge(GreyboxLevel level, float d, float drop)
        {
            var edge = level.Place(HZ + "H17_Waterfall.prefab", d, 0f);
            edge.name = "BordeCascada";
            var config = edge.GetComponent<WaterfallEdge>();
            Curtain(level, d + 10f, 0f, level.WidthAt(d));
            EditorUtility.SetDirty(config);
        }

        // Cortina de agua visual (sin collider).
        private static void Curtain(GreyboxLevel level, float d, float lateral, float width)
        {
            float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
            var curtain = level.Block("CortinaCascada", level.PointAt(d, lateral, 1.4f), new Vector3(width, 2.8f, 0.15f), new Color(0.6f, 0.9f, 1f), yaw);
            Object.DestroyImmediate(curtain.GetComponent<Collider>());
        }

        // Nicho en la pared derecha (mango escondido): fondo y lados sólidos, la pared tiene hueco (WallGap en el nivel).
        private static void Alcove(GreyboxLevel level, float d, float side)
        {
            float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
            level.Block("Nicho_Fondo", level.PointAt(d, side * 10.6f), new Vector3(1f, 3f, 7f), RockColor, yaw);
            level.Block("Nicho_Lado1", level.PointAt(d - 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), RockColor, yaw);
            level.Block("Nicho_Lado2", level.PointAt(d + 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), RockColor, yaw);
        }

        private static Light Firefly(GreyboxLevel level, float d, float lateral)
        {
            var go = new GameObject("Luciernaga");
            SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.transform.position = level.PointAt(d, lateral, 2.2f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.8f, 0.95f);
            light.range = 9f;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;
            go.AddComponent<GasSafari.VFX.LightFlicker>();
            return light;
        }

        // Zonas oscuras (H18) cada 40 m entre 'from' y 'to'; 'blackout' > 0 apaga las luciérnagas dadas al entrar en la primera.
        private static void DarkSpan(GreyboxLevel level, float from, float to, Light[] fireflies, float blackout)
        {
            bool first = true;
            for (float d = from + 20f; d < to; d += 40f)
            {
                var zone = level.Place(HZ + "H18_Dark.prefab", d, 0f, 0f);
                zone.name = "ZonaOscura";
                var box = zone.GetComponent<BoxCollider>();
                box.size = new Vector3(level.WidthAt(d) + 6f, 12f, 40f);
                zone.GetComponent<DarkZone>().Configure(6f, fireflies ?? new Light[0], first ? blackout : 0f);
                EditorUtility.SetDirty(zone.GetComponent<DarkZone>());
                first = false;
            }
        }

        // Abismo (H22): trigger bajo el hueco del cauce.
        private static void Abyss(GreyboxLevel level, float from, float to)
        {
            float d = (from + to) * 0.5f;
            var go = level.Place(HZ + "H22_Abyss.prefab", d, 0f, 0f, -33f);
            go.name = "Abismo";
            // Alto (de -63 a -3 m): la balsa cae a gran velocidad y no debe atravesar el trigger entre dos pasos de física.
            go.GetComponent<BoxCollider>().size = new Vector3(level.WidthAt(d) + 40f, 60f, to - from + 20f);
        }

        private static GameObject Solid(GreyboxLevel level, string fbx, float d, float lateral, Quaternion rotation, float scale, float y)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.transform.SetPositionAndRotation(level.PointAt(d, lateral, y), rotation);
            go.transform.localScale = Vector3.one * scale;
            PropColliders.AddSolidBox(go);
            return go;
        }

        // Punto del momento estrella (W2StarShots pone la zona y la toma aquí).
        private static void Anchor(GreyboxLevel level, float d, float lateral)
        {
            var go = new GameObject("EstrellaAncla");
            SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.transform.position = level.PointAt(d, lateral);
        }
    }
}
