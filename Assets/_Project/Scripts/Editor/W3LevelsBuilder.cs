using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 3 (docs/04): playa y selva al atardecer, ríos de 12-14 m con paredes.
    // Cada nivel es una función de este script: se rehace entero al repetirlo. Distancias en metros desde la salida.
    public static class W3LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W3/";
        private static readonly Color BrownColor = new Color(0.32f, 0.22f, 0.12f);
        private static readonly Color FireColor = new Color(1f, 0.45f, 0.05f);

        // ---------------------------------------------------------------- W3-1 Playa de Llegada
        [MenuItem("Gas Safari/Setup/Greybox W3-1 Playa de Llegada (M5-15)")]
        public static void BuildW3_1()
        {
            var level = GreyboxLevel.Create("W3-1", "L_W3_1_PlayaLlegada");
            level.StartPath(Vector3.zero, 0f).Straight(150f, 14f);                    // S1 aguas poco profundas
            float crabs = level.Length;
            level.Straight(150f, 12f);                                                // S2 cangrejos
            float tide = level.Length;
            level.Arc(60f, 35f, 12f).Straight(130f, 12f);                             // S3 entrada al río sobre la ola de marea
            level.BuildChannel(0.3f);

            level.Section(1, "Playa", 0f);
            level.Section(2, "Cangrejos", crabs);
            level.Section(3, "Marea", tide);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(crabs);
            level.AddCheckpoint(tide);

            // S1: bancos de arena (H11) que dejan un paso de unos 6 m; castillo de arena con el mango encima.
            float[] bars = { 30f, 65f, 100f, 135f };
            float[] bar = { -3.2f, 3.2f, -2.8f, 3f };
            for (int i = 0; i < bars.Length; i++)
                level.Place(HZ + "H11_SandBar.prefab", bars[i], bar[i]).name = "BancoArena_" + i;
            var castle = Solid(level, "W3_Sandcastle_A", 80f, 4.8f, Quaternion.identity, 1f, -0.2f);
            castle.name = "CastilloArena";
            level.Mango(0, 80f, 3.4f, 1.8f);
            level.PowerUp("P01", 25f, 0f);

            // S2: rocas con cangrejos (H29) a los costados; mango sobre la tercera roca.
            for (int i = 0; i < 5; i++)
            {
                float d = crabs + 25f + i * 24f;
                float lateral = i % 2 == 0 ? 4.2f : -4.2f;
                Solid(level, "W3_Rock_A", d, lateral, Quaternion.Euler(0f, i * 70f, 0f), 1.3f, -0.4f).name = "RocaCangrejo_" + i;
                level.Place(HZ + "H29_Crab.prefab", d, lateral, i * 90f, 1.1f).name = "Cangrejo_" + i;
                if (i == 2)
                    level.Mango(1, d, lateral > 0 ? 2.8f : -2.8f, 2.0f);
            }
            level.PowerUp("P01", crabs + 12f, 0f);
            level.PowerUp("P03", crabs + 135f, 0f);

            // S3: la ola de marea baja por el cauce hacia el río (momento estrella); palmera caída con el mango detrás.
            var wave = W2LevelsBuilder.Wave("W3_TideWave", "Ola de marea", 1.0f, 28f, 5f);
            var tideWave = level.Place(HZ + "H04_Wave.prefab", tide + 12f, 0f, 0f);
            tideWave.name = "OlaMarea";
            W2LevelsBuilder.SetWave(tideWave, wave, 12f, 90f, 12f, 0f);
            Solid(level, "W3_Palm_C", tide + 100f, -5.4f, Quaternion.Euler(0f, 20f, 88f), 1f, 0.6f).name = "PalmeraCaida";
            level.Mango(2, tide + 100f, -4.4f, 1f);
            level.PowerUp("P01", tide + 60f, 0f);
            Anchor(level, tide + 25f, 0f);

            LevelDresser.Dress(level, LevelDresser.W3, 31, level.PointAt(tide + 20f, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W3-1");
            level.Finish();
            Debug.Log($"M5-15 W3-1 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W3-2 Río de Lianas
        [MenuItem("Gas Safari/Setup/Greybox W3-2 Río de Lianas (M5-16)")]
        public static void BuildW3_2()
        {
            var level = GreyboxLevel.Create("W3-2", "L_W3_2_RioLianas");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 12f);                    // S1 río cubierto de vegetación
            float curtains = level.Length;
            level.Arc(40f, 60f, 12f).Arc(40f, -120f, 12f).Arc(40f, 60f, 12f).Straight(20f, 12f); // S2 ruta normal: rodeo largo
            float logs = level.Length;
            level.Straight(150f, 12f);                                                // S3 troncos giratorios
            float tunnel = level.Length;
            level.Straight(110f, 10f);                                                // S4 túnel de lianas en llamas
            level.Branch(curtains, logs, 8f);                                         // atajo: lianas que se queman con locoto
            level.BuildChannel(0.3f);

            level.Section(1, "Vegetacion", 0f);
            level.Section(2, "Cortinas", curtains);
            level.Section(3, "Troncos", logs);
            level.Section(4, "TunelFuego", tunnel);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(curtains);
            level.AddCheckpoint(logs);
            level.AddCheckpoint(tunnel);

            level.PowerUp("P01", 40f, 0f);
            level.PowerUp("P02", curtains - 20f, 3f);
            level.PowerUp("P02", curtains - 20f, -3f);

            // S2: el atajo recto está cerrado por dos cortinas de lianas (H12); mango tras la primera.
            var a = level.PointAt(curtains);
            var b = level.PointAt(logs);
            var chord = (b - a).normalized;
            float yaw = Quaternion.LookRotation(chord).eulerAngles.y;
            float chordLength = Vector3.Distance(a, b);
            for (int i = 0; i < 2; i++)
            {
                var vine = level.PlaceAt(HZ + "H12_Vine.prefab", a + chord * (chordLength * (0.3f + 0.3f * i)), yaw);
                vine.name = "Liana_" + i;
                vine.transform.localScale = new Vector3(0.62f, 1.1f, 1f);
            }
            var shortcutMango = level.Mango(0, 0f, 0f, 1f);
            shortcutMango.transform.position = a + chord * (chordLength * 0.45f) + Vector3.up;
            level.PowerUp("P01", curtains + 70f, 0f);

            // S3: troncos giratorios (H13), uno podrido (Fragile); mango sobre el segundo.
            // Cada tronco gira con su eje a un costado (radio de barrido 2.5 m): deja un hueco de casi 7 m por el otro lado.
            var logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H13_SpinningLog.prefab");
            var logBox = logPrefab.GetComponent<BoxCollider>();
            float logScale = 5f / Mathf.Max(0.5f, Mathf.Max(logBox.size.x, logBox.size.z));
            for (int i = 0; i < 4; i++)
            {
                float d = logs + 25f + i * 32f;
                float logSide = i % 2 == 0 ? 3.3f : -3.3f;
                var log = level.Place(HZ + "H13_SpinningLog.prefab", d, logSide, i * 45f);
                log.transform.localScale = Vector3.one * logScale;
                log.name = "Tronco_" + i;
                if (i == 2)
                {
                    // Tronco podrido: el Rugido lo rompe.
                    var tags = log.GetComponent<HazardTags>() != null ? log.GetComponent<HazardTags>() : log.AddComponent<HazardTags>();
                    tags.Configure(HazardTag.Fragile);
                }
            }
            level.Mango(1, logs + 25f + 32f, -3.3f, 1.6f);
            level.PowerUp("P01", logs + 15f, 0f);
            level.PowerUp("P02", logs + 135f, 0f);

            // Cueva tras una cascada, en la pared izquierda (mango escondido).
            float cave = logs + 90f;
            level.WallGap(cave - 3f, cave + 3f);
            float caveYaw = Quaternion.LookRotation(level.DirectionAt(cave)).eulerAngles.y;
            level.Block("Cueva_Fondo", level.PointAt(cave, -10.5f), new Vector3(1f, 3f, 7f), BrownColor, caveYaw);
            level.Block("Cueva_Lado1", level.PointAt(cave - 3.5f, -8.25f), new Vector3(4.5f, 3f, 1f), BrownColor, caveYaw);
            level.Block("Cueva_Lado2", level.PointAt(cave + 3.5f, -8.25f), new Vector3(4.5f, 3f, 1f), BrownColor, caveYaw);
            Waterfall(level, cave, -6.2f, 6f, caveYaw);
            level.Mango(2, cave, -7.5f, 1f);

            // S4: túnel de lianas en llamas: arcos y fuego a los lados, luz naranja que parpadea.
            FireTunnel(level, tunnel + 6f, tunnel + 104f);
            Anchor(level, tunnel + 55f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W3, LevelDresser.W3.Decor, 0.8f, new Color(0.2f, 0.36f, 0.22f)), 32,
                level.PointAt(tunnel + 10f, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W3-2");
            level.Finish();
            Debug.Log($"M5-16 W3-2 listo: {level.Length:F0} m");
        }

        private static void FireTunnel(GreyboxLevel level, float from, float to)
        {
            var root = new GameObject("TunelFuego");
            SceneManager.MoveGameObjectToScene(root, level.Scene);
            var material = FireMaterial();
            for (float d = from; d <= to; d += 7f)
            {
                float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
                float half = level.WidthAt(d) * 0.5f;
                // Arco de lianas por encima de la cabeza de cualquier animal (sin collider: no estorba a la balsa ni a la cámara).
                var arch = level.Block("ArcoLianas", level.PointAt(d, 0f, 3.9f), new Vector3(level.WidthAt(d), 0.35f, 0.5f), BrownColor, yaw, true, "TunelFuego");
                Object.DestroyImmediate(arch.GetComponent<Collider>());
                for (int side = -1; side <= 1; side += 2)
                {
                    var fire = new GameObject("Fuego");
                    fire.transform.SetParent(root.transform, false);
                    fire.transform.position = level.PointAt(d, side * (half - 0.8f), 0.5f);
                    var ps = fire.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startLifetime = 0.9f;
                    main.startSpeed = 1.6f;
                    main.startSize = 0.9f;
                    main.startColor = new Color(1f, 0.55f, 0.1f, 0.85f);
                    main.gravityModifier = -0.3f;
                    var emission = ps.emission;
                    emission.rateOverTime = 18f;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 12f;
                    ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                    fire.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                }
                var lightGo = new GameObject("LuzFuego");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.position = level.PointAt(d, 0f, 2.6f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = FireColor;
                light.range = 11f;
                light.intensity = 2.4f;
                light.shadows = LightShadows.None;
                lightGo.AddComponent<GasSafari.VFX.LightFlicker>();
            }
        }

        private static Material FireMaterial()
        {
            const string path = "Assets/_Project/Art/Materials/World/M_Fire.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        // ---------------------------------------------------------------- W3-3 Territorio de las Fosas
        [MenuItem("Gas Safari/Setup/Greybox W3-3 Territorio de las Fosas (M5-17)")]
        public static void BuildW3_3()
        {
            var level = GreyboxLevel.Create("W3-3", "L_W3_3_TerritorioFosas");
            level.StartPath(Vector3.zero, 0f).Straight(180f, 12f);                    // S1 orillas altas con fosas
            float roots = level.Length;
            level.Straight(150f, 12f);                                                // S2 puentes de raíces
            float ambush = level.Length;
            level.Straight(120f, 12f);                                                // S3 emboscada en grupo
            float falls = level.Length;
            level.Straight(60f, 10f, -6f).Straight(30f, 10f);                         // S4 cascada de salida
            level.BuildChannel(0.3f);

            level.Section(1, "Orillas", 0f);
            level.Section(2, "Raices", roots);
            level.Section(3, "Emboscada", ambush);
            level.Section(4, "Cascada", falls);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(roots);
            level.AddCheckpoint(ambush);
            level.AddCheckpoint(falls);

            // S1: fosas escondidas en arbustos de las orillas (H14), alternando lados; aviso: el arbusto se mueve.
            for (int i = 0; i < 6; i++)
            {
                float d = 25f + i * 28f;
                float lateral = i % 2 == 0 ? 4.8f : -4.8f;
                level.Place(HZ + "H14_Pit.prefab", d, lateral, 0f).name = "Fosa_" + i;
            }
            level.PowerUp("P01", 60f, 0f);

            // S2: puentes de raíces por encima del cauce y un nido sobre las raíces (mango).
            for (int i = 0; i < 4; i++)
            {
                float d = roots + 20f + i * 34f;
                float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
                // Con collider (capa Environment): la cámara no atraviesa el puente.
                var bridge = level.Block("PuenteRaiz", level.PointAt(d, 0f, 4.7f), new Vector3(level.WidthAt(d) + 2f, 0.6f, 2.4f), BrownColor, yaw);
                bridge.layer = GameLayers.Environment;
            }
            var nest = level.Block("Nido", level.PointAt(roots + 54f, 5f, 0.3f), new Vector3(2.2f, 1f, 2.2f), BrownColor,
                Quaternion.LookRotation(level.DirectionAt(roots + 54f)).eulerAngles.y);
            nest.layer = GameLayers.Environment;
            level.Mango(0, roots + 54f, 4.2f, 1.6f);
            level.PowerUp("P01", roots + 90f, 0f);
            level.PowerUp("P04", roots + 130f, 0f);

            // S3: emboscada: seis fosas a la vez, a los dos lados; aguantar sin caer (mango en medio).
            for (int i = 0; i < 6; i++)
            {
                float d = ambush + 25f + (i / 2) * 6f;
                level.Place(HZ + "H14_Pit.prefab", d, i % 2 == 0 ? 4.8f : -4.8f, 0f).name = "Emboscada_" + i;
            }
            level.Mango(1, ambush + 51f, 0f, 1f);
            level.PowerUp("P01", ambush + 12f, 0f);
            Anchor(level, ambush + 51f, 0f);

            // S4: bajo la cascada, el mango.
            level.Mango(2, falls + 50f, 0f, 1f);
            Waterfall(level, falls + 44f, 0f, 10f, Quaternion.LookRotation(level.DirectionAt(falls + 44f)).eulerAngles.y);

            LevelDresser.Dress(level, LevelDresser.W3, 33, level.PointAt(roots, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W3-3");
            level.Finish();
            Debug.Log($"M5-17 W3-3 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W3-4 Pantano de los Cocodrilos
        [MenuItem("Gas Safari/Setup/Greybox W3-4 Pantano de los Cocodrilos (M5-18)")]
        public static void BuildW3_4()
        {
            var level = GreyboxLevel.Create("W3-4", "L_W3_4_PantanoCocodrilos");
            level.StartPath(Vector3.zero, 0f).Straight(140f, 14f);                    // S1 pantano con nenúfares
            float sleepers = level.Length;
            level.Straight(150f, 14f);                                                // S2 cocodrilos dormidos
            float silent = level.Length;
            level.Straight(120f, 12f);                                                // S3 pasaje en silencio
            float stampede = level.Length;
            level.Straight(140f, 16f);                                                // S4 estampida
            level.BuildChannel(0.2f);

            level.Section(1, "Pantano", 0f);
            level.Section(2, "Dormidos", sleepers);
            level.Section(3, "Silencio", silent);
            level.Section(4, "Estampida", stampede);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(sleepers);
            level.AddCheckpoint(silent);
            level.AddCheckpoint(stampede);

            // S1: nenúfares flotando (livianos: se apartan al empujarlos).
            var random = new System.Random(34);
            for (int i = 0; i < 14; i++)
            {
                float d = 15f + i * 9f;
                float lateral = (float)(random.NextDouble() * 10.0 - 5.0);
                var pad = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ENV + (i % 2 == 0 ? "W3_Lilypad_A" : "W3_Lilypad_B") + ".fbx"), level.Scene);
                pad.name = "Nenufar_" + i;
                pad.transform.position = level.PointAt(d, lateral, -0.05f);
                pad.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360.0), 0f);
                PropColliders.MakeLightFloater(pad, 0.4f);
            }
            level.PowerUp("P01", 60f, 0f);
            level.PowerUp("P06", sleepers - 15f, 0f);

            // S2: tres cocodrilos dormidos (H15) a los costados; mango sobre el lomo del primero.
            for (int i = 0; i < 3; i++)
            {
                float d = sleepers + 30f + i * 50f;
                float lateral = i % 2 == 0 ? 4.5f : -4.5f;
                var croc = level.Place(HZ + "H15_Crocodile.prefab", d, lateral, 90f);
                croc.name = "CocodriloDormido_" + i;
                if (i == 0)
                    level.Mango(0, d, lateral > 0 ? 3.4f : -3.4f, 1.2f);
            }
            level.PowerUp("P04", sleepers + 75f, 0f);
            level.PowerUp("P01", sleepers + 130f, 0f);

            // S3: pasaje en silencio: cuatro cocodrilos pegados a las orillas (solo toques suaves); mango en medio.
            for (int i = 0; i < 4; i++)
            {
                float d = silent + 20f + i * 25f;
                level.Place(HZ + "H15_Crocodile.prefab", d, i % 2 == 0 ? 4.2f : -4.2f, 90f).name = "CocodriloSilencio_" + i;
            }
            level.Mango(1, silent + 62f, 0f, 1f);
            
            // S4: estampida: seis cocodrilos que despiertan a la vez al cruzar la zona; tronco hueco con el mango detrás.
            var herd = new Crocodile[6];
            float[] lat = { -5.5f, 5.5f, -4.4f, 4.4f, -6.5f, 6.5f };
            for (int i = 0; i < herd.Length; i++)
            {
                var croc = level.Place(HZ + "H15_Crocodile.prefab", stampede + 40f + (i / 2) * 14f, lat[i], 90f);
                croc.name = "CocodriloEstampida_" + i;
                herd[i] = croc.GetComponent<Crocodile>();
            }
            var zone = new GameObject("ZonaEstampida");
            SceneManager.MoveGameObjectToScene(zone, level.Scene);
            zone.layer = GameLayers.Trigger;
            zone.transform.SetPositionAndRotation(level.PointAt(stampede + 24f, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(stampede + 24f)));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(stampede + 24f) + 2f, 4f, 3f);
            zone.AddComponent<CrocStampede>().Configure(herd);
            Solid(level, "W3_Log_B", stampede + 100f, -6.2f, Quaternion.Euler(0f, 90f, 0f), 1f, 0f).name = "TroncoHueco";
            level.Mango(2, stampede + 100f, -4.6f, 1.1f);
            level.PowerUp("P04", stampede + 10f, 0f);
            level.PowerUp("P06", stampede + 20f, -2f);
            level.PowerUp("P01", stampede + 110f, 0f);
            Anchor(level, stampede + 50f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W3, LevelDresser.W3.Decor, 0.85f, new Color(0.38f, 0.5f, 0.3f)), 34,
                level.PointAt(sleepers, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W3-4");
            level.Finish();
            Debug.Log($"M5-18 W3-4 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W3-5 La Gran Fiesta Lémur (jefe)
        [MenuItem("Gas Safari/Setup/Greybox W3-5 La Gran Fiesta Lémur (M5-19)")]
        public static void BuildW3_5()
        {
            var level = GreyboxLevel.Create("W3-5", "L_W3_5_FiestaLemurJefe");
            level.StartPath(Vector3.zero, 0f).Straight(120f, 14f);                    // S1 entrada con tambores
            float beats = level.Length;
            level.Straight(150f, 14f);                                                // S2 plataformas y chorros al ritmo (110 BPM)
            float change = level.Length;
            level.Straight(150f, 14f);                                                // S3 cambio de canción: 128 BPM
            float stage = level.Length;
            level.Straight(110f, 16f);                                                // S4 escenario final con confeti
            level.BuildChannel(0.2f);

            level.Section(1, "Tambores", 0f);
            level.Section(2, "Ritmo110", beats);
            level.Section(3, "Ritmo128", change);
            level.Section(4, "Escenario", stage);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(beats);
            level.AddCheckpoint(change);
            level.AddCheckpoint(stage);

            // Reloj de compás (110 BPM) y bono de pedo en el pulso.
            var director = new GameObject("Ritmo");
            SceneManager.MoveGameObjectToScene(director, level.Scene);
            director.AddComponent<RhythmClock>().Configure(110f);
            director.AddComponent<RhythmSyncBonus>();

            // S1: tambores (decorado) y los primeros bloques que se mueven al compás.
            for (int i = 0; i < 3; i++)
                RhythmBar(level, 40f + i * 28f, i % 2 == 0, 8f, 4.5f, 8f);
            level.PowerUp("P01", 20f, 0f);

            // S2: barras y una plataforma que solo baja en el compás 4 (con el mango encima).
            for (int i = 0; i < 4; i++)
                RhythmBar(level, beats + 20f + i * 26f, i % 2 == 0, 8f, 4.5f, 8f);
            var gate = level.Place(HZ + "H16_Rhythm.prefab", beats + 128f, 4.2f);
            gate.name = "PlataformaCompas4";
            gate.transform.localScale = new Vector3(2f, 1f, 1f);
            gate.GetComponent<RhythmObstacle>().Configure(Vector3.down, 1.6f, 4f, 4);
            level.Mango(1, beats + 128f, 4.2f, 2.2f);
            level.PowerUp("P01", beats + 10f, 0f);
            level.PowerUp("P03", beats + 100f, 0f);

            // Cambio de canción a 128 BPM a mitad del nivel (momento estrella).
            var tempo = new GameObject("CambioCancion");
            SceneManager.MoveGameObjectToScene(tempo, level.Scene);
            tempo.layer = GameLayers.Trigger;
            tempo.transform.SetPositionAndRotation(level.PointAt(change, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(change)));
            var tempoBox = tempo.AddComponent<BoxCollider>();
            tempoBox.isTrigger = true;
            tempoBox.size = new Vector3(level.WidthAt(change) + 2f, 4f, 2f);
            tempo.AddComponent<RhythmTempoZone>().Configure(128f);
            Anchor(level, change + 6f, 0f);

            // S3: barras más rápidas (ciclo de 2 pulsos).
            for (int i = 0; i < 5; i++)
                RhythmBar(level, change + 22f + i * 24f, i % 2 == 0, 4f, 4.5f, 4f);
            level.PowerUp("P01", change + 10f, 0f);
            level.PowerUp("P03", change + 120f, 0f);

            // S4: escenario de DJ con el mango sobre su plataforma lateral y otro escondido tras él; lluvia de confeti.
            Solid(level, "W3_DJStage", stage + 60f, 7.2f, Quaternion.Euler(0f, -90f, 0f), 1f, 0f).name = "EscenarioDJ";
            level.Mango(0, stage + 60f, 3.6f, 1.6f);
            Solid(level, "W3_Rock_B", stage + 98f, -6f, Quaternion.identity, 1.2f, -0.3f).name = "RocaTrasEscenario";
            level.Mango(2, stage + 98f, -3.6f, 1f);
            Confetti(level, stage + 8f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W3, LevelDresser.W3.Decor, 1.0f, new Color(0.7f, 0.5f, 0.55f)), 35,
                level.PointAt(stage + 60f, 40f), -90f);
            W2StarShots.Apply(level.Scene, "W3-5");
            level.Finish();
            Debug.Log($"M5-19 W3-5 listo: {level.Length:F0} m");
        }

        // Barra rítmica (H16) que cruza el cauce de un lado a otro en un compás de 'cycle' pulsos.
        private static void RhythmBar(GreyboxLevel level, float d, bool fromLeft, float cycle, float travel, float unused)
        {
            float half = level.WidthAt(d) * 0.5f - 1.6f;
            var go = level.Place(HZ + "H16_Rhythm.prefab", d, fromLeft ? -half : half, Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y);
            go.name = "BarraRitmo";
            go.GetComponent<RhythmObstacle>().Configure(fromLeft ? Vector3.right : Vector3.left, Mathf.Min(travel, half * 2f - 3f), cycle, 0);
        }

        private static void Confetti(GreyboxLevel level, float zoneDistance)
        {
            var go = new GameObject("Confeti");
            SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.AddComponent<GasSafari.VFX.FollowCamera>();
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 0f;
            main.startSize = 0.22f;
            main.gravityModifier = 0.4f;
            main.maxParticles = 700;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.97f, 0.15f, 0.52f), new Color(0.71f, 0.89f, 0.55f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 120f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 30f);
            go.transform.position = Vector3.up * 14f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = FireMaterial();
            go.SetActive(false);
            var zone = new GameObject("ZonaConfeti");
            SceneManager.MoveGameObjectToScene(zone, level.Scene);
            zone.layer = GameLayers.Trigger;
            zone.transform.SetPositionAndRotation(level.PointAt(zoneDistance, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(zoneDistance)));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(zoneDistance) + 2f, 4f, 2f);
            zone.AddComponent<ActivateOnRaft>().Configure(go);
        }

        // ---------------------------------------------------------------- utilidades
        private static GameObject Solid(GreyboxLevel level, string fbx, float d, float lateral, Quaternion rotation, float scale, float y)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.transform.SetPositionAndRotation(level.PointAt(d, lateral, y), rotation);
            go.transform.localScale = Vector3.one * scale;
            PropColliders.AddSolidBox(go);
            return go;
        }

        // Cortina de cascada (visual, sin collider) de 'width' m de ancho en el lateral dado.
        private static void Waterfall(GreyboxLevel level, float d, float lateral, float width, float yaw)
        {
            var curtain = level.Block("CortinaCascada", level.PointAt(d, lateral, 1.6f), new Vector3(0.15f, 3.2f, width), new Color(0.7f, 0.9f, 1f), yaw);
            Object.DestroyImmediate(curtain.GetComponent<Collider>());
        }

        // Punto del momento estrella: W2StarShots pone la zona y la toma de cámara aquí.
        private static void Anchor(GreyboxLevel level, float d, float lateral)
        {
            var go = new GameObject("EstrellaAncla");
            SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.transform.position = level.PointAt(d, lateral);
        }
    }
}
