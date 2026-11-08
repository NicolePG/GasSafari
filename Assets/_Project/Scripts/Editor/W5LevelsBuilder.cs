using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 5 (docs/04): cañón al atardecer, dorado #FFB703, rojo #9C3D1C y naranja #FB8500.
    // Cada nivel es una función de este script: todo lo que arma cuelga de _Generated y correrlo de nuevo lo rehace.
    public static class W5LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W5/";
        private static readonly Color RockColor = new Color(0.61f, 0.24f, 0.11f);
        private static readonly Color ConcreteColor = new Color(0.55f, 0.45f, 0.38f);

        // ---------------------------------------------------------------- W5-1 Rápidos del Cañón
        [MenuItem("Gas Safari/Setup/Greybox W5-1 Rápidos del Cañón (M5-27)")]
        public static void BuildW5_1()
        {
            var level = GreyboxLevel.Create("W5-1", "L_W5_1_RapidosCanon");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 14f);                    // S1 entrada tranquila
            float class1 = level.Length;
            level.Straight(110f, 14f);                                                // S2 rápidos clase 1
            float class2 = level.Length;
            level.Straight(120f, 14f);                                                // S3 rápidos clase 2 con rocas
            float longRapid = level.Length;
            level.Straight(170f, 14f);                                                // S4 el rápido más largo del juego
            level.Straight(30f, 14f);
            level.BuildChannel(0.3f);

            level.Section(1, "Entrada", 0f);
            level.Section(2, "Clase1", class1);
            level.Section(3, "Clase2", class2);
            level.Section(4, "RapidoLargo", longRapid);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(class1);
            level.AddCheckpoint(class2);
            level.AddCheckpoint(longRapid);

            // Rápidos (H21): clase 1 a media fuerza, clase 2 y el rápido largo a fuerza completa; las rocas del cauce son sólidas.
            Rapids(level, class1 + 6f, class2 - 6f, 0.5f);
            Rapids(level, class2 + 6f, longRapid - 6f, 1f);
            Rapids(level, longRapid + 6f, longRapid + 164f, 1f);
            level.PowerUp("P01", 24f, 0f);

            // S2: unas pocas rocas bajas (el rápido de clase 1 solo avisa).
            Rock(level, "W5_CanyonRock_A", class1 + 45f, -4.6f, 0f, 0.9f);
            Rock(level, "W5_CanyonRock_B", class1 + 85f, 4.6f, 70f, 0.9f);
            // Remanso tras una roca grande, junto a la orilla: mango de la ruta.
            Rock(level, "W5_CanyonRock_C", class1 + 55f, 5.2f, 20f, 1.2f);
            level.Mango(0, class1 + 66f, 5.6f, 1f);
            level.PowerUp("P04", class1 + 100f, 0f);

            // S3: campo de rocas alternadas (clase 2).
            for (int i = 0; i < 7; i++)
                Rock(level, i % 2 == 0 ? "W5_CanyonRock_A" : "W5_CanyonRock_B", class2 + 14f + i * 15f, i % 2 == 0 ? -3.8f : 3.8f, i * 53f, 0.85f);
            // Cascada pequeña lateral con el mango de riesgo.
            level.WallGap(class2 + 70f, class2 + 80f);
            Alcove(level, class2 + 75f, 1f);
            level.Mango(1, class2 + 75f, 8.4f, 1f);
            level.PowerUp("P01", class2 + 8f, 0f);

            // S4: el rápido más largo: pocas rocas, mango al final sin frenar.
            for (int i = 0; i < 5; i++)
                Rock(level, "W5_CanyonRock_C", longRapid + 30f + i * 30f, i % 2 == 0 ? 4.2f : -4.2f, i * 37f, 0.9f);
            level.Mango(2, longRapid + 168f, 0f, 1f);
            level.PowerUp("P04", longRapid + 12f, 0f);
            level.PowerUp("P01", longRapid + 100f, 0f);
            Anchor(level, longRapid + 90f, 0f);

            LevelDresser.Dress(level, LevelDresser.W5, 51, level.PointAt(longRapid, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W5-1");
            level.Finish();
            Debug.Log($"M5-27 W5-1 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W5-2 Rampas del Abismo
        [MenuItem("Gas Safari/Setup/Greybox W5-2 Rampas del Abismo (M5-28)")]
        public static void BuildW5_2()
        {
            var level = GreyboxLevel.Create("W5-2", "L_W5_2_RampasAbismo");
            level.StartPath(Vector3.zero, 0f).Straight(30f, 14f);
            // S1: tres rampas pequeñas: sube 1 m en 8 m y un hueco de 2.5 m con el agua 1 m más abajo al otro lado.
            var gapsFrom = new float[3];
            var gapsTo = new float[3];
            for (int i = 0; i < 3; i++)
            {
                level.Straight(8f, 14f, 1f);
                gapsFrom[i] = level.Length;
                level.Straight(2.5f, 14f, -1f);
                gapsTo[i] = level.Length;
                level.Straight(24f, 14f);
            }
            float platforms = level.Length;
            level.Straight(100f, 14f);                                                // S2 plataformas flotantes
            float bridges = level.Length;
            level.Straight(110f, 14f);                                                // S3 puentes rotos
            float jump = level.Length;
            level.Straight(40f, 14f);                                                 // S4 el gran salto
            level.Straight(14f, 14f, 2f);                                             // rampa de despegue (el agua sube 2 m)
            float abyss = level.Length;
            level.Straight(18f, 14f, -2f);                                            // hueco de 18 m: el otro lado queda 2 m más abajo
            float abyssEnd = level.Length;
            level.Straight(60f, 14f);
            for (int i = 0; i < 3; i++)
                level.WaterGap(gapsFrom[i], gapsTo[i]);
            level.WaterGap(abyss, abyssEnd);
            level.NoBaseWater();
            level.BuildChannel(0.2f);

            level.Section(1, "Rampas", 0f);
            level.Section(2, "Plataformas", platforms);
            level.Section(3, "Puentes", bridges);
            level.Section(4, "GranSalto", jump);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(platforms);
            level.AddCheckpoint(bridges);
            level.AddCheckpoint(jump);

            // S1: rampas con su marca para que Marty galope; abismos pequeños con ayuda (puente de boyas tras 2 caídas).
            for (int i = 0; i < 3; i++)
            {
                Abyss(level, gapsFrom[i], gapsTo[i], 5f);
                level.Hint(Bots.BotHintType.Ramp, gapsFrom[i] - 6f, 0f, 5f);
            }
            level.Mango(0, gapsFrom[0] - 4f, 0f, 2.2f);                              // sobre la primera rampa
            level.PowerUp("P01", 16f, 0f);
            level.PowerUp("P03", gapsTo[1] + 10f, 0f);

            // S2: plataformas flotantes que se hunden (H35) repartidas por el cauce; mango en una plataforma lateral.
            for (int i = 0; i < 6; i++)
            {
                var platform = level.Place(HZ + "H35_SinkingPlatform.prefab", platforms + 14f + i * 14f, i % 2 == 0 ? -4f : 4f);
                platform.transform.localScale = Vector3.one * 0.6f;
                platform.name = "PlataformaFlotante_" + i;
            }
            var side = level.Place(HZ + "H35_SinkingPlatform.prefab", platforms + 58f, 6.3f);
            side.transform.localScale = Vector3.one * 0.5f;
            side.name = "PlataformaLateral";
            level.Mango(1, platforms + 58f, 6.3f, 1.6f);
            level.PowerUp("P01", platforms + 8f, 0f);

            // S3: puentes rotos (vista) sobre el cauce, uno con la roca caída; sin daño.
            for (int i = 0; i < 3; i++)
                Overpass(level, bridges + 20f + i * 34f);
            level.PowerUp("P03", bridges + 60f, 0f);
            level.PowerUp("P01", bridges + 100f, 0f);

            // S4: gran salto. Antes de la rampa, un géiser de gas dorado (H36) llena el medidor; en el despegue los bots lanzan el
            // Súper Pedo solos (BotHint SuperFart); si el equipo cae 2 veces aparece el puente de boyas. Mango en el aire.
            var gold = level.Place(HZ + "H36_GoldGasGeyser.prefab", abyss - 30f, 0f);
            gold.name = "H36_GeiserGasDorado";
            Abyss(level, abyss, abyssEnd, 8f);
            level.Hint(Bots.BotHintType.SuperFart, abyss - 1.5f, 0f, 6f);
            var air = level.Mango(2, 0f, 0f, 1f);
            air.transform.position = level.PointAt((abyss + abyssEnd) * 0.5f, 0f, 3.6f);
            level.PowerUp("P01", abyss - 40f, 0f);
            level.PowerUp("P03", jump + 12f, 0f);
            Anchor(level, abyss - 6f, 0f);

            LevelDresser.Dress(level, LevelDresser.W5, 52, level.PointAt(jump, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W5-2");
            level.Finish();
            Debug.Log($"M5-28 W5-2 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W5-3 Laberinto de Manglares
        [MenuItem("Gas Safari/Setup/Greybox W5-3 Laberinto de Manglares (M5-29)")]
        public static void BuildW5_3()
        {
            var level = GreyboxLevel.Create("W5-3", "L_W5_3_LaberintoManglares");
            level.StartPath(Vector3.zero, 0f).Straight(70f, 14f);                     // entrada
            float fork1 = level.Length;
            level.Straight(110f, 14f);                                                // S1 primera bifurcación triple
            float roots = level.Length;
            level.Arc(45f, 90f, 14f).Straight(90f, 14f);                              // S2 raíces como anclas (giro cerrado)
            float fog = level.Length;
            level.Arc(45f, -90f, 14f).Straight(110f, 14f);                            // S3 zona de niebla
            float join = level.Length;
            level.Arc(45f, -90f, 14f).Straight(80f, 14f);                             // S4 reunión de rutas
            level.Arc(45f, 90f, 14f).Straight(60f, 14f);
            // Ruta rápida (estrecha, recta) y ruta de los mangos (un poco más larga) en la primera bifurcación; atajo secreto
            // que corta las curvas desde las raíces hasta la reunión.
            level.Branch(fork1, roots, 6f, -14f);
            level.Branch(fork1, roots, 6f, 14f);
            level.Branch(roots + 10f, join - 10f, 5f, 0f);
            level.BuildChannel(0.2f);

            level.Section(1, "Bifurcacion", fork1);
            level.Section(2, "Raices", roots);
            level.Section(3, "Niebla", fog);
            level.Section(4, "Reunion", join);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(fork1);
            level.AddCheckpoint(roots);
            level.AddCheckpoint(fog);
            level.AddCheckpoint(join);

            // Bifurcación: una ruta segura (el cauce, ancho y tranquilo), una rápida (a la izquierda, con corriente de rápidos) y la de
            // los mangos (a la derecha, con troncos). Los bots siguen el cauce principal.
            Rapids(level, fork1 + 6f, roots - 6f, 0.6f, -14f, 6f);
            for (int i = 0; i < 4; i++)
                Rock(level, "W5_Mangrove_B", fork1 + 25f + i * 24f, 14f + (i % 2 == 0 ? 1.4f : -1.4f), i * 71f, 0.7f);
            level.Mango(0, fork1 + 55f, 0f, 1f);                                      // ruta segura
            var fast = level.Mango(1, 0f, 0f, 1f);                                    // ruta rápida
            fast.transform.position = level.PointAt(fork1 + 82f, -14f, 1f);
            var rich = level.Mango(2, 0f, 0f, 1f);                                    // ruta de mangos
            rich.transform.position = level.PointAt(fork1 + 60f, 14f, 1f);
            level.PowerUp("P01", 30f, 0f);
            level.PowerUp("P02", fork1 + 20f, 0f);
            level.PowerUp("P01", fork1 + 80f, 0f);

            // S2: raíces de manglar que dejan pasar poco; postes de ancla para el Cuello de Melman.
            for (int i = 0; i < 6; i++)
                Rock(level, i % 2 == 0 ? "W5_Mangrove_A" : "W5_Mangrove_B", roots + 10f + i * 14f, i % 2 == 0 ? -5.2f : 5.2f, i * 47f, 0.75f);
            for (int i = 0; i < 3; i++)
                Anchor(level, roots + 20f + i * 28f, i % 2 == 0 ? 2.5f : -2.5f);
            level.PowerUp("P01", roots + 30f, 0f);
            level.PowerUp("P06", roots + 60f, 0f);

            // S3: zona de niebla (visibilidad de 18 m); el atajo secreto empieza tras unas raíces espesas.
            FogSpan(level, fog, join, 18f);
            level.PowerUp("P01", fog + 40f, 0f);

            // S4: reunión; salida.
            level.PowerUp("P01", join + 30f, 0f);
            Anchor(level, roots + 14f, 0f);

            var theme = LevelDresser.With(LevelDresser.W5, new[] { "W5_Mangrove_A", "W5_Mangrove_B", "W5_DesertFlowers", "W5_Tumbleweed" }, 0.8f,
                new Color(0.5f, 0.32f, 0.22f));
            LevelDresser.Dress(level, theme, 53, level.PointAt(roots, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W5-3");
            level.Finish();
            Debug.Log($"M5-29 W5-3 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W5-4 La Represa
        [MenuItem("Gas Safari/Setup/Greybox W5-4 La Represa (M5-30)")]
        public static void BuildW5_4()
        {
            var level = GreyboxLevel.Create("W5-4", "L_W5_4_Represa");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 14f);                    // S1 canal de la represa
            float locks = level.Length;
            level.Straight(120f, 14f);                                                // S2 esclusas
            float gates = level.Length;
            level.Straight(130f, 14f);                                                // S3 compuertas gigantes
            float spill = level.Length;
            level.Straight(50f, 14f);                                                 // S4 aliviadero: caída de 10 m con la oleada
            level.Straight(36f, 14f, -10f).Straight(60f, 14f);
            level.BuildChannel(0.3f);

            level.Section(1, "Canal", 0f);
            level.Section(2, "Esclusas", locks);
            level.Section(3, "Compuertas", gates);
            level.Section(4, "Aliviadero", spill);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(locks);
            level.AddCheckpoint(gates);
            level.AddCheckpoint(spill);

            // S1: canal tranquilo entre muros de la represa.
            level.PowerUp("P01", 30f, 0f);
            level.PowerUp("P04", 70f, 0f);

            // S2: esclusas = compuertas pequeñas en fila (oleada débil): cada una abre unos segundos y cierra.
            for (int i = 0; i < 3; i++)
                DamGate(level, locks + 22f + i * 36f, 0.3f, i * 1.5f);
            // Mango en una esclusa alta: una plataforma sobre el muro.
            var lockTop = level.Block("EsclusaAlta", level.PointAt(locks + 58f, 8.4f, 1.8f), new Vector3(5f, 0.6f, 5f), ConcreteColor,
                Quaternion.LookRotation(level.DirectionAt(locks + 58f)).eulerAngles.y);
            lockTop.layer = GameLayers.Environment;
            level.WallGap(locks + 54f, locks + 62f);
            level.Mango(0, locks + 58f, 8.4f, 2.6f);
            level.PowerUp("P01", locks + 10f, 0f);
            level.PowerUp("P04", locks + 100f, 0f);

            // S3: compuertas gigantes: cada una libera una oleada fuerte (se sube con la ola o se espera).
            DamGate(level, gates + 24f, 1f, 0f);
            DamGate(level, gates + 84f, 1f, 3f);
            level.Mango(1, gates + 52f, 0f, 1.4f);                                    // en la cresta de la primera oleada
            level.PowerUp("P01", gates + 8f, 0f);
            level.PowerUp("P01", gates + 110f, 0f);

            // S4: aliviadero: borde (H17) y caída de 10 m; torre de control con el mango escondido.
            var edge = level.Place(HZ + "H17_Waterfall.prefab", spill + 30f, 0f);
            edge.name = "BordeAliviadero";
            EditorUtility.SetDirty(edge.GetComponent<WaterfallEdge>());
            DamGate(level, spill + 8f, 1f, 1f);
            Rock(level, "W5_CanyonRock_A", spill + 20f, 9.5f, 0f, 1.6f);
            level.WallGap(spill + 64f, spill + 74f);
            Alcove(level, spill + 69f, 1f);
            level.Mango(2, spill + 69f, 8.4f, 1.2f);                                  // torre de control (escondido)
            Anchor(level, spill + 56f, 0f);

            var theme = LevelDresser.With(LevelDresser.W5, LevelDresser.W5.Decor, 0.9f, new Color(0.7f, 0.4f, 0.25f));
            theme.Landmark = "W5_Dam";
            LevelDresser.Dress(level, theme, 54, level.PointAt(gates, 60f), -90f);
            W2StarShots.Apply(level.Scene, "W5-4");
            level.Finish();
            Debug.Log($"M5-30 W5-4 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W5-5 El Megatobogán Final (jefe)
        [MenuItem("Gas Safari/Setup/Greybox W5-5 El Megatobogán Final (M5-31)")]
        public static void BuildW5_5()
        {
            var level = GreyboxLevel.Create("W5-5", "L_W5_5_MegatoboganJefe");
            level.StartPath(Vector3.zero, 0f).Straight(70f, 14f);                     // S1 salida en la montaña
            level.Straight(40f, 14f, -4f);
            float bends = level.Length;
            level.Arc(45f, 90f, 14f, -5f).Straight(30f, 14f, -2f);                    // S2 curvas peraltadas
            level.Arc(45f, -90f, 14f, -5f).Straight(30f, 14f, -2f);
            level.Arc(45f, 90f, 14f, -5f).Straight(20f, 14f, -2f);
            float tunnel = level.Length;
            level.Straight(30f, 14f, -3f).Straight(110f, 14f, -4f);                   // S3 túnel oscuro
            float geysers = level.Length;
            level.Straight(60f, 14f, -3f).Straight(60f, 14f);                         // S4 géiseres y rampa
            float chase = level.Length;
            level.Straight(80f, 14f, -4f).Straight(110f, 14f, -4f);                   // S5 el cocodrilo muerde la cola
            float sea = level.Length;
            level.Straight(60f, 14f, -3f).Straight(60f, 14f);                         // S6 llegada al mar y al barco
            level.BuildChannel(0.4f);

            level.Section(1, "Montana", 0f);
            level.Section(2, "Curvas", bends);
            level.Section(3, "Tunel", tunnel);
            level.Section(4, "Geiseres", geysers);
            level.Section(5, "Cocodrilo", chase);
            level.Section(6, "Mar", sea);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(bends);
            level.AddCheckpoint(tunnel);
            level.AddCheckpoint(geysers);
            level.AddCheckpoint(chase);
            level.AddCheckpoint(sea);

            // S1: salida en lo alto del tobogán; torre del tobogán de hito.
            level.PowerUp("P01", 25f, 0f);
            level.PowerUp("P04", 80f, 0f);

            // S2: curvas peraltadas: el mango de la curva exterior (a un costado, riesgo con la velocidad).
            var outer = level.Mango(0, 0f, 0f, 1f);
            outer.transform.position = level.PointAt(bends + 52f, -6.2f, 1f);
            level.PowerUp("P01", bends + 20f, 0f);
            level.PowerUp("P04", bends + 100f, 0f);

            // S3: túnel oscuro (visibilidad de 8 m) con el mango dentro.
            DarkTunnel(level, tunnel, geysers, 8f);
            level.Mango(1, tunnel + 90f, 3.5f, 1f);
            level.PowerUp("P01", tunnel + 10f, 0f);
            level.PowerUp("P03", tunnel + 70f, 0f);

            // S4: géiseres como trampolín y una rampa final de salto sobre la poza.
            for (int i = 0; i < 2; i++)
            {
                var geyser = level.Place(HZ + "H19_Geyser.prefab", geysers + 25f + i * 30f, 0f);
                geyser.name = "Geiser_" + i;
                geyser.GetComponent<Geyser>().SetPhase(i * 1.1f);
                EditorUtility.SetDirty(geyser.GetComponent<Geyser>());
            }
            level.PowerUp("P01", geysers + 12f, 0f);
            level.PowerUp("P03", geysers + 90f, 0f);

            // S5: el cocodrilo gigante (H23) arranca detrás de la balsa al entrar en esta zona y la persigue por el cauce.
            var croc = level.Place(HZ + "H23_GiantCroc.prefab", chase + 4f, 0f);
            croc.name = "CocodriloGigante";
            var chaser = croc.GetComponent<GiantCrocChaser>();
            var waypoints = new List<Vector3>();
            for (float d = chase - 28f; d <= level.Length - 6f; d += 10f)
                waypoints.Add(level.PointAt(d, 0f, 0.4f));
            var body = croc.transform.Find("Cocodrilo");
            body.position = waypoints[0];
            chaser.Configure(body, waypoints.ToArray(), 0.9f, 5f);
            EditorUtility.SetDirty(chaser);
            level.PowerUp("P01", chase + 30f, 0f);
            level.PowerUp("P04", chase + 120f, 0f);
            level.PowerUp("P01", chase + 170f, 0f);

            // S6: llegada al mar: rampa final con el trampolín (H27) y el barco al que saltan; el cocodrilo cierra la boca detrás.
            level.Place(HZ + "H27_Jet.prefab", sea + 50f, 0f);
            level.Mango(2, sea + 62f, 0f, 3.2f);                                       // en la rampa final
            level.PowerUp("P01", sea + 8f, 0f);
            level.PowerUp("P03", sea + 30f, 0f);
            // El barco queda en la orilla derecha, junto a la llegada (no en el cauce: la meta está en el agua).
            var ship = level.Block("Barco", level.PointAt(level.Length - 12f, 13f, 0.8f), new Vector3(10f, 3f, 16f), ConcreteColor,
                Quaternion.LookRotation(level.DirectionAt(level.Length - 12f)).eulerAngles.y);
            ship.layer = GameLayers.Environment;
            Anchor(level, sea + 56f, 0f);
            CrocFinale(level, sea + 40f, chaser);

            var theme = LevelDresser.With(LevelDresser.W5, LevelDresser.W5.Decor, 0.9f, new Color(0.72f, 0.4f, 0.22f));
            theme.Landmark = "W5_SlideTower";
            LevelDresser.Dress(level, theme, 55, level.PointAt(0f, 45f), -90f);
            W2StarShots.Apply(level.Scene, "W5-5");
            level.Finish();
            Debug.Log($"M5-31 W5-5 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- utilidades
        // Rápidos (H21) entre dos distancias del cauce (o de un ramal con 'lateral'): zona con corriente hacia adelante.
        private static void Rapids(GreyboxLevel level, float from, float to, float intensity, float lateral = 0f, float width = 0f)
        {
            float mid = (from + to) * 0.5f;
            var zone = level.Place(HZ + "H21_Rapids.prefab", mid, lateral);
            zone.name = "Rapidos";
            float w = width > 0f ? width + 2f : level.WidthAt(mid) + 2f;
            zone.transform.localScale = new Vector3(w / 14f, 1f, (to - from) / 10f);
            zone.GetComponent<RapidsZone>().Configure(intensity);
            EditorUtility.SetDirty(zone.GetComponent<RapidsZone>());
        }

        private static GameObject Rock(GreyboxLevel level, string fbx, float d, float lateral, float yaw, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.name = fbx;
            go.transform.SetPositionAndRotation(level.PointAt(d, lateral, -0.3f), Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            PropColliders.AddSolidBox(go);
            return go;
        }

        // Puente roto de vista, a 5 m sobre el agua (la balsa pasa por debajo).
        private static void Overpass(GreyboxLevel level, float d)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W5_BrokenBridge.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.name = "PuenteRoto";
            go.transform.SetPositionAndRotation(level.PointAt(d, 0f, 5f), Quaternion.LookRotation(level.DirectionAt(d)) * Quaternion.Euler(0f, 90f, 0f));
        }

        // Compuerta de represa (H37) con su zona de oleada aguas abajo.
        private static void DamGate(GreyboxLevel level, float d, float surge, float phase)
        {
            var gate = level.Place(HZ + "H37_DamGate.prefab", d, 0f);
            gate.name = "Compuerta";
            var script = gate.GetComponent<DamGate>();
            var so = new SerializedObject(script);
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("surgeScale").floatValue = surge;
            so.ApplyModifiedPropertiesWithoutUndo();
            gate.transform.localScale = new Vector3(level.WidthAt(d) / 14f, 1f, 1f);
        }

        // Abismo (H22): trigger bajo el hueco con el puente de boyas de ayuda.
        private static void Abyss(GreyboxLevel level, float from, float to, float bridgeWidth)
        {
            float d = (from + to) * 0.5f;
            var go = level.Place(HZ + "H22_Abyss.prefab", d, 0f, 0f, -33f);
            go.name = "Abismo";
            go.GetComponent<BoxCollider>().size = new Vector3(level.WidthAt(d) + 40f, 60f, to - from + 20f);
            var abyss = go.GetComponent<GasSafari.Hazards.Abyss>();
            abyss.Configure(level.BuoyBridge(from, to, bridgeWidth));
            EditorUtility.SetDirty(abyss);
        }

        // Zonas de niebla (visibilidad 'visibility' m, sin apagón) a lo largo de un tramo.
        private static void FogSpan(GreyboxLevel level, float from, float to, float visibility)
        {
            for (float d = from + 20f; d < to; d += 40f)
            {
                var zone = level.Place(HZ + "H18_Dark.prefab", d, 0f, 0f);
                zone.name = "ZonaNiebla";
                zone.GetComponent<BoxCollider>().size = new Vector3(level.WidthAt(d) + 6f, 12f, 40f);
                zone.GetComponent<DarkZone>().Configure(visibility, new Light[0], 0f);
                EditorUtility.SetDirty(zone.GetComponent<DarkZone>());
            }
        }

        private static void DarkTunnel(GreyboxLevel level, float from, float to, float visibility)
        {
            FogSpan(level, from, to, visibility);
            var arch = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W5_NaturalArch.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(arch, level.Scene);
            go.name = "ArcoTunel";
            go.transform.SetPositionAndRotation(level.PointAt(from + 4f, 0f, -0.3f), Quaternion.LookRotation(level.DirectionAt(from + 4f)));
            go.transform.localScale = Vector3.one * 0.9f;
        }

        // Momento estrella final: al llegar al salto, el cocodrilo cierra la boca detrás de la balsa.
        private static void CrocFinale(GreyboxLevel level, float d, GiantCrocChaser chaser)
        {
            var zone = new GameObject("ZonaFinalCocodrilo");
            SceneManager.MoveGameObjectToScene(zone, level.Scene);
            zone.layer = GameLayers.Trigger;
            zone.transform.SetPositionAndRotation(level.PointAt(d, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(d)));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(d) + 2f, 4f, 3f);
            zone.AddComponent<CrocFinaleZone>().Configure(chaser);
        }

        // Nicho en la pared derecha (mango escondido): fondo y lados sólidos, la pared tiene hueco (WallGap en el nivel).
        private static void Alcove(GreyboxLevel level, float d, float side)
        {
            float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
            level.Block("Nicho_Fondo", level.PointAt(d, side * 10.6f), new Vector3(1f, 3f, 7f), RockColor, yaw);
            level.Block("Nicho_Lado1", level.PointAt(d - 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), RockColor, yaw);
            level.Block("Nicho_Lado2", level.PointAt(d + 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), RockColor, yaw);
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
