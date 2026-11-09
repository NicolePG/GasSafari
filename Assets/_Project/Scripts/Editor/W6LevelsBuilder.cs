using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 6 "Reino de Julien" (docs/04): fiesta nocturna, magenta #F72585 y lima #B5E48C.
    // Cada nivel es una función de este script: todo lo que arma cuelga de _Generated y correrlo de nuevo lo rehace.
    public static class W6LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W6/";
        private const float WHIRL_PREFAB_RADIUS = 8f;
        private static readonly Color Magenta = new Color(0.97f, 0.15f, 0.52f);
        private static readonly Color Lime = new Color(0.71f, 0.89f, 0.55f);
        private static readonly Color StageColor = new Color(0.3f, 0.1f, 0.4f);
        private static readonly Color CaveColor = new Color(0.32f, 0.12f, 0.36f);

        // ---------------------------------------------------------------- W6-1 Fiesta en Espejo
        [MenuItem("Gas Safari/Setup/Greybox W6-1 Fiesta en Espejo (M5-33)")]
        public static void BuildW6_1()
        {
            var level = GreyboxLevel.Create("W6-1", "L_W6_1_FiestaEspejo");
            level.StartPath(Vector3.zero, 0f).Straight(60f, 14f);
            level.Arc(40f, -90f, 14f).Straight(50f, 14f);                             // S1 puerto (tramo de W1 en espejo: gira a la izquierda)
            float forest = level.Length;
            level.Straight(40f, 14f);
            float bendFrom = level.Length;
            level.Arc(40f, 90f, 14f);                                                 // S2 selva (tramo de W3 en espejo: gira a la derecha)
            float bendTo = level.Length;
            level.Straight(60f, 14f);
            float rhythm = level.Length;
            level.Straight(150f, 14f);                                                // S3 ritmo
            float stage = level.Length;
            level.Straight(110f, 16f);                                                // S4 escenario del DJ
            level.Branch(bendFrom, bendTo, 5f, 0f);                                   // atajo oculto: la recta que corta la curva de la selva
            level.BuildChannel(0.25f);

            level.Section(1, "Puerto", 0f);
            level.Section(2, "Selva", forest);
            level.Section(3, "Ritmo", rhythm);
            level.Section(4, "Escenario", stage);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(forest);
            level.AddCheckpoint(rhythm);
            level.AddCheckpoint(stage);

            // Reloj de compás a 120 BPM (propio, sin pista) y bono de pedo en el pulso.
            RhythmDirector(level, 120f);

            // S1: puerto en espejo: compuertas temporizadas (H03) y tambores en las orillas.
            Gate(level, 24f, 3f);
            Gate(level, 56f, 2.5f);
            Gate(level, forest - 24f, 3f);
            Drums(level, 12f, forest - 10f, 6);
            level.Mango(0, 112f, 5.2f, 1f);                                           // ruta: en el tramo espejo del puerto
            level.PowerUp("P01", 18f, 0f);
            level.PowerUp("P04", 92f, 0f);

            // S2: selva en espejo: fosas (H14) en las orillas y dos barras al compás.
            for (int i = 0; i < 4; i++)
                level.Place(HZ + "H14_Pit.prefab", forest + 14f + i * 22f, i % 2 == 0 ? 4.8f : -4.8f).name = "Fosa_" + i;
            RhythmBar(level, forest + 110f, true, 8f);
            RhythmBar(level, forest + 134f, false, 8f);
            var hidden = level.Mango(2, bendFrom, 0f, 1f);                            // escondido: a mitad de la recta oculta (dentro de la curva)
            hidden.transform.position = (level.PointAt(bendFrom) + level.PointAt(bendTo)) * 0.5f + Vector3.up;
            level.PowerUp("P01", forest + 20f, 0f);
            level.PowerUp("P04", forest + 100f, 0f);
            level.PowerUp("P01", forest + 60f, 0f);

            // S3: ritmo: barras al compás, chorros que brotan en los pulsos y tambores.
            for (int i = 0; i < 6; i++)
                RhythmBar(level, rhythm + 16f + i * 22f, i % 2 == 0, 8f);
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                level.Place(HZ + "H16_Jet.prefab", rhythm + 38f + i * 44f, side * 5.2f, side > 0f ? 180f : 0f).name = "ChorroRitmo_" + i;
            }
            Drums(level, rhythm + 6f, stage - 6f, 8);
            level.PowerUp("P01", rhythm + 8f, 0f);
            level.PowerUp("P03", rhythm + 70f, 0f);
            level.PowerUp("P01", rhythm + 130f, 0f);

            // S4: escenario del DJ: la plataforma lateral con el mango y el arco de luces.
            Prop(level, "W6_Stage", stage + 56f, 11.5f, 270f, 1f).name = "EscenarioDJ";
            level.Block("PlataformaDJ", level.PointAt(stage + 56f, 4.4f, 0.3f), new Vector3(3f, 1f, 3f), StageColor,
                Quaternion.LookRotation(level.DirectionAt(stage + 56f)).eulerAngles.y).layer = GameLayers.Environment;
            level.Mango(1, stage + 56f, 3.0f, 1.9f);                                  // riesgo: sobre la plataforma lateral del DJ
            Prop(level, "W6_LightTruss", stage + 12f, 0f, 90f, 1f, 6.5f, false).name = "ArcoLuces";
            level.PowerUp("P01", stage + 14f, 0f);
            Anchor(level, stage + 40f, 0f);

            LevelDresser.Dress(level, LevelDresser.W6, 61, level.PointAt(stage + 56f, 40f), -90f);
            W2StarShots.Apply(level.Scene, "W6-1");
            level.Finish();
            Debug.Log($"M5-33 W6-1 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W6-2 Noche sin Luz
        [MenuItem("Gas Safari/Setup/Greybox W6-2 Noche sin Luz (M5-34)")]
        public static void BuildW6_2()
        {
            FireflyMaterials.Clear();
            var level = GreyboxLevel.Create("W6-2", "L_W6_2_NocheSinLuz");
            level.StartPath(Vector3.zero, 0f).Straight(100f, 14f);                    // S1 entrada a oscuras
            float pits = level.Length;
            level.Straight(60f, 14f).Arc(40f, 90f, 14f).Straight(60f, 14f);           // S2 fosas
            float whirl = level.Length;
            level.Straight(160f, 16f);                                                // S3 remolinos
            float cave = level.Length;
            level.Arc(40f, -90f, 14f).Straight(80f, 14f);                             // S4 cueva y salida
            level.BuildChannel(0.25f);

            level.Section(1, "Entrada", 0f);
            level.Section(2, "Fosas", pits);
            level.Section(3, "Remolinos", whirl);
            level.Section(4, "Cueva", cave);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(pits);
            level.AddCheckpoint(whirl);
            level.AddCheckpoint(cave);

            // Oscuridad casi total en todo el nivel (visibilidad 6 m, H18); las luciérnagas móviles son la única luz.
            DarkSpan(level, 0f, level.Length, 6f);
            for (int i = 0; i < 14; i++)
                Firefly(level, 18f + i * 42f, (i % 3 - 1) * 3.2f, i % 2 == 0 ? Lime : Magenta, 1f, 0.4f * i);
            // La luciérnaga gigante lleva el mango de la ruta y vuela sobre el cauce.
            var giant = Firefly(level, 62f, 0f, Lime, 2.2f, 0f, 1.6f);
            level.Mango(0, 62f, 0f, 1.2f).transform.SetParent(giant.transform, true);
            // Tres luciérnagas más alrededor del segundo remolino (momento estrella): el resto están cada 42 m y no lo alcanzan.
            Firefly(level, whirl + 72f, -2f, Lime, 1f, 1.1f);
            Firefly(level, whirl + 85f, 1f, Magenta, 1f, 2.3f);
            Firefly(level, whirl + 98f, -2f, Lime, 1f, 3.4f);
            level.PowerUp("P01", 24f, 0f);
            level.PowerUp("P06", 80f, 0f);

            // S2: fosas (H14) en las orillas, alternando lados; el aviso es el movimiento del arbusto.
            for (int i = 0; i < 5; i++)
                level.Place(HZ + "H14_Pit.prefab", pits + 16f + i * 30f, i % 2 == 0 ? 4.8f : -4.8f).name = "Fosa_" + i;
            level.PowerUp("P01", pits + 10f, 0f);
            level.PowerUp("P04", pits + 90f, 0f);
            level.PowerUp("P01", pits + 60f, 0f);
            level.PowerUp("P01", pits + 150f, 0f);

            // S3: tres remolinos (H09), con el mango en el centro del segundo.
            Pool(level, "A", whirl + 30f, -3.5f, 6f);
            Pool(level, "B", whirl + 85f, 3.5f, 7f);
            Pool(level, "C", whirl + 135f, -2f, 6f);
            level.Mango(1, whirl + 85f, 3.5f, 1f);                                    // riesgo: en el ojo del remolino
            level.PowerUp("P01", whirl + 8f, 0f);
            level.PowerUp("P05", whirl + 60f, 0f);
            level.PowerUp("P01", whirl + 110f, 0f);

            // S4: cueva en la pared derecha con el mango escondido; salida.
            float hole = cave + 78f;
            level.WallGap(hole - 3f, hole + 3f);
            Alcove(level, hole, 1f);
            level.Mango(2, hole, 8.4f, 1.2f);
            level.PowerUp("P01", cave + 30f, 0f);
            level.PowerUp("P04", cave + 70f, 0f);
            Anchor(level, whirl + 82f, 0f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W6, LevelDresser.W6.Decor, 0.25f, new Color(0.08f, 0.02f, 0.12f)), 62,
                level.PointAt(whirl, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W6-2");
            level.Finish();
            Debug.Log($"M5-34 W6-2 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W6-3 Todo a Contracorriente
        [MenuItem("Gas Safari/Setup/Greybox W6-3 Todo a Contracorriente (M5-35)")]
        public static void BuildW6_3()
        {
            var level = GreyboxLevel.Create("W6-3", "L_W6_3_Contracorriente");
            level.StartPath(Vector3.zero, 0f).Straight(130f, 14f);                    // S1 contra la corriente
            float rapids = level.Length;
            level.Straight(130f, 14f);                                                // S2 rápidos inversos
            float gates = level.Length;
            level.Straight(130f, 14f);                                                // S3 compuertas
            float falls = level.Length;
            level.Straight(130f, 14f);                                                // S4 cascada
            float finale = level.Length;
            level.Straight(120f, 14f);                                                // S5 final
            // La corriente va en contra en todo el nivel (aceleración negativa a lo largo del cauce); los remansos la anulan.
            level.FlowZone(52f, 88f, 0.1f);
            level.BuildChannel(-0.7f);

            level.Section(1, "Contracorriente", 0f);
            level.Section(2, "RapidosInversos", rapids);
            level.Section(3, "Compuertas", gates);
            level.Section(4, "Cascada", falls);
            level.Section(5, "Final", finale);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(rapids);
            level.AddCheckpoint(gates);
            level.AddCheckpoint(falls);
            level.AddCheckpoint(finale);

            // S1: remanso detrás de una roca grande (mango de la ruta) para recargar gas.
            Prop(level, "W6_Rock_C", 70f, 5.2f, 20f, 1.3f).name = "RocaRemanso";
            level.Mango(0, 76f, 2.0f, 1f);                                         // a sotavento de la roca, dentro del remanso
            level.PowerUp("P01", 24f, 0f);
            level.PowerUp("P01", 100f, 0f);
            RapidsAgainst(level, 100f, 125f, 0.1f);

            // S2: rápidos en contra (H21 girado 180°) entre rocas.
            RapidsAgainst(level, rapids + 20f, rapids + 110f, 0.2f);
            for (int i = 0; i < 4; i++)
                Prop(level, i % 2 == 0 ? "W6_Rock_A" : "W6_Rock_B", rapids + 24f + i * 24f, i % 2 == 0 ? -4.4f : 4.4f, i * 61f, 0.9f);
            level.PowerUp("P01", rapids + 10f, 0f);
            level.PowerUp("P04", rapids + 60f, 0f);
            level.PowerUp("P01", rapids + 118f, 0f);

            // S3: tres compuertas temporizadas; el mango queda justo tras la segunda.
            Gate(level, gates + 20f, 3f);
            Gate(level, gates + 60f, 2.5f);
            Gate(level, gates + 100f, 3f);
            level.Mango(1, gates + 68f, 0f, 1f);                                      // riesgo: tras la compuerta
            level.PowerUp("P01", gates + 8f, 0f);
            level.PowerUp("P03", gates + 40f, 0f);
            level.PowerUp("P01", gates + 82f, 0f);

            // S4: cascada en la pared derecha con un saliente en la cima: el mango exige un Súper Pedo hacia arriba.
            float cascade = falls + 70f;
            float yaw = Quaternion.LookRotation(level.DirectionAt(cascade)).eulerAngles.y;
            level.WallGap(cascade - 4f, cascade + 4f);
            var curtain = level.Block("CortinaCascada", level.PointAt(cascade, 6.8f, 1.8f), new Vector3(0.15f, 3.5f, 8f), new Color(0.75f, 0.95f, 1f), yaw);
            Object.DestroyImmediate(curtain.GetComponent<Collider>());
            var ledge = level.Block("SalienteCima", level.PointAt(cascade, 5.2f, 2.7f), new Vector3(4f, 0.8f, 5f), StageColor, yaw);
            ledge.layer = GameLayers.Environment;
            level.Mango(2, cascade, 5.2f, 3.6f);                                      // en la cima de la cascada (Súper Pedo hacia arriba)
            level.Place(HZ + "H36_GoldGasGeyser.prefab", falls + 40f, 0f).name = "H36_GeiserGasDorado";   // llena el medidor antes de subir
            for (int i = 0; i < 3; i++)                                                // eslalon de rocas antes de la cascada
                Prop(level, i % 2 == 0 ? "W6_Rock_A" : "W6_Rock_C", falls + 10f + i * 14f, i % 2 == 0 ? 4.2f : -4.2f, i * 71f, 0.9f);
            level.PowerUp("P04", falls + 50f, 0f);
            level.PowerUp("P01", falls + 12f, 0f);
            level.PowerUp("P01", falls + 100f, 0f);
            Anchor(level, falls + 62f, 0f);

            // S5: final: rocas y el escenario de cierre.
            for (int i = 0; i < 3; i++)
                Prop(level, "W6_Rock_B", finale + 26f + i * 30f, i % 2 == 0 ? 4.2f : -4.2f, i * 83f, 0.9f);
            level.PowerUp("P01", finale + 10f, 0f);
            level.PowerUp("P04", finale + 60f, 0f);
            RapidsAgainst(level, finale + 70f, finale + 105f, 0.3f);                    // sprint final contra la corriente

            LevelDresser.Dress(level, LevelDresser.W6, 63, level.PointAt(falls, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W6-3");
            level.Finish();
            Debug.Log($"M5-35 W6-3 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W6-4 Lluvia de Cocos
        [MenuItem("Gas Safari/Setup/Greybox W6-4 Lluvia de Cocos (M5-36)")]
        public static void BuildW6_4()
        {
            var level = GreyboxLevel.Create("W6-4", "L_W6_4_LluviaCocos");
            level.StartPath(Vector3.zero, 0f).Straight(150f, 14f);                    // S1 palmeral
            float rain = level.Length;
            level.Straight(60f, 14f).Arc(45f, 90f, 14f).Straight(40f, 14f);           // S2 el centro de la lluvia
            float beasts = level.Length;
            level.Straight(150f, 14f);                                                // S3 cocodrilos y pinchos bajo la lluvia
            float downpour = level.Length;
            level.Straight(130f, 14f);                                                // S4 aguacero final
            level.BuildChannel(0.25f);

            level.Section(1, "Palmeral", 0f);
            level.Section(2, "Lluvia", rain);
            level.Section(3, "Fieras", beasts);
            level.Section(4, "Aguacero", downpour);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(rain);
            level.AddCheckpoint(beasts);
            level.AddCheckpoint(downpour);

            // S1: algunos cocos aislados (cada 2.4 s) entre las palmeras.
            CoconutRain(level, 30f, 140f, 2.4f, "LluviaCocos_1");
            for (int i = 0; i < 5; i++)
                Prop(level, i % 3 == 0 ? "W6_Palm_A" : i % 3 == 1 ? "W6_Palm_B" : "W6_Palm_C", 30f + i * 26f, i % 2 == 0 ? 7.6f : -7.6f, i * 53f, 1f);
            level.PowerUp("P01", 20f, 0f);
            level.PowerUp("P04", 90f, 0f);

            // S2: el centro de la lluvia: dos lluvias que se cruzan (cada 1.5 s); el mango queda en medio.
            CoconutRain(level, rain + 4f, rain + 70f, 1.5f, "LluviaCocos_2", 0f);
            CoconutRain(level, rain + 40f, rain + 110f, 1.6f, "LluviaCocos_3", 0.8f);
            level.Mango(0, rain + 55f, 0f, 1f);                                       // ruta: en el centro de la lluvia
            level.PowerUp("P01", rain + 8f, 0f);
            level.PowerUp("P04", rain + 60f, 0f);
            level.PowerUp("P01", rain + 100f, 0f);

            // S3: cocodrilos dormidos (H15), pinchos (H10) y lluvia suave; el mango va sobre el lomo del primer cocodrilo.
            for (int i = 0; i < 4; i++)
            {
                float d = beasts + 24f + i * 34f;
                float lateral = i % 2 == 0 ? 4.4f : -4.4f;
                level.Place(HZ + "H15_Crocodile.prefab", d, lateral, 90f).name = "CocodriloDormido_" + i;
                if (i == 0)
                    level.Mango(1, d, lateral > 0f ? 3.4f : -3.4f, 1.2f);                                 // riesgo: sobre el cocodrilo
            }
            for (float d = beasts + 40f; d < beasts + 140f; d += 34f)
                level.Place(HZ + "H10_Spikes.prefab", d, d % 68f < 34f ? -5.4f : 5.4f, d * 3f).name = "Pinchos";
            CoconutRain(level, beasts + 10f, beasts + 140f, 2.2f, "LluviaCocos_4", 0.4f);
            level.PowerUp("P01", beasts + 10f, 0f);
            level.PowerUp("P04", beasts + 70f, 0f);
            level.PowerUp("P01", beasts + 130f, 0f);

            // S4: aguacero final; el mango queda tras una palmera pegada a la orilla.
            CoconutRain(level, downpour + 6f, downpour + 110f, 1.4f, "LluviaCocos_5", 0f);
            CoconutRain(level, downpour + 30f, downpour + 120f, 1.2f, "LluviaCocos_6", 0.7f);
            Prop(level, "W6_Palm_A", downpour + 70f, 5.8f, 0f, 1.5f).name = "PalmeraGrande";
            level.Mango(2, downpour + 72f, 4.6f, 1f);                                 // escondido: tras la palmera
            level.PowerUp("P01", downpour + 12f, 0f);
            level.PowerUp("P04", downpour + 60f, 0f);
            Anchor(level, downpour + 90f, 0f);

            LevelDresser.Dress(level, LevelDresser.W6, 64, level.PointAt(rain, 55f), -90f);
            W2StarShots.Apply(level.Scene, "W6-4");
            level.Finish();
            Debug.Log($"M5-36 W6-4 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W6-5 La Corona del Rey (jefe final secreto)
        [MenuItem("Gas Safari/Setup/Greybox W6-5 La Corona del Rey (M5-37)")]
        public static void BuildW6_5()
        {
            var level = GreyboxLevel.Create("W6-5", "L_W6_5_CoronaReyJefe");
            level.StartPath(Vector3.zero, 0f).Straight(150f, 14f);                    // S1 puerto (W1)
            float storm = level.Length;
            level.Straight(150f, 14f);                                                // S2 tormenta (W2)
            float jungle = level.Length;
            level.Straight(150f, 14f);                                                // S3 selva (W3)
            float cave = level.Length;
            // S4 cueva (W4): abismo de 15 m con la receta de W4-3 (géiser dorado H36 antes, rampa de despegue, marca SuperFart).
            level.Straight(50f, 14f);
            level.Straight(14f, 14f, 2f);
            float abyss1 = level.Length;
            level.Straight(15f, 14f, -2f);
            float abyss1End = level.Length;
            level.Straight(60f, 14f);
            float canyon = level.Length;
            // S5 cañón (W5): rápidos y un segundo abismo de 18 m con la misma receta.
            level.Straight(60f, 14f);
            level.Straight(40f, 14f);
            level.Straight(14f, 14f, 2f);
            float abyss2 = level.Length;
            level.Straight(18f, 14f, -2f);
            float abyss2End = level.Length;
            level.Straight(40f, 14f);
            float finale = level.Length;
            level.Straight(160f, 16f);                                                // S6 final rítmico
            level.WaterGap(abyss1, abyss1End);
            level.WaterGap(abyss2, abyss2End);
            level.NoBaseWater();
            level.BuildChannel(0.25f);

            level.Section(1, "Puerto", 0f);
            level.Section(2, "Tormenta", storm);
            level.Section(3, "Selva", jungle);
            level.Section(4, "Cueva", cave);
            level.Section(5, "Canon", canyon);
            level.Section(6, "Ritmo", finale);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(storm);
            level.AddCheckpoint(jungle);
            level.AddCheckpoint(cave);
            level.AddCheckpoint(canyon);
            level.AddCheckpoint(finale);

            RhythmDirector(level, 120f);

            // S1: puerto: compuertas y tambores.
            Gate(level, 28f, 3f);
            Gate(level, 66f, 2.5f);
            Gate(level, 108f, 3f);
            Drums(level, 12f, 140f, 5);
            level.PowerUp("P01", 18f, 0f);
            level.PowerUp("P04", 90f, 0f);

            // S2: tormenta: remolinos.
            Pool(level, "A", storm + 40f, -3.5f, 6f);
            Pool(level, "B", storm + 100f, 3.5f, 7f);
            level.Mango(0, storm + 70f, 0f, 1f);                                      // primer tercio: entre los dos remolinos
            level.PowerUp("P01", storm + 10f, 0f);
            level.PowerUp("P05", storm + 70f, 0f);
            level.PowerUp("P01", storm + 120f, 0f);

            // S3: selva: fosas y dos cocodrilos dormidos.
            for (int i = 0; i < 3; i++)
                level.Place(HZ + "H14_Pit.prefab", jungle + 20f + i * 30f, i % 2 == 0 ? 4.8f : -4.8f).name = "Fosa_" + i;
            for (int i = 0; i < 2; i++)
                level.Place(HZ + "H15_Crocodile.prefab", jungle + 100f + i * 28f, i % 2 == 0 ? 4.4f : -4.4f, 90f).name = "CocodriloDormido_" + i;
            level.PowerUp("P01", jungle + 10f, 0f);
            level.PowerUp("P04", jungle + 70f, 0f);
            level.PowerUp("P01", jungle + 50f, 0f);
            level.PowerUp("P03", jungle + 130f, 0f);

            // S4: cueva: primer Súper Pedo obligatorio.
            var gold1 = level.Place(HZ + "H36_GoldGasGeyser.prefab", abyss1 - 30f, 0f);
            gold1.name = "H36_GeiserGasDorado_1";
            Abyss(level, abyss1, abyss1End, 8f);
            level.Hint(Bots.BotHintType.SuperFart, abyss1 - 1.5f, 0f, 6f);
            var air = level.Mango(1, 0f, 0f, 1f);                                     // segundo tercio: en el aire sobre el abismo
            air.transform.position = level.PointAt((abyss1 + abyss1End) * 0.5f, 0f, 3.6f);
            level.PowerUp("P01", abyss1 - 40f, 0f);
            level.PowerUp("P03", abyss1End + 12f, 0f);
            Anchor(level, abyss1 - 6f, 0f);

            // S5: cañón: rápidos y segundo Súper Pedo obligatorio.
            Rapids(level, canyon + 8f, canyon + 56f, 0.6f);
            Prop(level, "W6_Rock_A", canyon + 24f, 4.2f, 0f, 0.9f);
            Prop(level, "W6_Rock_B", canyon + 42f, -4.2f, 90f, 0.9f);
            var gold2 = level.Place(HZ + "H36_GoldGasGeyser.prefab", abyss2 - 30f, 0f);
            gold2.name = "H36_GeiserGasDorado_2";
            Abyss(level, abyss2, abyss2End, 8f);
            level.Hint(Bots.BotHintType.SuperFart, abyss2 - 1.5f, 0f, 6f);
            level.PowerUp("P01", canyon + 10f, 0f);
            level.PowerUp("P01", abyss2 - 40f, 0f);
            level.PowerUp("P03", abyss2End + 12f, 0f);

            // S6: final rítmico: barras al compás, chorros y tambores; el trono y la corona gigante cierran el nivel.
            for (int i = 0; i < 6; i++)
                RhythmBar(level, finale + 16f + i * 22f, i % 2 == 0, 8f);
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                level.Place(HZ + "H16_Jet.prefab", finale + 36f + i * 40f, side * 5.2f, side > 0f ? 180f : 0f).name = "ChorroRitmo_" + i;
            }
            Drums(level, finale + 6f, level.Length - 6f, 8);
            level.Block("PlataformaTrono", level.PointAt(finale + 140f, 4.4f, 0.3f), new Vector3(3f, 1f, 3f), StageColor,
                Quaternion.LookRotation(level.DirectionAt(finale + 140f)).eulerAngles.y).layer = GameLayers.Environment;
            level.Mango(2, finale + 140f, 3.0f, 1.9f);                                // tercer tercio: sobre la plataforma del trono
            var end = level.PointAt(level.Length, 0f) + level.DirectionAt(level.Length) * 7f;
            var throne = Prop(level, "W6_Throne", level.Length, 0f, 0f, 1.1f);
            throne.transform.position = end;
            throne.transform.rotation = Quaternion.LookRotation(-level.DirectionAt(level.Length));
            level.PowerUp("P01", finale + 10f, 0f);
            level.PowerUp("P04", finale + 70f, 0f);
            level.PowerUp("P01", finale + 40f, 0f);
            level.PowerUp("P03", finale + 100f, 0f);
            level.PowerUp("P01", finale + 120f, 0f);

            var theme = LevelDresser.With(LevelDresser.W6, LevelDresser.W6.Decor, 0.7f, new Color(0.2f, 0.06f, 0.26f));
            theme.Landmark = "W6_GiantCrown";
            LevelDresser.Dress(level, theme, 65, level.PointAt(finale + 80f, 45f), -90f);
            W2StarShots.Apply(level.Scene, "W6-5");
            level.Finish();
            Debug.Log($"M5-37 W6-5 listo: {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- utilidades
        private static GameObject Prop(GreyboxLevel level, string fbx, float d, float lateral, float yaw, float scale, float height = -0.3f, bool solid = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.name = fbx;
            go.transform.SetPositionAndRotation(level.PointAt(d, lateral, height), Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            if (fbx.StartsWith("W6_Palm"))
            {
                // Palmera: solo el tronco es sólido (la caja del modelo incluye las hojas y dejaría un muro invisible al nivel del agua).
                var trunk = go.AddComponent<CapsuleCollider>();
                trunk.radius = 0.5f;
                trunk.height = 4f;
                trunk.center = new Vector3(0f, 2f, 0f);
                go.layer = GameLayers.Environment;
            }
            else if (solid)
            {
                PropColliders.AddSolidBox(go);
            }
            return go;
        }

        // Reloj de compás propio (sin pista de música) y bono de pedo en el pulso.
        private static void RhythmDirector(GreyboxLevel level, float bpm)
        {
            var director = new GameObject("Ritmo");
            SceneManager.MoveGameObjectToScene(director, level.Scene);
            director.AddComponent<RhythmClock>().Configure(bpm, false);
            director.AddComponent<RhythmSyncBonus>();
        }

        // Barra rítmica (H16) que cruza el cauce de un lado a otro en un compás de 'cycle' pulsos.
        private static void RhythmBar(GreyboxLevel level, float d, bool fromLeft, float cycle)
        {
            float half = level.WidthAt(d) * 0.5f - 1.6f;
            var go = level.Place(HZ + "H16_Rhythm.prefab", d, fromLeft ? -half : half, Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y);
            go.name = "BarraRitmo";
            go.GetComponent<RhythmObstacle>().Configure(fromLeft ? Vector3.right : Vector3.left, Mathf.Min(4.5f, half * 2f - 3f), cycle, 0);
        }

        private static void Drums(GreyboxLevel level, float from, float to, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float d = Mathf.Lerp(from, to, count > 1 ? i / (float)(count - 1) : 0f);
                float side = i % 2 == 0 ? 1f : -1f;
                level.Place(HZ + "H16_Drum.prefab", d, side * (level.WidthAt(d) * 0.5f + 2.2f), 0f, 0f, "Props").name = "Tambor_" + i;
            }
        }

        private static void Gate(GreyboxLevel level, float distance, float cycle)
        {
            var gate = level.Place(HZ + "H03_TimedGate.prefab", distance);
            gate.name = "Compuerta";
            var so = new SerializedObject(gate.GetComponent<TimedGate>());
            so.FindProperty("cycleOverride").floatValue = cycle;
            so.FindProperty("phase").floatValue = distance * 0.013f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Pool(GreyboxLevel level, string name, float d, float lateral, float radius)
        {
            var pool = level.Place(HZ + "H09_Whirlpool.prefab", d, lateral);
            pool.name = "Remolino_" + name;
            pool.transform.localScale = Vector3.one * (radius / WHIRL_PREFAB_RADIUS);
            level.Hint(Bots.BotHintType.Whirlpool, d, lateral, radius);
        }

        // Rápidos (H21) a favor de la corriente (como en W5).
        private static void Rapids(GreyboxLevel level, float from, float to, float intensity)
        {
            float mid = (from + to) * 0.5f;
            var zone = level.Place(HZ + "H21_Rapids.prefab", mid, 0f);
            zone.name = "Rapidos";
            zone.transform.localScale = new Vector3((level.WidthAt(mid) + 2f) / 14f, 1f, (to - from) / 10f);
            zone.GetComponent<RapidsZone>().Configure(intensity);
            EditorUtility.SetDirty(zone.GetComponent<RapidsZone>());
        }

        // Rápidos en contra: el prefab girado 180° empuja hacia atrás.
        private static void RapidsAgainst(GreyboxLevel level, float from, float to, float intensity)
        {
            float mid = (from + to) * 0.5f;
            var zone = level.Place(HZ + "H21_Rapids.prefab", mid, 0f, 180f);
            zone.name = "RapidosInversos";
            zone.transform.localScale = new Vector3((level.WidthAt(mid) + 2f) / 14f, 1f, (to - from) / 10f);
            zone.GetComponent<RapidsZone>().Configure(intensity);
            EditorUtility.SetDirty(zone.GetComponent<RapidsZone>());
        }

        // Lluvia de cocos (H30) entre dos distancias: los cocos caen delante de la balsa cada 'interval' s.
        private static void CoconutRain(GreyboxLevel level, float from, float to, float interval, string name, float phase = 0f)
        {
            float mid = (from + to) * 0.5f;
            var go = level.Place(HZ + "H30_Coconut.prefab", mid, 0f);
            go.name = name;
            var fall = go.GetComponent<CoconutFall>();
            fall.ConfigureRain(interval, go.transform.Find("Sombra"), 0.8f, phase);
            EditorUtility.SetDirty(fall);
        }

        // Luciérnaga móvil con luz y esfera emisiva; 'size' 1 = pequeña, 2.2 = gigante.
        private static GameObject Firefly(GreyboxLevel level, float d, float lateral, Color color, float size, float phase, float height = 2.4f)
        {
            var go = new GameObject(size > 1.5f ? "LuciernagaGigante" : "Luciernaga");
            go.transform.SetParent(level.GroupOf("Props"), false);
            go.transform.position = level.PointAt(d, lateral, height);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 8f + size * 2f;
            light.intensity = 2.2f + size * 0.4f;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>();
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Cuerpo";
            orb.transform.SetParent(go.transform, false);
            orb.transform.localScale = Vector3.one * 0.3f * size;
            Object.DestroyImmediate(orb.GetComponent<Collider>());
            orb.GetComponent<MeshRenderer>().sharedMaterial = FireflyMaterial(level, color);
            go.AddComponent<FireflyDrift>().Configure(new Vector3(size > 1.5f ? 2.2f : 2.6f, 0.5f, size > 1.5f ? 1.4f : 2.6f), size > 1.5f ? 0.35f : 0.45f, phase);
            return go;
        }

        // Un material emisivo por color y nivel (guardar otro con el mismo nombre rompería las luciérnagas ya hechas).
        private static readonly Dictionary<string, Material> FireflyMaterials = new Dictionary<string, Material>();

        private static Material FireflyMaterial(GreyboxLevel level, Color color)
        {
            string key = ColorUtility.ToHtmlStringRGB(color);
            if (!FireflyMaterials.TryGetValue(key, out var material) || material == null)
            {
                material = level.ToonMaterial("Luciernaga_" + key, color, color * 3f);
                FireflyMaterials[key] = material;
            }
            return material;
        }

        // Zonas oscuras (H18, visibilidad 'visibility' m) cada 40 m entre 'from' y 'to'.
        private static void DarkSpan(GreyboxLevel level, float from, float to, float visibility)
        {
            for (float d = from + 20f; d < to; d += 40f)
            {
                var zone = level.Place(HZ + "H18_Dark.prefab", d, 0f, 0f);
                zone.name = "ZonaOscura";
                zone.GetComponent<BoxCollider>().size = new Vector3(level.WidthAt(d) + 6f, 12f, 40f);
                zone.GetComponent<DarkZone>().Configure(visibility, new Light[0], 0f);
                EditorUtility.SetDirty(zone.GetComponent<DarkZone>());
            }
        }

        // Abismo (H22): trigger bajo el hueco con el puente de boyas de ayuda y el aviso de 30 m antes.
        private static void Abyss(GreyboxLevel level, float from, float to, float bridgeWidth)
        {
            float d = (from + to) * 0.5f;
            var go = level.Place(HZ + "H22_Abyss.prefab", d, 0f, 0f, -33f);
            go.name = "Abismo";
            go.GetComponent<BoxCollider>().size = new Vector3(level.WidthAt(d) + 40f, 60f, to - from + 20f);
            var abyss = go.GetComponent<GasSafari.Hazards.Abyss>();
            abyss.Configure(level.BuoyBridge(from, to, bridgeWidth));
            EditorUtility.SetDirty(abyss);
            AbyssSignage.Add(level, from, Mathf.RoundToInt(from).ToString());
        }

        // Nicho en la pared (mango escondido): fondo y lados sólidos, la pared tiene hueco (WallGap en el nivel).
        private static void Alcove(GreyboxLevel level, float d, float side)
        {
            float yaw = Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y;
            level.Block("Nicho_Fondo", level.PointAt(d, side * 10.6f), new Vector3(1f, 3f, 7f), CaveColor, yaw);
            level.Block("Nicho_Lado1", level.PointAt(d - 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), CaveColor, yaw);
            level.Block("Nicho_Lado2", level.PointAt(d + 3.5f, side * 8.4f), new Vector3(4.5f, 3f, 1f), CaveColor, yaw);
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
