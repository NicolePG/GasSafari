using System.IO;
using System.Text.RegularExpressions;
using GasSafari.Level;
using NUnit.Framework;
using UnityEditor;

namespace GasSafari.Tests
{
    // Los 31 LevelData y 6 WorldData coinciden con docs/04.
    public class LevelDataDocsTests
    {
        private const string LEVELS_FOLDER = "Assets/_Project/Data/Levels/";
        private const string WORLDS_FOLDER = "Assets/_Project/Data/Worlds/";
        private static readonly Regex SceneLine = new Regex(
            @"^## (?<id>T-0|W\d-\d) .*\n\n- Escena `(?<scene>\w+)` · Oro (?<gm>\d+):(?<gs>\d\d) · Plata (?<sm>\d+):(?<ss>\d\d) · Checkpoints (?<cp>\d+)",
            RegexOptions.Multiline);

        private static string DocText => File.ReadAllText("docs/04-niveles.md").Replace("\r\n", "\n");

        [Test]
        public void Levels_MatchDocs()
        {
            var matches = SceneLine.Matches(DocText);
            Assert.AreEqual(31, matches.Count);
            foreach (Match m in matches)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(LEVELS_FOLDER + m.Groups["id"].Value + ".asset");
                Assert.IsNotNull(level, m.Groups["id"].Value);
                Assert.AreEqual(m.Groups["id"].Value, level.id);
                Assert.AreEqual(m.Groups["scene"].Value, level.sceneName, level.id);
                Assert.AreEqual(int.Parse(m.Groups["gm"].Value) * 60 + int.Parse(m.Groups["gs"].Value), level.goldTime, level.id);
                Assert.AreEqual(int.Parse(m.Groups["sm"].Value) * 60 + int.Parse(m.Groups["ss"].Value), level.silverTime, level.id);
                Assert.AreEqual(int.Parse(m.Groups["cp"].Value), level.checkpointCount, level.id);
                Assert.AreEqual(3, level.mangoCount, level.id);
            }
        }

        [Test]
        public void SilverTime_IsGoldTimes1Point4RoundedUpToMultipleOf5()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_Project/Data/Levels" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                float expected = UnityEngine.Mathf.Ceil(level.goldTime * 1.4f / 5f - 1e-4f) * 5f;
                Assert.AreEqual(expected, level.silverTime, level.id);
            }
        }

        [Test]
        public void Worlds_HaveFiveLevelsLastIsBoss_AndDocsUnlocks()
        {
            int[] stars = { 0, 8, 20, 32, 45 };
            for (int w = 1; w <= 6; w++)
            {
                var world = AssetDatabase.LoadAssetAtPath<WorldData>(WORLDS_FOLDER + "W" + w + ".asset");
                Assert.IsNotNull(world, "W" + w);
                Assert.AreEqual(5, world.levels.Count, world.id);
                for (int i = 0; i < 5; i++)
                {
                    Assert.AreEqual(world.id + "-" + (i + 1), world.levels[i].id);
                    Assert.AreSame(world, world.levels[i].world);
                    Assert.AreEqual(i + 1, world.levels[i].orderInWorld);
                    Assert.AreEqual(i == 4, world.levels[i].isBoss, world.levels[i].id);
                }
                Assert.AreEqual(world.musicId + "_boss", world.levels[4].musicId);
                Assert.AreEqual(world.musicId, world.levels[0].musicId);
                if (w < 6)
                {
                    Assert.AreEqual(w == 1 ? WorldUnlockKind.None : WorldUnlockKind.Stars, world.unlockKind);
                    Assert.AreEqual(stars[w - 1], world.unlockAmount);
                }
                else
                {
                    Assert.AreEqual(WorldUnlockKind.GoldenMangoes, world.unlockKind);
                    Assert.AreEqual(60, world.unlockAmount);
                }
            }
        }

        [Test]
        public void Tutorial_HasNoWorldNorTimer_AndRaceTracksAreTheSix()
        {
            var tutorial = AssetDatabase.LoadAssetAtPath<LevelData>(LEVELS_FOLDER + "T-0.asset");
            Assert.IsNull(tutorial.world);
            Assert.IsFalse(tutorial.showTimer);
            Assert.AreEqual("mus_tutorial", tutorial.musicId);

            int race = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_Project/Data/Levels" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level.raceCompatible)
                    race++;
            }
            Assert.AreEqual(6, race);
            foreach (var id in new[] { "W1-3", "W2-2", "W3-2", "W4-1", "W5-1", "W5-5" })
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<LevelData>(LEVELS_FOLDER + id + ".asset").raceCompatible, id);
        }
    }
}
