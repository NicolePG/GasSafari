using System.Collections.Generic;
using System.Linq;
using GasSafari.Core;
using GasSafari.Editor;
using GasSafari.Input;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GasSafari.Tests
{
    // M7-02 Espejo (docs/06 §1): entrada y pantalla invertidas, dificultad mínima Normal, y que cada uno de los 31 niveles
    // se pueda invertir con una sola cámara (la que MirrorScreen dibuja al revés).
    public class MirrorTests
    {
        [TearDown]
        public void TearDown() => MirrorView.Active = false;

        [Test]
        public void Commands_AreMirroredInX_OnlyWhileActive()
        {
            var command = new PlayerCommand { SeatMove = 0.7f, Aim = -0.4f, Swim = new Vector2(0.5f, 0.8f), Ping = PingType.TurnLeft, FartHeld = true };
            var same = MirrorView.Apply(command);
            Assert.AreEqual(0.7f, same.SeatMove);
            Assert.AreEqual(PingType.TurnLeft, same.Ping);

            MirrorView.Active = true;
            var mirrored = MirrorView.Apply(command);
            Assert.AreEqual(-0.7f, mirrored.SeatMove, "moverse por el borde hacia la derecha de la pantalla");
            Assert.AreEqual(0.4f, mirrored.Aim);
            Assert.AreEqual(new Vector2(-0.5f, 0.8f), mirrored.Swim, "nadar: solo cambia el lado");
            Assert.AreEqual(PingType.TurnRight, mirrored.Ping, "\"gira a la izquierda\" de la pantalla es la derecha del mundo");
            Assert.IsTrue(mirrored.FartHeld);
            Assert.AreEqual(PingType.Now, MirrorView.Swap(PingType.Now));
        }

        [Test]
        public void HudArrows_UseTheMirroredViewport()
        {
            Assert.AreEqual(0.2f, MirrorView.Viewport(new Vector3(0.2f, 0.3f, 5f)).x, 1e-5f);
            MirrorView.Active = true;
            var view = MirrorView.Viewport(new Vector3(0.2f, 0.3f, 5f));
            Assert.AreEqual(0.8f, view.x, 1e-5f, "un peligro a la izquierda del mundo se ve a la derecha");
            Assert.AreEqual(0.3f, view.y, 1e-5f);
            Assert.AreEqual(5f, view.z);
            Assert.AreEqual(1.3f, MirrorView.Viewport(new Vector3(-0.3f, 0f, 1f)).x, 1e-5f, "fuera de pantalla sigue fuera, del otro lado");
        }

        [Test]
        public void Mirror_RaisesFamiliarToNormal_AndGivesItBackLater()
        {
            var catalog = GameCatalog.Instance;
            var familiar = catalog.difficulties.First(d => d.id == "Familiar");
            var normal = catalog.difficulties.First(d => d.id == "Normal");
            var hard = catalog.difficulties.First(d => d.id == "AmistadesRotas");
            var go = new GameObject("Session");
            try
            {
                var session = go.AddComponent<GameSession>();
                session.Difficulty = familiar;
                ModeLauncher.Prepare(session, ProgressionRules.MODE_MIRROR);
                Assert.AreSame(normal, session.Difficulty, "docs/06: mínimo dificultad Normal");
                ModeLauncher.Prepare(session, ProgressionRules.MODE_MIRROR);
                Assert.AreSame(normal, session.Difficulty);
                ModeLauncher.Prepare(session, ProgressionRules.MODE_STORY);
                Assert.AreSame(familiar, session.Difficulty, "fuera del Espejo vuelve la elegida");

                session.Difficulty = hard;
                ModeLauncher.Prepare(session, ProgressionRules.MODE_MIRROR);
                Assert.AreSame(hard, session.Difficulty, "Amistades Rotas no se toca");
                Assert.IsNull(session.DifficultyBeforeMirror);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Data_AndTexts_FollowTheDocs()
        {
            Assert.AreEqual(1.5f, ModesConfig.Instance.mirrorBeanMultiplier, "docs/06: frijolitos x1.5");
            Assert.AreEqual("Normal", ModesConfig.Instance.mirrorMinDifficulty);
            Assert.AreEqual(ProgressionRules.MODE_MIRROR, ModeLauncher.RunModeFor(ProgressionRules.MODE_MIRROR));
            Assert.IsTrue(GameCatalog.Instance.modes.First(m => m.id == ProgressionRules.MODE_MIRROR).implemented);
            Assert.AreEqual("mirror.bonus", ResultsScreen.ModeNote(new Level.LevelResults { RunMode = ProgressionRules.MODE_MIRROR }).key);
            foreach (var language in new[] { "es", "en" })
                Assert.AreNotEqual("mirror.bonus", LocalizedText.Table.Get("mirror.bonus", language));
        }

        // Lo que MirrorScreen no puede invertir: otra cámara que dibuje directo en pantalla quedaría tapada por la imagen
        // invertida, y un lienzo "Screen Space - Camera" se invertiría con el nivel.
        [Test]
        public void EveryLevel_HasOneScreenCamera_AndNoCameraSpaceCanvas()
        {
            var problems = new List<string>();
            var report = new List<string>();
            foreach (var name in CollisionAudit.Scenes)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/Juego/Niveles/{name}.unity", OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true))
                    .Where(c => c.enabled && c.gameObject.activeInHierarchy && c.targetTexture == null).ToArray();
                var mains = cameras.Where(c => c.CompareTag("MainCamera")).ToArray();
                if (mains.Length != 1)
                    problems.Add($"{name}: {mains.Length} cámaras MainCamera");
                foreach (var other in cameras.Where(c => !c.CompareTag("MainCamera")))
                    problems.Add($"{name}: la cámara {other.name} dibuja en pantalla y el Espejo no la invierte");
                if (mains.Length == 1 && mains[0].GetComponent("CinemachineBrain") == null)
                    problems.Add($"{name}: la cámara principal no tiene CinemachineBrain (las tomas de cámara no pasarían por el Espejo)");
                foreach (var canvas in roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.renderMode == RenderMode.ScreenSpaceCamera))
                    problems.Add($"{name}: el lienzo {canvas.name} es Screen Space - Camera");
                int texts = roots.SelectMany(r => r.GetComponentsInChildren<TMPro.TextMeshPro>(true)).Count();
                int worldCanvases = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Count(c => c.renderMode == RenderMode.WorldSpace && c.isRootCanvas);
                report.Add($"{name}: textos 3D {texts}, lienzos de mundo {worldCanvases}");
            }
            Debug.Log("MIRROR audit " + string.Join(" | ", report));
            Assert.AreEqual(31, CollisionAudit.Scenes.Length, "los 31 niveles");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
