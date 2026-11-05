using System.IO;
using GasSafari.Save;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // T-SAVE-01 y T-SAVE-02 (docs/11 §2).
    public class SaveSystemTests
    {
        private string folder;
        private SaveSystem system;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "gasSafariSaveTests_" + System.Guid.NewGuid().ToString("N"));
            system = new SaveSystem(Path.Combine(folder, "save.json"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private static SaveData Sample(int beans)
        {
            var data = new SaveData { frijolitos = beans };
            data.levels.Add(new SaveData.LevelRecord { id = "W1-1", best = 172.4f, stars = 3, mangos = 7 });
            data.levels.Add(new SaveData.LevelRecord { id = "W1-2", best = 201.5f, stars = 1, mangos = 2 });
            data.cosmetics.owned.Add("hat_pirate");
            data.cosmetics.equipped.Add(new SaveData.EquippedRecord { player = "p1", hat = "hat_pirate", gas = "gas_rainbow" });
            data.cosmetics.raft = "raft_lily";
            data.achievements.Add("first_fart");
            data.stats.farts = 1520;
            data.stats.sync4 = 9;
            data.stats.playSeconds = 9021;
            data.daily.lastDate = "20261105";
            data.daily.streak = 3;
            data.daily.best = 412.8f;
            data.settings.volMaster = 0.5f;
            data.settings.colorblind = true;
            data.settings.difficulty = "Familiar";
            return data;
        }

        [Test]
        public void TSave01_SaveAndLoad_KeepEveryField()
        {
            var original = Sample(340);
            system.Save(original);
            var loaded = system.Load();

            Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(loaded));
            Assert.AreEqual(340, loaded.frijolitos);
            Assert.AreEqual(172.4f, loaded.FindLevel("W1-1").best, 1e-4f);
            Assert.AreEqual(7, loaded.FindLevel("W1-1").mangos);
            Assert.AreEqual("gas_rainbow", loaded.cosmetics.equipped[0].gas);
            Assert.AreEqual("Familiar", loaded.settings.difficulty);
            Assert.IsFalse(File.Exists(Path.Combine(folder, "save.tmp")), "el temporal no queda");
        }

        [Test]
        public void TSave02_CorruptJson_LoadsBackup()
        {
            system.Save(Sample(100));
            system.Save(Sample(250));
            Assert.IsTrue(File.Exists(system.BackupPath), "el segundo guardado deja copia del primero");

            File.WriteAllText(system.FilePath, "{ esto no es json");
            var loaded = system.Load();

            Assert.AreEqual(100, loaded.frijolitos);
        }

        [Test]
        public void BothFilesCorrupt_StartsFresh()
        {
            system.Save(Sample(100));
            system.Save(Sample(250));
            File.WriteAllText(system.FilePath, "basura");
            File.WriteAllText(system.BackupPath, "más basura");

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Guardado ilegible"));
            var loaded = system.Load();

            Assert.AreEqual(0, loaded.frijolitos);
            Assert.AreEqual(0, loaded.levels.Count);
        }

        [Test]
        public void MissingFile_ReturnsNewData()
        {
            var loaded = system.Load();
            Assert.AreEqual(SaveData.CURRENT_VERSION, loaded.version);
            Assert.AreEqual(0, loaded.frijolitos);
        }

        [Test]
        public void Migrator_FillsMissingVersion()
        {
            var data = new SaveData { version = 0 };
            Assert.AreEqual(SaveData.CURRENT_VERSION, SaveMigrator.Migrate(data).version);
        }
    }
}
