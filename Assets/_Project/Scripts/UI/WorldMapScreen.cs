using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Mapa de mundos (docs/07 §2): el mapa ilustrado con las 6 islas, los niveles como puntos en el río, estrellas y mangos por
    // nivel y candados con su requisito. Las coordenadas de las islas son las del README de Tools/Export/UI (px, origen arriba a la izquierda).
    public class WorldMapScreen : MenuScreen
    {
        private static readonly Vector2[] IslandCenters =
        {
            new Vector2(200f, 720f), new Vector2(580f, 400f), new Vector2(890f, 640f),
            new Vector2(1180f, 330f), new Vector2(1510f, 630f), new Vector2(1670f, 310f)
        };

        // Dónde queda el tutorial T-0: en el río de la salida, junto a la balsa.
        private static readonly Vector2 TutorialPosition = new Vector2(420f, 505f);

        // Cinco puntos en arco alrededor de cada isla.
        private static readonly Vector2[] NodeOffsets =
        {
            new Vector2(-138f, 28f), new Vector2(-69f, -4f), new Vector2(0f, -20f), new Vector2(69f, -4f), new Vector2(138f, 28f)
        };

        private static readonly Vector2 NodeSize = new Vector2(92f, 70f);

        public class Node
        {
            public LevelData Level;
            public WorldData World;
            public Button Button;
            public Image Face;
            public Image[] Stars = new Image[3];
            public Image Lock;
            public LocalizedText Number;
        }

        private readonly List<Node> nodes = new List<Node>();
        private readonly List<LocalizedText> worldTitles = new List<LocalizedText>();
        private readonly List<LocalizedText> worldStars = new List<LocalizedText>();
        private LocalizedText totalStars;
        private LocalizedText totalMangos;
        private LocalizedText beans;
        private Button back;

        public override bool DimBackground => false;
        public IReadOnlyList<Node> Nodes => nodes;
        public Button BackButton => back;

        public override Selectable FirstSelected
        {
            get
            {
                var focus = MenuRequest.FocusLevel;
                if (focus != null)
                {
                    foreach (var node in nodes)
                    {
                        if (node.Level == focus)
                            return node.Button;
                    }
                }
                return NextPending() ?? (nodes.Count > 0 ? nodes[0].Button : back);
            }
        }

        // El primer nivel de un mundo desbloqueado que todavía no se termina.
        private Button NextPending()
        {
            var data = Data;
            foreach (var node in nodes)
            {
                if (node.World != null && ProgressionRules.IsWorldUnlocked(node.World, data) && !ProgressionRules.HasFinished(data, node.Level.id))
                    return node.Button;
            }
            return null;
        }

        private static SaveData Data => Services.IsReady && Services.Progression != null ? Services.Progression.Data : new SaveData();

        protected override void Build()
        {
            var catalog = GameCatalog.Instance;
            var map = UiFactory.Picture(Root, "Map", "WorldMap", Color.white, false);
            UiFactory.Stretch(map.rectTransform);
            map.raycastTarget = false;

            BuildTopBar();

            if (catalog != null)
            {
                for (int w = 0; w < catalog.worlds.Length && w < IslandCenters.Length; w++)
                    BuildWorld(catalog.worlds[w], w);
                if (catalog.tutorial != null)
                    nodes.Add(BuildNode(catalog.tutorial, null, TutorialPosition, "T", ButtonStyle.Blue, false));
            }

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(300f, 92f), () => App.Back(), 40f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(40f, 30f));
        }

        private void BuildTopBar()
        {
            var bar = UiFactory.Panel(Root, "TopBar", "Panel_Dark");
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(1840f, 96f), new Vector2(0f, -16f));
            bar.raycastTarget = false;
            var title = UiFactory.Label(bar.transform, "Title", "map.title", 40f, TextAlignmentOptions.MidlineLeft, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Region(title.Label.rectTransform, new Vector2(0f, 0f), new Vector2(0.24f, 1f), new Vector4(40f, 4f, 10f, 10f));
            var star = UiFactory.Picture(bar.transform, "StarIcon", "Icon_Star_Full", Color.white);
            UiFactory.Place(star.rectTransform, new Vector2(0.25f, 0.5f), new Vector2(54f, 54f), Vector2.zero);
            totalStars = UiFactory.Label(bar.transform, "Stars", "map.stars", 34f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold, 0, 93);
            UiFactory.Region(totalStars.Label.rectTransform, new Vector2(0.27f, 0f), new Vector2(0.5f, 1f), new Vector4(8f, 4f, 8f, 10f));
            var mango = UiFactory.Picture(bar.transform, "MangoIcon", "Icon_GoldenMango_Full", Color.white);
            UiFactory.Place(mango.rectTransform, new Vector2(0.51f, 0.5f), new Vector2(54f, 54f), Vector2.zero);
            totalMangos = UiFactory.Label(bar.transform, "Mangos", "map.golden_mangos", 34f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold, 0, 93);
            UiFactory.Region(totalMangos.Label.rectTransform, new Vector2(0.53f, 0f), new Vector2(0.82f, 1f), new Vector4(8f, 4f, 8f, 10f));
            var bean = UiFactory.Picture(bar.transform, "BeanIcon", "Icon_Bean", Color.white);
            UiFactory.Place(bean.rectTransform, new Vector2(0.83f, 0.5f), new Vector2(54f, 54f), Vector2.zero);
            beans = UiFactory.Literal(bar.transform, "Beans", "0", 38f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
            UiFactory.Region(beans.Label.rectTransform, new Vector2(0.85f, 0f), new Vector2(1f, 1f), new Vector4(8f, 4f, 20f, 10f));
        }

        private static Vector2 MapPoint(Vector2 pixels) => new Vector2(pixels.x, -pixels.y);

        private void BuildWorld(WorldData world, int index)
        {
            var center = IslandCenters[index];
            // Nombre del mundo y estrellas conseguidas, sobre las líneas de niveles.
            var panel = UiFactory.Panel(Root, "World" + (index + 1), "Panel_Dark", new Color(1f, 1f, 1f, 0.9f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400f, 104f);
            panelRect.anchoredPosition = MapPoint(center + new Vector2(0f, -110f));
            panel.raycastTarget = false;
            var title = UiFactory.Label(panel.transform, "Name", world.NameKey, 32f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(title.Label.rectTransform, new Vector2(0f, 0.46f), Vector2.one, new Vector4(18f, 0f, 18f, 10f));
            var stars = UiFactory.Label(panel.transform, "Stars", "map.stars", 26f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold, 0, world.levels.Count * 3);
            UiFactory.Region(stars.Label.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector4(18f, 6f, 18f, 0f));
            worldTitles.Add(title);
            worldStars.Add(stars);

            for (int i = 0; i < world.levels.Count && i < NodeOffsets.Length; i++)
            {
                bool boss = world.levels[i].isBoss;
                nodes.Add(BuildNode(world.levels[i], world, center + new Vector2(NodeOffsets[i].x, NodeOffsets[i].y * -1f), (i + 1).ToString(),
                    boss ? ButtonStyle.Red : ButtonStyle.Blue, boss));
            }
        }

        private Node BuildNode(LevelData level, WorldData world, Vector2 mapPixels, string number, ButtonStyle style, bool boss)
        {
            var size = boss ? NodeSize * 1.15f : NodeSize;
            var button = UiFactory.MakeButton(Root, "Level_" + level.id, null, style, size, null, 40f);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = MapPoint(mapPixels);

            var node = new Node { Level = level, World = world, Button = button, Face = button.GetComponent<Image>() };
            node.Number = UiFactory.Literal(button.transform, "Number", number, 44f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(node.Number.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(6f, 16f, 6f, 10f));
            for (int s = 0; s < 3; s++)
            {
                node.Stars[s] = UiFactory.Picture(button.transform, "Star" + s, "Icon_Star_Empty", Color.white);
                UiFactory.Place(node.Stars[s].rectTransform, new Vector2(0.5f, 0f), new Vector2(34f, 34f), new Vector2((s - 1) * 30f, -22f));
            }
            node.Lock = UiFactory.Picture(button.transform, "Lock", "Icon_Lock", Color.white);
            UiFactory.Place(node.Lock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(46f, 46f), Vector2.zero);
            node.Lock.gameObject.SetActive(false);

            var captured = node;
            button.onClick.AddListener(() => Choose(captured));
            return node;
        }

        public override void OnShow()
        {
            var data = Data;
            var catalog = GameCatalog.Instance;
            if (catalog == null)
                return;

            totalStars.SetKey("map.stars", ProgressionRules.TotalStars(data), 93);
            totalMangos.SetKey("map.golden_mangos", ProgressionRules.TotalMangos(data), 93);
            beans.SetLiteral(data.frijolitos.ToString());

            for (int w = 0; w < worldStars.Count; w++)
            {
                var world = catalog.worlds[w];
                int stars = 0;
                foreach (var level in world.levels)
                {
                    var record = data.FindLevel(level.id);
                    stars += record != null ? record.stars : 0;
                }
                worldStars[w].SetKey("map.stars", stars, world.levels.Count * 3);
            }

            foreach (var node in nodes)
                RefreshNode(node, data);
        }

        private void RefreshNode(Node node, SaveData data)
        {
            bool locked = node.World != null && !ProgressionRules.IsWorldUnlocked(node.World, data);
            var record = data.FindLevel(node.Level.id);
            int stars = record != null ? record.stars : 0;
            for (int s = 0; s < 3; s++)
                node.Stars[s].sprite = UiFactory.Sprite(s < stars ? "Icon_Star_Full" : "Icon_Star_Empty");
            node.Lock.gameObject.SetActive(locked);
            node.Number.gameObject.SetActive(!locked);
            foreach (var star in node.Stars)
                star.gameObject.SetActive(!locked);

            // Terminado = verde; el resto conserva su color (azul, o rojo en los jefes). Bloqueado = gris.
            string style = node.Level.isBoss ? "Red" : stars > 0 ? "Green" : "Blue";
            if (locked)
                style = node.Level.isBoss ? "Red" : "Blue";
            var normal = UiFactory.Sprite("Button_" + style + (locked ? "_Disabled" : "_Normal"));
            var highlighted = UiFactory.Sprite("Button_" + style + (locked ? "_Disabled" : "_Highlighted"));
            var pressed = UiFactory.Sprite("Button_" + style + (locked ? "_Disabled" : "_Pressed"));
            node.Face.sprite = normal;
            node.Button.spriteState = new SpriteState { highlightedSprite = highlighted, selectedSprite = highlighted, pressedSprite = pressed, disabledSprite = normal };
        }

        private void Choose(Node node)
        {
            var data = Data;
            if (node.World != null && !ProgressionRules.IsWorldUnlocked(node.World, data))
            {
                if (node.World.unlockKind == WorldUnlockKind.GoldenMangoes)
                    App.Toast("map.locked_mangos", node.World.unlockAmount);
                else
                    App.Toast("map.locked_stars", node.World.unlockAmount);
                return;
            }
            var sheet = App.Get<LevelSheetScreen>();
            sheet.Level = node.Level;
            App.Push<LevelSheetScreen>();
        }
    }
}
