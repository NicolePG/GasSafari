using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Editor;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // M10-01 adelantado: la tabla de textos es/en sale de strings.csv y TODA la interfaz lee por clave.
    public class LocalizationTests
    {
        private const string CSV = "Tools/Export/Localization/strings.csv";
        private const string TABLE = "Assets/_Project/Resources/Strings.asset";

        private UIStrings table;

        [SetUp]
        public void SetUp()
        {
            table = AssetDatabase.LoadAssetAtPath<UIStrings>(TABLE);
            Localization.Set(Localization.SPANISH);
            Assert.IsNotNull(table, "falta Resources/Strings.asset (menú Gas Safari/Setup/Importar textos (CSV))");
        }

        [TearDown]
        public void TearDown()
        {
            Localization.Set(Localization.SPANISH);
        }

        [Test]
        public void Table_HasEveryCsvRow_InBothLanguages()
        {
            var rows = StringsImporter.Parse(File.ReadAllText(CSV));
            Assert.GreaterOrEqual(rows.Count, 660, "el CSV trae 660+ textos");
            var missing = new List<string>();
            foreach (var row in rows)
            {
                if (table.Get(row.key, Localization.SPANISH) != row.text || table.Get(row.key, Localization.ENGLISH) != row.en)
                    missing.Add(row.key);
                Assert.IsNotEmpty(row.text, row.key + " sin español");
                Assert.IsNotEmpty(row.en, row.key + " sin inglés");
            }
            Assert.IsEmpty(missing, "la tabla no está al día con strings.csv (volver a importar): " + string.Join(", ", missing));
        }

        [Test]
        public void Placeholders_MatchBetweenLanguages()
        {
            var number = new Regex(@"\{\d+\}");
            foreach (var row in StringsImporter.Parse(File.ReadAllText(CSV)))
            {
                var es = new HashSet<string>();
                var en = new HashSet<string>();
                foreach (Match match in number.Matches(row.text)) es.Add(match.Value);
                foreach (Match match in number.Matches(row.en)) en.Add(match.Value);
                Assert.IsTrue(es.SetEquals(en), row.key + ": los {n} del español y del inglés no coinciden");
            }
        }

        [Test]
        public void Get_FollowsTheActiveLanguage()
        {
            Assert.AreEqual("Jugar", table.Get("menu.play"));
            Localization.Set(Localization.ENGLISH);
            Assert.AreEqual("Play", table.Get("menu.play"));
            Assert.AreEqual("Trophies", table.Get("menu.achievements"));
            Localization.Set(Localization.SPANISH);
            Assert.AreEqual("Logros", table.Get("menu.achievements"));
        }

        [Test]
        public void Get_MissingEnglish_FallsBackToSpanish_AndUnknownKeyReturnsTheKey()
        {
            var custom = ScriptableObject.CreateInstance<UIStrings>();
            custom.entries = new[] { new UIStrings.Entry { key = "a.b", text = "hola", en = "" }, new UIStrings.Entry { key = "a.c", text = "uno", en = "one" } };
            Assert.AreEqual("hola", custom.Get("a.b", Localization.ENGLISH));
            Assert.AreEqual("one", custom.Get("a.c", Localization.ENGLISH));
            Assert.AreEqual("a.z", custom.Get("a.z"));
            Assert.AreEqual(string.Empty, custom.Get(null));
            Object.DestroyImmediate(custom);
        }

        [Test]
        public void Set_RaisesChangedOnlyWhenTheLanguageChanges()
        {
            int calls = 0;
            void Count() => calls++;
            Localization.Changed += Count;
            try
            {
                Localization.Set(Localization.SPANISH);
                Assert.AreEqual(0, calls);
                Localization.Set(Localization.ENGLISH);
                Assert.AreEqual(1, calls);
                Localization.Set("fr");
                Assert.AreEqual(Localization.SPANISH, Localization.Language, "un código desconocido cae en español");
                Assert.AreEqual(2, calls);
            }
            finally
            {
                Localization.Changed -= Count;
            }
        }

        [Test]
        public void LocalizedText_ReadsByKey_AndRefreshesOnLanguageChange()
        {
            var previous = LocalizedText.Table;
            LocalizedText.Table = table;
            var go = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI), typeof(LocalizedText));
            go.SetActive(false);
            go.SetActive(true);
            try
            {
                var label = go.GetComponent<LocalizedText>();
                label.Setup("menu.play", 40f);
                Assert.AreEqual("Jugar", label.Label.text);
                Localization.Set(Localization.ENGLISH);
                label.Refresh(); // en EditMode no corre OnEnable: el aviso por evento se prueba en PlayMode
                Assert.AreEqual("Play", label.Label.text);
                label.Setup("map.stars", 40f, 3, 93);
                Assert.AreEqual("Stars 3/93", label.Label.text);
                LocalizedText.SetTextScale(1.5f);
                label.Refresh();
                Assert.AreEqual(60f, label.Label.fontSizeMax, 0.01f);
                LocalizedText.SetTextScale(1f);
            }
            finally
            {
                LocalizedText.Table = previous;
                Object.DestroyImmediate(go);
                LocalizedText.SetTextScale(1f);
            }
        }

        // Toda clave escrita como literal en el código de la interfaz existe en la tabla.
        [Test]
        public void EveryKeyUsedInRuntimeCode_ExistsInTheTable()
        {
            var pattern = new Regex("\"((?:game|ui|lock|lang|menu|mode|tt|mirror|endless|daily|race|mod|party|select|stat|char|lobby|net|map|sheet|level|world|hud|synergy|ping|pause|results|award|assist|diff|loading|powerup|hazard|tutorial|ctrl|options|shop|reward|ach|stats|credits|story)\\.[a-z0-9_]+(?:\\.[a-z0-9_]+)*)\"(?!\\s*\\+)");
            var missing = new SortedSet<string>();
            foreach (string file in Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Editor/"))
                    continue;
                foreach (Match match in pattern.Matches(File.ReadAllText(file)))
                {
                    string key = match.Groups[1].Value;
                    if (!table.Has(key))
                        missing.Add(key + " (" + Path.GetFileName(file) + ")");
                }
            }
            Assert.IsEmpty(missing, "claves sin texto en la tabla: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryGameDataText_HasKeysInBothLanguages()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(MenuDataBuilder.CATALOG_PATH);
            Assert.IsNotNull(catalog, "falta Resources/GameCatalog.asset");
            var keys = new List<string>();

            foreach (var level in catalog.AllLevels())
            {
                keys.Add(level.NameKey);
                keys.Add(level.MechanicKey);
            }
            foreach (var world in catalog.worlds) keys.Add(world.NameKey);
            foreach (var difficulty in catalog.difficulties) { keys.Add(difficulty.NameKey); keys.Add(difficulty.DescKey); }
            foreach (var character in catalog.characters)
            {
                foreach (string suffix in new[] { "name", "species", "role", "passive_name", "passive_desc", "ability_name", "ability_desc" })
                    keys.Add(character.KeyBase + "." + suffix);
            }
            foreach (var item in catalog.cosmetics) keys.Add(item.nameKey);
            foreach (var achievement in catalog.achievements) { keys.Add(achievement.NameKey); keys.Add(achievement.DescKey); }
            foreach (var mode in catalog.modes) { keys.Add(mode.NameKey); keys.Add(mode.DescKey); }
            foreach (string guid in AssetDatabase.FindAssets("t:TutorialStep"))
            {
                var step = AssetDatabase.LoadAssetAtPath<TutorialStep>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsNotEmpty(step.textKey, step.name + " sin textKey");
                keys.Add(step.textKey + ".title");
                keys.Add(step.textKey + ".text");
            }

            Assert.GreaterOrEqual(keys.Count, 200);
            foreach (string key in keys)
            {
                Assert.IsTrue(table.Has(key), "falta la clave " + key);
                Assert.AreNotEqual(key, table.Get(key, Localization.SPANISH), key);
                Assert.IsNotEmpty(table.Get(key, Localization.ENGLISH), key + " sin inglés");
            }
        }

        [Test]
        public void StoryCaptions_ReadFromTheUnifiedTable()
        {
            int checkedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:StoryVignetteData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<StoryVignetteData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreSame(table, data.strings, data.name + " debe leer Resources/Strings.asset");
                checkedCount++;
            }
            Assert.AreEqual(7, checkedCount);
            Assert.IsFalse(File.Exists("Assets/_Project/Data/UI/Strings_es.asset"), "Strings_es.asset se reemplazó por la tabla");
        }

        [Test]
        public void TutorialCard_UsesTheTutorialKeys_NotTheBakedSpanish()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TutorialStep"))
            {
                var step = AssetDatabase.LoadAssetAtPath<TutorialStep>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreEqual(table.Get(step.textKey + ".title", Localization.SPANISH), step.title, step.name + ": el texto de respaldo debe coincidir con la tabla");
            }
        }
    }
}
