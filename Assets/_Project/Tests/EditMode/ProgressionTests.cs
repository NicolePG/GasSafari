using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // T-PROG-01 y T-PROG-02 (docs/11 §2) y las reglas de guardado de docs/06 §2.
    public class ProgressionTests
    {
        private const string WORLDS = "Assets/_Project/Data/Worlds/";

        private static SaveData WithStars(int stars)
        {
            var data = new SaveData();
            // Reparte las estrellas en niveles de 3.
            for (int i = 0; stars > 0; i++, stars -= 3)
                data.levels.Add(new SaveData.LevelRecord { id = "L" + i, stars = Mathf.Min(3, stars) });
            return data;
        }

        [Test]
        public void TProg01_Stars_OneTwoThree()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.goldTime = 100f;
            level.silverTime = 140f;
            Assert.AreEqual(1, ResultsBuilder.StarsFor(level, 200f, false));
            Assert.AreEqual(2, ResultsBuilder.StarsFor(level, 130f, false));
            Assert.AreEqual(3, ResultsBuilder.StarsFor(level, 95f, false));
            Assert.AreEqual(2, ResultsBuilder.StarsFor(level, 95f, true), "hundirse quita la tercera estrella");
            Object.DestroyImmediate(level);
        }

        [TestCase(2, false)]
        [TestCase(2, true)]
        public void TProg02_WorldsUnlockWithStars(int world, bool enough)
        {
            int[] needed = { 0, 8, 20, 32, 45 };
            var asset = AssetDatabase.LoadAssetAtPath<WorldData>(WORLDS + "W" + world + ".asset");
            int required = needed[world - 1];
            Assert.AreEqual(required, asset.unlockAmount);
            var data = WithStars(enough ? required : required - 1);
            Assert.AreEqual(enough, ProgressionRules.IsWorldUnlocked(asset, data));
        }

        [Test]
        public void TProg02_AllWorldThresholds()
        {
            int[] needed = { 0, 8, 20, 32, 45 };
            for (int w = 1; w <= 5; w++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<WorldData>(WORLDS + "W" + w + ".asset");
                Assert.IsTrue(ProgressionRules.IsWorldUnlocked(asset, WithStars(needed[w - 1])), "W" + w);
                if (needed[w - 1] > 0)
                    Assert.IsFalse(ProgressionRules.IsWorldUnlocked(asset, WithStars(needed[w - 1] - 1)), "W" + w);
            }
        }

        [Test]
        public void TProg02_WorldSixNeedsSixtyGoldenMangos()
        {
            var world = AssetDatabase.LoadAssetAtPath<WorldData>(WORLDS + "W6.asset");
            var data = new SaveData();
            for (int i = 0; i < 20; i++)
                data.levels.Add(new SaveData.LevelRecord { id = "L" + i, mangos = 7 });
            Assert.AreEqual(60, ProgressionRules.TotalMangos(data));
            Assert.IsTrue(ProgressionRules.IsWorldUnlocked(world, data));

            data.levels[0].mangos = 3;
            Assert.IsFalse(ProgressionRules.IsWorldUnlocked(world, data), "59 mangos no alcanzan");
        }

        [Test]
        public void PopCount_CountsBits()
        {
            Assert.AreEqual(0, ProgressionRules.PopCount(0));
            Assert.AreEqual(2, ProgressionRules.PopCount(5));
            Assert.AreEqual(3, ProgressionRules.PopCount(7));
        }

        [Test]
        public void Modes_UnlockAsInDocs()
        {
            var data = new SaveData();
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_STORY, data));
            Assert.IsFalse(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_TIME_TRIAL, data));
            data.GetOrAddLevel("T-0").stars = 1;
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_TIME_TRIAL, data));

            Assert.IsFalse(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_PARTY, data));
            data.GetOrAddLevel("W1-5").stars = 1;
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_PARTY, data));
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_TEAM_RACE, data));
            Assert.IsFalse(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_ENDLESS, data));
            data.GetOrAddLevel("W2-5").stars = 1;
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_DAILY, data));
            Assert.IsFalse(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MIRROR, data));
            data.GetOrAddLevel("W5-5").stars = 1;
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MIRROR, data));

            Assert.IsFalse(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MODIFIERS, data));
            Assert.IsTrue(ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MODIFIERS, WithStars(30)));
        }

        private ProgressionService CreateService(out GameObject go)
        {
            go = new GameObject("Progression");
            var service = go.AddComponent<ProgressionService>();
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariProg_" + System.Guid.NewGuid().ToString("N"), "save.json");
            service.Initialize(new SaveSystem(path));
            return service;
        }

        private static LevelResults Result(float time, int stars, int mangoMask, int beans)
        {
            return new LevelResults { Time = time, Stars = stars, MangoMask = mangoMask, Beans = beans };
        }

        [Test]
        public void Commit_KeepsBestStarsAndTime_AccumulatesMangosAndBeans()
        {
            var service = CreateService(out var go);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.id = "W1-1";

            service.Commit(level, Result(150f, 2, 0b001, 80), false);
            service.Commit(level, Result(170f, 1, 0b100, 20), false);
            var record = service.Data.FindLevel("W1-1");

            Assert.AreEqual(2, record.stars, "una partida peor no baja las estrellas");
            Assert.AreEqual(150f, record.best, 1e-4f);
            Assert.AreEqual(0b101, record.mangos);
            Assert.AreEqual(100, service.Beans);

            service.Commit(level, Result(120f, 3, 0, 40), false);
            Assert.AreEqual(3, record.stars);
            Assert.AreEqual(120f, record.best, 1e-4f);

            Object.DestroyImmediate(level);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Commit_WithModifiers_DoesNotSaveStarsOrTime()
        {
            var service = CreateService(out var go);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.id = "W1-2";

            service.Commit(level, Result(100f, 3, 0b010, 50), true);
            var record = service.Data.FindLevel("W1-2");

            Assert.AreEqual(0, record.stars);
            Assert.AreEqual(0f, record.best);
            Assert.AreEqual(0b010, record.mangos, "los mangos sí cuentan");
            Assert.AreEqual(-1, service.BestStars("W1-2"));

            Object.DestroyImmediate(level);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Beans_NeverGoNegative()
        {
            var service = CreateService(out var go);
            service.AddBeans(-50);
            Assert.AreEqual(0, service.Beans);
            service.AddBeans(30);
            Assert.IsFalse(service.TrySpendBeans(100));
            Assert.AreEqual(30, service.Beans);
            Assert.IsTrue(service.TrySpendBeans(30));
            Assert.AreEqual(0, service.Beans);
            Assert.IsFalse(service.TrySpendBeans(-5));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Commit_PersistsToDisk()
        {
            var service = CreateService(out var go);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.id = "W1-3";
            service.Commit(level, Result(200f, 2, 0b11, 70), false);

            var reloaded = new SaveSystem(service.GetComponent<ProgressionService>() != null ? SavePathOf(service) : null).Load();
            Assert.AreEqual(70, reloaded.frijolitos);
            Assert.AreEqual(2, reloaded.FindLevel("W1-3").stars);

            Object.DestroyImmediate(level);
            Object.DestroyImmediate(go);
        }

        private static string SavePathOf(ProgressionService service)
        {
            var field = typeof(ProgressionService).GetField("system", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return ((SaveSystem)field.GetValue(service)).FilePath;
        }
    }
}
