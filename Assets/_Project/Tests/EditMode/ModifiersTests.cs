using System.IO;
using System.Linq;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // M7-03 Modificadores (Caos, docs/06 §1): los 8 con su efecto y sus estrellas, textos es/en y que no den estrellas.
    public class ModifiersTests
    {
        [Test]
        public void TheEightModifiers_FollowTheDocs()
        {
            var config = ModesConfig.Instance;
            var expected = new (string id, ModifierKind kind, float value, int stars)[]
            {
                ("soapy", ModifierKind.SoapyRaft, 0.5f, 30),
                ("infinite_gas", ModifierKind.InfiniteGas, 1f, 30),
                ("moon", ModifierKind.LunarGravity, 0.4f, 40),
                ("reverse", ModifierKind.ReversedFarts, -1f, 40),
                ("bigheads", ModifierKind.BigHeads, 2f, 50),
                ("turbo", ModifierKind.TurboCurrent, 1.5f, 50),
                ("dizzy", ModifierKind.DizzyCamera, 6f, 60),
                ("single", ModifierKind.SingleChamber, 1f, 70),
            };
            Assert.AreEqual(expected.Length, config.modifiers.Length);
            var ordered = config.modifiers.OrderBy(m => m.order).ToArray();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].id, ordered[i].id);
                Assert.AreEqual(expected[i].kind, ordered[i].kind, ordered[i].id);
                Assert.AreEqual(expected[i].value, ordered[i].value, 1e-5f, ordered[i].id);
                Assert.AreEqual(expected[i].stars, ordered[i].starsRequired, ordered[i].id);
                Assert.AreSame(ordered[i], config.FindModifier(expected[i].id));
            }
            Assert.IsTrue(GameCatalog.Instance.modes.First(m => m.id == ProgressionRules.MODE_MODIFIERS).implemented);
            Assert.AreEqual(ProgressionRules.MODE_MODIFIERS, ModeLauncher.RunModeFor(ProgressionRules.MODE_MODIFIERS));
        }

        [Test]
        public void Texts_ExistInBothLanguages()
        {
            var keys = ModesConfig.Instance.modifiers.SelectMany(m => new[] { m.NameKey, m.DescKey })
                .Concat(new[] { "mod.title", "mod.warning", "mod.choose_one", "ui.on", "ui.off" });
            foreach (var key in keys)
            {
                foreach (var language in new[] { "es", "en" })
                {
                    string text = LocalizedText.Table.Get(key, language);
                    Assert.IsFalse(string.IsNullOrEmpty(text) || text == key, key + " en " + language);
                }
            }
            Assert.AreEqual("Cámara única", LocalizedText.Table.Get("mod.single.name", "es"));
        }

        [Test]
        public void Beans_WithModifiers_AreOnlyFinishAndFullSyncs()
        {
            Assert.AreEqual(20, ResultsBuilder.BeansWithoutProgress(0), "sin primera vez ni estrellas nuevas, que no se guardan");
            Assert.AreEqual(27, ResultsBuilder.BeansWithoutProgress(7));
            Assert.AreEqual(30, ResultsBuilder.BeansWithoutProgress(25), "máximo 10 por grupos x4");
            Assert.AreEqual("mod.warning", ResultsScreen.ModeNote(new LevelResults { RunMode = ProgressionRules.MODE_MODIFIERS }).key);
        }

        [Test]
        public void Commit_WithModifiers_KeepsStarsAndTimes()
        {
            string folder = Path.Combine(Path.GetTempPath(), "gasSafariMods_" + System.Guid.NewGuid().ToString("N"));
            string previousGhosts = GhostStore.Folder;
            var go = new GameObject("Progression");
            try
            {
                var service = go.AddComponent<ProgressionService>();
                service.Initialize(new SaveSystem(Path.Combine(folder, "save.json")));
                var level = ScriptableObject.CreateInstance<LevelData>();
                level.id = "W1-1";
                service.Commit(level, new LevelResults { Time = 50f, Stars = 3, Beans = 20, MangoMask = 2 }, true);
                var record = service.Data.FindLevel("W1-1");
                Assert.AreEqual(0, record.stars, "docs/06: con modificadores no se guardan estrellas");
                Assert.AreEqual(0f, record.best, "ni el tiempo");
                Assert.AreEqual(2, record.mangos, "los mangos dorados sí (como en M3-10)");
                Assert.AreEqual(20, service.Data.frijolitos);
                Object.DestroyImmediate(level);
            }
            finally
            {
                Object.DestroyImmediate(go);
                GhostStore.Folder = previousGhosts;
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void DizzyCamera_SwaysWithinItsAmplitude()
        {
            Assert.AreEqual(0f, DizzyCamera.Sway(0f, 6f, 4f).x, 1e-4f);
            Assert.AreEqual(6f, DizzyCamera.Sway(1f, 6f, 4f).x, 1e-4f, "un cuarto de vuelta: giro máximo");
            for (float t = 0f; t < 8f; t += 0.13f)
            {
                var sway = DizzyCamera.Sway(t, 6f, 4f);
                Assert.LessOrEqual(Mathf.Abs(sway.x), 6f + 1e-4f);
                Assert.LessOrEqual(Mathf.Abs(sway.y), 6f * 0.35f + 1e-4f);
            }
        }
    }
}
