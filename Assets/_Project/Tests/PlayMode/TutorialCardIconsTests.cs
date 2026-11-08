using System.Collections;
using GasSafari.Level;
using GasSafari.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // El cartel del tutorial muestra los iconos Kenney del dispositivo, no el texto.
    public class TutorialCardIconsTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.Destroy(root);
        }

        private TutorialCard BuildCard()
        {
            root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(root.transform, false);
            TMP_Text Text(string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(panel.transform, false);
                ((RectTransform)go.transform).sizeDelta = new Vector2(860f, 55f);
                return go.GetComponent<TextMeshProUGUI>();
            }
            var card = root.AddComponent<TutorialCard>();
            card.Configure((RectTransform)panel.transform, panel.GetComponent<CanvasGroup>(), Text("Title"), Text("Body"), Text("Buttons"));
            return card;
        }

        private static TutorialStep Step(TutorialAction action)
        {
            var step = ScriptableObject.CreateInstance<TutorialStep>();
            step.title = "Prueba";
            step.text = "Cuerpo";
            step.action = action;
            return step;
        }

        [UnityTest]
        public IEnumerator Card_ShowsIconsInsteadOfText()
        {
            var card = BuildCard();
            yield return null;
            card.Show(Step(TutorialAction.Charge), null);
            yield return null;

            var row = root.transform.Find("Panel/Icons");
            Assert.IsNotNull(row, "el cartel arma su fila de iconos");
            var images = row.GetComponentsInChildren<Image>();
            Assert.AreEqual(2, images.Length, "LT + RT: dos iconos de Xbox (por defecto sin jugadores)");
            Assert.AreEqual("xbox_lt", images[0].sprite.name);
            Assert.AreEqual("xbox_rt", images[1].sprite.name);
            Assert.IsFalse(root.transform.Find("Panel/Buttons").GetComponent<TMP_Text>().enabled, "el texto se apaga mientras hay iconos");
            Assert.IsNotEmpty(card.Buttons, "el texto queda de respaldo");
        }
    }
}
