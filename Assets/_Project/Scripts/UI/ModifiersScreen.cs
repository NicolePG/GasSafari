using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Menú de modificadores antes del nivel (docs/06 §1): 8 tarjetas que se encienden y apagan (se combinan), con candado
    // por estrellas, el aviso "Con modificadores no hay estrellas" y Jugar.
    public class ModifiersScreen : MenuScreen
    {
        private static readonly Color OnTint = new Color(0.62f, 1f, 0.62f);

        private class Card
        {
            public ModifierData Data;
            public Button Button;
            public Image Face;
            public GameObject LockBox;
            public LocalizedText LockText;
            public LocalizedText State;
        }

        private readonly List<Card> cards = new List<Card>();
        private Button play;
        private Button back;

        public LevelData Level { get; set; }
        public override Selectable FirstSelected => cards.Count > 0 ? cards[0].Button : play;
        public Button PlayButton => play;
        public int CardCount => cards.Count;
        public Button CardButton(int i) => cards[i].Button;
        public bool IsLocked(int i) => cards[i].LockBox.activeSelf;
        public string CardId(int i) => cards[i].Data.id;

        private static SaveData Data => Services.IsReady && Services.Progression != null ? Services.Progression.Data : new SaveData();
        private static List<string> Selection => Services.IsReady ? Services.Session.Modifiers : new List<string>();

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "mod.title", 74f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 100f), new Vector2(0f, -36f));
            var warning = UiFactory.Label(Root, "Warning", "mod.warning", 38f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(warning.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 60f), new Vector2(0f, -136f));

            var config = ModesConfig.Instance;
            var list = new List<ModifierData>(config != null ? config.modifiers : new ModifierData[0]);
            list.RemoveAll(m => m == null);
            list.Sort((a, b) => a.order.CompareTo(b.order));

            var grid = UiFactory.Grid(Root, "Cards", new Vector2(420f, 300f), new Vector2(26f, 20f), 4);
            UiFactory.Place((RectTransform)grid.transform, new Vector2(0.5f, 0.5f), new Vector2(4 * 420f + 3 * 26f, 2 * 300f + 20f), new Vector2(0f, -10f));
            foreach (var modifier in list)
                cards.Add(BuildCard(grid.transform, modifier));

            var buttons = UiFactory.HorizontalList(Root, "Buttons", 30f);
            UiFactory.Place((RectTransform)buttons.transform, new Vector2(0.5f, 0f), new Vector2(900f, 110f), new Vector2(0f, 40f));
            play = UiFactory.MakeButton(buttons.transform, "Play", "sheet.play", ButtonStyle.Green, new Vector2(400f, 104f), Play, 46f);
            back = UiFactory.MakeButton(buttons.transform, "Back", "ui.back", ButtonStyle.Red, new Vector2(320f, 104f), () => App.Back(), 42f);
            UiFactory.LinkHorizontal(play, back);
            LinkGrid(4);
        }

        private Card BuildCard(Transform parent, ModifierData data)
        {
            var button = UiFactory.MakeCard(parent, "Modifier_" + data.id, new Vector2(420f, 300f));
            var card = new Card { Data = data, Button = button, Face = button.GetComponent<Image>() };
            var t = button.transform;
            var name = UiFactory.Label(t, "Name", data.NameKey, 42f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(name.Label.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.9f), new Vector4(20f, 0f, 20f, 0f));
            var desc = UiFactory.Label(t, "Desc", data.DescKey, 28f, TextAlignmentOptions.Top, new Color(0.25f, 0.23f, 0.35f));
            UiFactory.Region(desc.Label.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.6f), new Vector4(26f, 0f, 26f, 0f));
            card.State = UiFactory.Label(t, "State", "ui.off", 30f, TextAlignmentOptions.Center, new Color(0.4f, 0.38f, 0.55f), FontStyles.Bold);
            UiFactory.Region(card.State.Label.rectTransform, new Vector2(0f, 0.05f), new Vector2(1f, 0.25f));

            var veil = UiFactory.Picture(t, "Lock", (Sprite)null, new Color(0.08f, 0.07f, 0.16f, 0.94f), false);
            UiFactory.Stretch(veil.rectTransform);
            veil.raycastTarget = false;
            card.LockBox = veil.gameObject;
            var padlock = UiFactory.Picture(veil.transform, "Padlock", "Icon_Lock", Color.white);
            UiFactory.Place(padlock.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(96f, 96f), Vector2.zero);
            card.LockText = UiFactory.Label(veil.transform, "Requirement", "lock.stars", 32f, TextAlignmentOptions.Top, Color.white, FontStyles.Bold, data.starsRequired);
            UiFactory.Region(card.LockText.Label.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.42f), new Vector4(26f, 0f, 26f, 0f));

            var captured = card;
            button.onClick.AddListener(() => Toggle(captured));
            return card;
        }

        private void LinkGrid(int columns)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % columns > 0) nav.selectOnLeft = cards[i - 1].Button;
                if (i % columns < columns - 1 && i + 1 < cards.Count) nav.selectOnRight = cards[i + 1].Button;
                if (i >= columns) nav.selectOnUp = cards[i - columns].Button;
                nav.selectOnDown = i + columns < cards.Count ? cards[i + columns].Button : play;
                cards[i].Button.navigation = nav;
            }
            var playNav = play.navigation;
            playNav.selectOnUp = cards.Count > 0 ? cards[0].Button : null;
            play.navigation = playNav;
        }

        public override void OnShow()
        {
            var data = Data;
            // Un modificador que ya no está desbloqueado (otro guardado) sale de la selección.
            Selection.RemoveAll(id =>
            {
                var modifier = ModesConfig.Instance != null ? ModesConfig.Instance.FindModifier(id) : null;
                return modifier == null || !ProgressionRules.IsModifierUnlocked(modifier.starsRequired, data);
            });
            foreach (var card in cards)
                Refresh(card, data);
        }

        private void Refresh(Card card, SaveData data)
        {
            bool unlocked = ProgressionRules.IsModifierUnlocked(card.Data.starsRequired, data);
            bool on = Selection.Contains(card.Data.id);
            card.LockBox.SetActive(!unlocked);
            card.Face.color = on ? OnTint : Color.white;
            card.State.SetKey(on ? "ui.on" : "ui.off");
        }

        public void Toggle(int index) => Toggle(cards[index]);

        private void Toggle(Card card)
        {
            var data = Data;
            if (!ProgressionRules.IsModifierUnlocked(card.Data.starsRequired, data))
            {
                App.Toast("lock.stars", card.Data.starsRequired);
                return;
            }
            var selection = Selection;
            if (!selection.Remove(card.Data.id))
                selection.Add(card.Data.id);
            Refresh(card, data);
        }

        private void Play()
        {
            if (Selection.Count == 0)
            {
                App.Toast("mod.choose_one");
                return;
            }
            ModeLauncher.Launch(Level, ProgressionRules.MODE_MODIFIERS);
        }
    }
}
