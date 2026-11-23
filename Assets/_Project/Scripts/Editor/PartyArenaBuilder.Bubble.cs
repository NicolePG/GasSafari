using GasSafari.Core;
using GasSafari.Modes;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Carrera de Burbujas (M7-07): pista ovalada (dos rectas de 20 m y curvas de 9 m de radio en la línea central, carril
    // de 7 m) con una isla en el medio, barandas altas (las mini balsas flotan a 1 m con la gaseosa), postes en las curvas
    // y una chicana en la recta de arriba. La línea de salida y meta está en la mitad de la recta de abajo.
    public static partial class PartyArenaBuilder
    {
        private const float RAIL_TOP = 2.4f;
        private const float RAIL_THICK = 0.6f;
        private const float OUTER_RING = 8f;
        private const int TURN_SEGMENTS = 20;

        // Línea de carrera de los bots (s, lat): pasa a 1.7 m o más de cada obstáculo.
        private static readonly Vector2[] BubbleLine =
        {
            new Vector2(0f, 0.2f), new Vector2(10f, 0f), new Vector2(20f, 0.4f), new Vector2(27f, 1.3f), new Vector2(34f, 1.6f),
            new Vector2(42f, -0.4f), new Vector2(48f, -1.4f), new Vector2(53f, -1.9f), new Vector2(57.5f, 0f), new Vector2(62f, 1.9f),
            new Vector2(66f, 1.2f), new Vector2(71f, 0.2f), new Vector2(76f, -1.4f), new Vector2(82f, 0f), new Vector2(88f, 1.4f),
            new Vector2(93f, 0.6f),
        };

        // Frijoles P01 (s, lat): tres filas por vuelta que cruzan el carril.
        private static readonly Vector2[] BubbleBeans =
        {
            new Vector2(18f, -2f), new Vector2(18f, 0.3f), new Vector2(18f, 2.4f),
            new Vector2(46f, -2.2f), new Vector2(46f, -0.6f), new Vector2(46f, 1.2f),
            new Vector2(70f, -2f), new Vector2(70f, 0.2f), new Vector2(70f, 2.2f),
        };

        // Postes: (s, lat, radio).
        private static readonly Vector3[] BubblePosts =
        {
            new Vector3(34f, -1f, 0.7f), new Vector3(42f, 2f, 0.7f), new Vector3(76f, 1.4f, 0.7f), new Vector3(88f, -1.4f, 0.7f),
        };

        private static void BuildBubble(PartyGameData data)
        {
            var scene = Begin();
            try
            {
                var track = new StadiumTrack(Vector3.zero, 10f, 9f, 3.5f);
                const float startS = 10f;
                float outerR = track.radius + track.halfWidth;
                float innerR = track.radius - track.halfWidth;
                float hx = track.halfStraight + outerR + 2f;
                float hz = outerR + 2f;
                var root = Common(data, scene, out var spawner, new Vector3(0f, 30f, -27f), new Vector3(0f, 0f, -2f));
                var game = root.AddComponent<BubbleRaceGame>();
                Water(root.transform, new Vector2(hx, hz));
                var deck = Mat("M_Party_Deck", new Color(0.95f, 0.86f, 0.66f));
                var grass = Mat("M_Party_Island", new Color(0.45f, 0.78f, 0.42f));
                var red = Mat("M_Party_RailRed", new Color(0.92f, 0.25f, 0.22f));
                var white = Mat("M_Party_Buoy", new Color(1f, 0.97f, 0.9f));
                var arena = new GameObject("Arena").transform;
                arena.SetParent(root.transform, false);

                // Borde exterior: rectas y anillos de las curvas, con su baranda alta.
                for (int side = -1; side <= 1; side += 2)
                {
                    Block(arena, "Outer_" + side, new Vector3(0f, 0f, side * (outerR + OUTER_RING * 0.5f)), new Vector3(track.Straight, 0f, OUTER_RING), deck);
                    Block(arena, "OuterRail_" + side, new Vector3(0f, 0f, side * (outerR + RAIL_THICK * 0.5f)), new Vector3(track.Straight, 0f, RAIL_THICK), side < 0 ? red : white, 0f, RAIL_TOP);
                    Block(arena, "Island_" + side, new Vector3(0f, 0f, side * innerR * 0.5f), new Vector3(track.Straight, 0f, innerR), grass);
                    Block(arena, "InnerRail_" + side, new Vector3(0f, 0f, side * (innerR - RAIL_THICK * 0.5f)), new Vector3(track.Straight, 0f, RAIL_THICK), side < 0 ? white : red, 0f, RAIL_TOP);
                    TurnRing(arena, "OuterTurn_" + side, new Vector3(side * track.halfStraight, 0f, 0f), side, outerR, OUTER_RING, deck, 0f);
                    TurnRing(arena, "OuterRailTurn_" + side, new Vector3(side * track.halfStraight, 0f, 0f), side, outerR, RAIL_THICK, red, RAIL_TOP, white);
                    TurnRing(arena, "IslandTurn_" + side, new Vector3(side * track.halfStraight, 0f, 0f), side, 0f, innerR, grass, 0f);
                    TurnRing(arena, "InnerRailTurn_" + side, new Vector3(side * track.halfStraight, 0f, 0f), side, innerR - RAIL_THICK, RAIL_THICK, white, RAIL_TOP, red);
                }
                Skirt(arena, hx, hz);

                foreach (var post in BubblePosts)
                    Post(arena, track.Point(post.x, post.y), post.z, red);
                // Chicana de la recta de arriba: una barrera de boyas desde afuera y otra desde la isla.
                ChicaneWall(arena, track, 53f, 0.3f, track.halfWidth, white);
                ChicaneWall(arena, track, 62f, -track.halfWidth, -0.3f, white);
                FinishLine(arena, track, startS, white);

                var spawns = new Transform[4];
                var models = new GameObject[4];
                var grid = new[] { new Vector2(startS - 3f, -1.5f), new Vector2(startS - 3f, 1.5f), new Vector2(startS - 6f, -1.5f), new Vector2(startS - 6f, 1.5f) };
                for (int i = 0; i < 4; i++)
                {
                    var position = track.Point(grid[i].x, grid[i].y) + Vector3.up * 0.3f;
                    spawns[i] = Marker(root.transform, "Spawn_J" + (i + 1), position, Quaternion.LookRotation(track.Direction(grid[i].x)));
                    models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(MINI_PREFABS + (i + 1) + ".prefab");
                }
                game.Configure(data, spawner, Balance);
                game.ConfigureArena(track, startS, spawns, models, BubbleLine,
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PowerUps/P01_Beans.prefab"), BubbleBeans);
                Save(scene, data.sceneName);
            }
            finally
            {
                End(scene);
            }
        }

        // Media corona de bloques alrededor del centro de una curva (side +1 = curva de la derecha), de 'inner' a
        // inner + depth. Con 'alternate', las piezas alternan dos colores (baranda roja y blanca).
        private static void TurnRing(Transform parent, string name, Vector3 center, int side, float inner, float depth, Material material, float top, Material alternate = null)
        {
            float mid = inner + depth * 0.5f;
            float outer = inner + depth;
            float width = Mathf.PI * outer / TURN_SEGMENTS * 1.08f;
            for (int i = 0; i < TURN_SEGMENTS; i++)
            {
                float angle = (side > 0 ? 0f : 180f) + (i + 0.5f) * 180f / TURN_SEGMENTS;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var mat = alternate != null && i % 2 == 1 ? alternate : material;
                Block(parent, name + "_" + i, center + direction * mid, new Vector3(width, 0f, depth), mat, angle, top > 0f ? top : DECK_TOP);
            }
        }

        private static void Post(Transform parent, Vector3 position, float radius, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Post";
            go.layer = GameLayers.Environment;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, (RAIL_TOP + DECK_BOTTOM) * 0.5f, position.z);
            go.transform.localScale = new Vector3(2f * radius, (RAIL_TOP - DECK_BOTTOM) * 0.5f, 2f * radius);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // Barrera atravesada en 's' que cubre de lat0 a lat1 (0.8 m de grueso), con boyas encima.
        private static void ChicaneWall(Transform parent, StadiumTrack track, float s, float lat0, float lat1, Material material)
        {
            var center = track.Point(s, (lat0 + lat1) * 0.5f);
            float yaw = Quaternion.LookRotation(track.Direction(s)).eulerAngles.y;
            Block(parent, "Chicane_" + s, center, new Vector3(Mathf.Abs(lat1 - lat0), 0f, 0.8f), material, yaw, RAIL_TOP);
            var buoy = Mat("M_Party_RailRed", new Color(0.92f, 0.25f, 0.22f));
            for (float lat = Mathf.Min(lat0, lat1) + 0.4f; lat < Mathf.Max(lat0, lat1); lat += 0.9f)
                Visual(parent, PrimitiveType.Sphere, "ChicaneBuoy", track.Point(s, lat) + Vector3.up * RAIL_TOP, new Vector3(0.7f, 0.5f, 0.7f), buoy);
        }

        // Franja a cuadros sobre el agua y un arco con dos postes en las orillas.
        private static void FinishLine(Transform parent, StadiumTrack track, float s, Material white)
        {
            var black = Mat("M_Party_Black", new Color(0.12f, 0.12f, 0.15f));
            float yaw = Quaternion.LookRotation(track.Direction(s)).eulerAngles.y;
            const int squares = 10;
            float size = 2f * track.halfWidth / squares;
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < squares; i++)
                {
                    float lat = -track.halfWidth + (i + 0.5f) * size;
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Finish_" + row + "_" + i;
                    Object.DestroyImmediate(go.GetComponent<Collider>());
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = track.Point(s + (row - 0.5f) * size, lat) + Vector3.up * 0.03f;
                    go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    go.transform.localScale = new Vector3(size, 0.02f, size);
                    go.GetComponent<MeshRenderer>().sharedMaterial = (i + row) % 2 == 0 ? white : black;
                }
            }
            for (int side = -1; side <= 1; side += 2)
                Visual(parent, PrimitiveType.Cylinder, "ArchPost_" + side, track.Point(s, side * (track.halfWidth + 0.9f)) + Vector3.up * 2f, new Vector3(0.35f, 2f, 0.35f), white);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "ArchBar";
            Object.DestroyImmediate(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = track.Point(s, 0f) + Vector3.up * 4f;
            bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            bar.transform.localScale = new Vector3(2f * track.halfWidth + 2.2f, 0.5f, 0.3f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = black;
        }
    }
}
