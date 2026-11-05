using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Construye el HUD del nivel (docs/07 §3) con arte provisional: sprites de UGUI y un anillo generado.
    // LevelTemplateBuilder lo coloca dentro del canvas HUD; los prefabs de panel y anillo viven en Prefabs/UI.
    public static class HudBuilder
    {
        public const string STRINGS_PATH = "Assets/_Project/Data/UI/Strings_es.asset";
        private const string PANEL_PREFAB = "Assets/_Project/Prefabs/UI/HudPlayerPanel.prefab";
        private const string OVERLAY_PREFAB = "Assets/_Project/Prefabs/UI/AvatarOverlay.prefab";
        private const string RING_PATH = "Assets/_Project/Art/UI/ring.png";
        private const string SPRITE_BOX = "UI/Skin/UISprite.psd";
        private const string SPRITE_CIRCLE = "UI/Skin/Knob.psd";
        private const string SPRITE_ARROW = "UI/Skin/DropdownArrow.psd";
        private static readonly Color Panel = new Color(0.05f, 0.08f, 0.15f, 0.75f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.1f);

        [MenuItem("Gas Safari/Setup/Crear textos y prefabs del HUD")]
        public static void BuildAssets()
        {
            BuildStrings();
            BuildPlayerPanel();
            BuildOverlay();
            AssetDatabase.SaveAssets();
        }

        // Llena el canvas HUD de la plantilla de nivel.
        public static void Populate(GameObject hud)
        {
            BuildAssets();
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(STRINGS_PATH);
            var root = hud.AddComponent<LevelHud>();
            var timer = BuildTimer(hud.transform);
            var chambers = BuildChambers(hud.transform);
            var progress = BuildProgress(hud.transform);
            var eventText = BuildEventText(hud.transform);
            var players = Rect("Players", hud.transform);
            Stretch(players);
            BuildArrows(hud.transform);

            SetRef(root, "strings", strings);
            SetRef(root, "timer", timer);
            SetRef(root, "chambers", chambers);
            SetRef(root, "progress", progress);
            SetRef(root, "eventText", eventText);
            SetRef(root, "playersRoot", players);
            SetRef(root, "panelPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PANEL_PREFAB).GetComponent<HudPlayerPanel>());
            SetRef(root, "overlayPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(OVERLAY_PREFAB).GetComponent<AvatarOverlay>());
        }

        private static void BuildStrings()
        {
            var strings = LoadOrCreate<UIStrings>(STRINGS_PATH);
            strings.entries = new[]
            {
                E("ui.press_any", "Pulsa cualquier botón"), E("ui.join", "Pulsa A para unirte"), E("ui.ready", "¡Listo!"),
                E("hud.sync2", "¡SYNC x2!"), E("hud.sync3", "¡SYNC x3!"), E("hud.sync4", "¡SYNC x4!"),
                E("hud.superfart_ready", "¡SÚPER PEDO LISTO!"), E("hud.go", "¡YA!"), E("hud.checkpoint", "CHECKPOINT"),
                E("hud.no_gas", "Sin gas"), E("hud.mangos", "Mangos {0}/{1}"), E("hud.checkpoint_progress", "Checkpoint {0}/{1}"),
                E("ping.now", "¡Ahora!"), E("ping.wait", "Espera"), E("ping.left", "Gira a la izquierda"), E("ping.right", "Gira a la derecha"),
                E("pause.title", "Pausa"), E("pause.resume", "Continuar"), E("pause.restart_checkpoint", "Reiniciar desde checkpoint"),
                E("pause.restart_level", "Reiniciar nivel"), E("pause.options", "Opciones"), E("pause.controls", "Controles"),
                E("pause.quit", "Salir al mapa"), E("pause.yes", "Sí"), E("pause.no", "No"),
                E("pause.vote", "¿Reiniciar desde el checkpoint? Sí {0} · No {1} (hacen falta {2})"),
                E("results.who", "¿Quién fue?"), E("results.beans", "Frijolitos +{0}"), E("results.next", "Siguiente"),
                E("results.retry", "Repetir"), E("results.map", "Mapa"),
                // Premios de docs/06 §4.
                E("award.sync_master", "Maestro de la Sincronía"), E("award.lifeguard", "Salvavidas"), E("award.show_king", "Rey del Show"),
                E("award.glutton", "Tragón"), E("award.untimely_fart", "Pedo más inoportuno"), E("award.captain_titanic", "Capitán Titanic"),
                E("award.wasted_gas", "Gas Desperdiciado"), E("award.sleeping_beauty", "Bello Durmiente"), E("assist.offer", "¿Necesitan una Ayuda Real?"),
                // Nombres de sinergias (docs/03).
                E("synergy.honda_selva", "Honda de la Selva"), E("synergy.terremoto", "Terremoto"), E("synergy.fiesta_total", "Fiesta Total"),
                E("synergy.duo_pesado", "Dúo Pesado"), E("synergy.rescate_real", "Rescate Real")
            };
            EditorUtility.SetDirty(strings);
        }

        private static UIStrings.Entry E(string key, string text) => new UIStrings.Entry { key = key, text = text };

        private static HudTimer BuildTimer(Transform parent)
        {
            var group = Rect("Timer", parent);
            Anchor(group, new Vector2(0.5f, 1f), new Vector2(520f, 150f), new Vector2(0f, -16f));
            var time = Text("Time", group, 72, TextAlignmentOptions.Center);
            Place(time.rectTransform, new Vector2(0.5f, 1f), new Vector2(520f, 90f), Vector2.zero);

            var glow = Img("MeterGlow", group, SPRITE_BOX, Gold);
            Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(432f, 36f), new Vector2(0f, -102f));
            var back = Img("MeterBack", group, SPRITE_BOX, Panel);
            Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(420f, 24f), new Vector2(0f, -108f));
            var fill = Img("MeterFill", back.transform, SPRITE_BOX, new Color(0.3f, 0.85f, 0.95f));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            var ready = Text("SuperReady", group, 40, TextAlignmentOptions.Center);
            ready.color = Gold;
            Place(ready.rectTransform, new Vector2(0.5f, 1f), new Vector2(520f, 50f), new Vector2(0f, -150f));
            glow.gameObject.SetActive(false);
            ready.gameObject.SetActive(false);

            var timer = group.gameObject.AddComponent<HudTimer>();
            SetRef(timer, "timeText", time);
            SetRef(timer, "meterFill", fill);
            SetRef(timer, "meterGlow", glow);
            SetRef(timer, "superReadyText", ready);
            return timer;
        }

        private static HudChambers BuildChambers(Transform parent)
        {
            var group = Rect("Chambers", parent);
            Anchor(group, new Vector2(0f, 1f), new Vector2(150f, 150f), new Vector2(24f, -24f));
            var shield = Img("Shield", group, SPRITE_BOX, new Color(0.3f, 0.55f, 1f));
            Stretch(shield.rectTransform);
            shield.gameObject.SetActive(false);
            var back = Img("Back", group, SPRITE_BOX, Panel);
            Stretch(back.rectTransform, 6f);

            // Índices de la balsa: 0 NE, 1 NO, 2 SE, 3 SO.
            var offsets = new[] { new Vector2(34f, 34f), new Vector2(-34f, 34f), new Vector2(34f, -34f), new Vector2(-34f, -34f) };
            var icons = new Image[offsets.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = Img("Chamber" + i, group, SPRITE_CIRCLE, Color.green);
                Place(icons[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(56f, 56f), offsets[i]);
            }

            var chambers = group.gameObject.AddComponent<HudChambers>();
            var so = new SerializedObject(chambers);
            var array = so.FindProperty("icons");
            array.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            so.FindProperty("shieldFrame").objectReferenceValue = shield;
            so.ApplyModifiedPropertiesWithoutUndo();
            return chambers;
        }

        private static HudProgress BuildProgress(Transform parent)
        {
            var group = Rect("Progress", parent);
            Anchor(group, new Vector2(1f, 1f), new Vector2(380f, 110f), new Vector2(-24f, -24f));
            var mango = Text("Mangos", group, 40, TextAlignmentOptions.Right);
            mango.color = Gold;
            Place(mango.rectTransform, new Vector2(1f, 1f), new Vector2(380f, 50f), Vector2.zero);
            var checkpoint = Text("Checkpoint", group, 34, TextAlignmentOptions.Right);
            Place(checkpoint.rectTransform, new Vector2(1f, 1f), new Vector2(380f, 50f), new Vector2(0f, -56f));

            var progress = group.gameObject.AddComponent<HudProgress>();
            SetRef(progress, "mangoText", mango);
            SetRef(progress, "checkpointText", checkpoint);
            return progress;
        }

        private static HudEventText BuildEventText(Transform parent)
        {
            var group = Rect("EventText", parent);
            Anchor(group, new Vector2(0.5f, 0.5f), new Vector2(1200f, 220f), new Vector2(0f, 120f));
            var label = Text("Label", group, 130, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform);

            var eventText = group.gameObject.AddComponent<HudEventText>();
            SetRef(eventText, "label", label);
            return eventText;
        }

        private static void BuildArrows(Transform parent)
        {
            var group = Rect("HazardArrows", parent);
            Stretch(group);
            var arrows = new RectTransform[4];
            for (int i = 0; i < arrows.Length; i++)
            {
                arrows[i] = Rect("Arrow" + i, group);
                arrows[i].sizeDelta = new Vector2(70f, 70f);
                var image = Img("Sprite", arrows[i], SPRITE_ARROW, new Color(1f, 0.25f, 0.2f));
                Stretch(image.rectTransform);
                // El sprite apunta hacia abajo; la flecha se orienta con "arriba" como cero.
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }

            var component = group.gameObject.AddComponent<HudHazardArrows>();
            var so = new SerializedObject(component);
            var array = so.FindProperty("arrows");
            array.arraySize = arrows.Length;
            for (int i = 0; i < arrows.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = arrows[i];
            so.FindProperty("area").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPlayerPanel()
        {
            var root = Rect("HudPlayerPanel", null);
            root.sizeDelta = new Vector2(340f, 150f);
            var frame = Img("Frame", root, SPRITE_BOX, Color.white);
            Stretch(frame.rectTransform);
            var back = Img("Back", root, SPRITE_BOX, Panel);
            Stretch(back.rectTransform, 6f);

            var portrait = Img("Portrait", root, SPRITE_BOX, Color.gray);
            Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 110f), new Vector2(20f, 0f));
            var number = Text("Number", portrait.transform, 30, TextAlignmentOptions.TopLeft);
            Stretch(number.rectTransform, 4f);

            var gasBack = Img("GasBack", root, SPRITE_BOX, new Color(0f, 0f, 0f, 0.6f));
            Place(gasBack.rectTransform, new Vector2(1f, 1f), new Vector2(190f, 26f), new Vector2(-20f, -26f));
            var gas = Img("GasFill", gasBack.transform, SPRITE_BOX, new Color(0.6f, 0.9f, 0.3f));
            Stretch(gas.rectTransform);
            gas.type = Image.Type.Filled;
            gas.fillMethod = Image.FillMethod.Horizontal;

            var ability = Img("Ability", root, SPRITE_CIRCLE, new Color(0.9f, 0.9f, 0.9f));
            Place(ability.rectTransform, new Vector2(1f, 0f), new Vector2(76f, 76f), new Vector2(-132f, 22f));
            var cooldown = Img("Cooldown", ability.transform, SPRITE_CIRCLE, new Color(0f, 0f, 0f, 0.7f));
            Stretch(cooldown.rectTransform);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;

            var medkit = Img("Medkit", root, SPRITE_BOX, new Color(0.9f, 0.2f, 0.25f));
            Place(medkit.rectTransform, new Vector2(1f, 0f), new Vector2(48f, 48f), new Vector2(-40f, 36f));
            var plus = Text("Plus", medkit.transform, 36, TextAlignmentOptions.Center);
            plus.SetText("+");
            Stretch(plus.rectTransform);

            var panel = root.gameObject.AddComponent<HudPlayerPanel>();
            SetRef(panel, "frame", frame);
            SetRef(panel, "portrait", portrait);
            SetRef(panel, "gasFill", gas);
            SetRef(panel, "abilityIcon", ability);
            SetRef(panel, "abilityCooldown", cooldown);
            SetRef(panel, "medkitIcon", medkit.gameObject);
            SetRef(panel, "numberText", number);
            EnsureFolder("Assets/_Project/Prefabs", "UI");
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PANEL_PREFAB);
            Object.DestroyImmediate(root.gameObject);
        }

        private static void BuildOverlay()
        {
            var ringSprite = EnsureRingSprite();
            var go = new GameObject("AvatarOverlay", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.localScale = Vector3.one * 0.012f;
            rect.localPosition = new Vector3(0f, 1.6f, 0f);

            var ring = new GameObject("Ring", typeof(RectTransform)).AddComponent<Image>();
            ring.transform.SetParent(go.transform, false);
            ring.sprite = ringSprite;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            Stretch(ring.rectTransform);

            var ping = Text("Ping", go.transform, 44, TextAlignmentOptions.Center);
            Place(ping.rectTransform, new Vector2(0.5f, 1f), new Vector2(420f, 60f), new Vector2(0f, 50f));
            ping.fontStyle = FontStyles.Bold;

            var overlay = go.AddComponent<AvatarOverlay>();
            SetRef(overlay, "ring", ring);
            SetRef(overlay, "pingText", ping);
            PrefabUtility.SaveAsPrefabAsset(go, OVERLAY_PREFAB);
            Object.DestroyImmediate(go);
        }

        // Anillo hueco de 128 px; el proyecto no tiene sprites de UI todavía.
        private static Sprite EnsureRingSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(RING_PATH);
            if (existing != null)
                return existing;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    float alpha = Mathf.Clamp01((d - 0.72f) * 40f) * Mathf.Clamp01((1f - d) * 40f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            EnsureFolder("Assets/_Project/Art", "UI");
            System.IO.File.WriteAllBytes(RING_PATH, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RING_PATH);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RING_PATH);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RING_PATH);
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static Image Img(string name, Transform parent, string builtinSprite, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
            image.color = color;
            image.raycastTarget = false;
            // El sprite de caja es de 9 trozos: así los bordes no se estiran.
            if (builtinSprite == SPRITE_BOX)
                image.type = Image.Type.Sliced;
            return image;
        }

        internal static TextMeshProUGUI Text(string name, Transform parent, float size, TextAlignmentOptions alignment)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        internal static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            Place(rect, anchor, size, position);
        }

        internal static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        internal static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
