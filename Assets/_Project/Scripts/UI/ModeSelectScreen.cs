using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Elegir modo (docs/06 §1, docs/07 §2): tarjetas de los 8 modos; los bloqueados muestran su requisito.
    public class ModeSelectScreen : MenuScreen
    {
        private class Card
        {
            public GameModeData Mode;
            public Button Button;
            public Image Icon;
            public GameObject LockBox;
            public LocalizedText LockText;
            public LocalizedText Players;
        }

        private readonly List<Card> cards = new List<Card>();
        private Button back;

        public override Selectable FirstSelected => cards.Count > 0 ? cards[0].Button : back;
        public IReadOnlyList<Button> CardButtons
        {
            get
            {
                var list = new List<Button>();
                foreach (var card in cards)
                    list.Add(card.Button);
                return list;
            }
        }

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "mode.select.title", 78f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 110f), new Vector2(0f, -36f));
            var subtitle = UiFactory.Label(Root, "Subtitle", "ui.mode_prompt", 40f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(subtitle.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 64f), new Vector2(0f, -140f));

            var catalog = GameCatalog.Instance;
            var modes = new List<GameModeData>(catalog != null ? catalog.modes : new GameModeData[0]);
            modes.Sort((a, b) => a.order.CompareTo(b.order));

            var grid = UiFactory.Grid(Root, "Cards", new Vector2(420f, 340f), new Vector2(26f, 20f), 4);
            UiFactory.Place((RectTransform)grid.transform, new Vector2(0.5f, 0.5f), new Vector2(4 * 420f + 3 * 26f, 2 * 340f + 20f), new Vector2(0f, -40f));
            grid.childAlignment = TextAnchor.UpperCenter;

            foreach (var mode in modes)
                cards.Add(BuildCard(grid.transform, mode));

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(320f, 100f), () => App.Back(), 44f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(320f, 100f), new Vector2(60f, 36f));
            LinkGrid(4);
        }

        private Card BuildCard(Transform parent, GameModeData mode)
        {
            var button = UiFactory.MakeCard(parent, "Mode_" + mode.id, new Vector2(420f, 340f));
            var card = new Card { Mode = mode, Button = button };
            var transform = button.transform;

            card.Icon = UiFactory.Picture(transform, "Icon", mode.icon, Color.white);
            UiFactory.Place(card.Icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(130f, 130f), new Vector2(0f, -22f));

            var name = UiFactory.Label(transform, "Name", mode.NameKey, 44f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(name.Label.rectTransform, new Vector2(0f, 0.389f), new Vector2(1f, 0.544f), new Vector4(24f, 0f, 24f, 0f));
            var desc = UiFactory.Label(transform, "Desc", mode.DescKey, 28f, TextAlignmentOptions.Top, new Color(0.25f, 0.23f, 0.35f));
            UiFactory.Region(desc.Label.rectTransform, new Vector2(0f, 0.167f), new Vector2(1f, 0.383f), new Vector4(30f, 0f, 30f, 0f));

            card.Players = UiFactory.Label(transform, "Players", "mode.players", 26f, TextAlignmentOptions.Center, new Color(0.4f, 0.38f, 0.55f), FontStyles.Bold,
                mode.minPlayers == mode.maxPlayers ? mode.minPlayers.ToString() : mode.minPlayers + "-" + mode.maxPlayers);
            UiFactory.Region(card.Players.Label.rectTransform, new Vector2(0f, 0.03f), new Vector2(1f, 0.16f), new Vector4(20f, 0f, 20f, 0f));

            // Bloqueado: velo oscuro con candado y el requisito.
            var veil = UiFactory.Picture(transform, "Lock", (Sprite)null, new Color(0.08f, 0.07f, 0.16f, 0.94f), false);
            UiFactory.Stretch(veil.rectTransform);
            veil.raycastTarget = false;
            card.LockBox = veil.gameObject;
            var lockIcon = UiFactory.Picture(veil.transform, "Padlock", "Icon_Lock", Color.white);
            UiFactory.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(110f, 110f), Vector2.zero);
            card.LockText = UiFactory.Label(veil.transform, "Requirement", "lock.any_level", 32f, TextAlignmentOptions.Top, Color.white, FontStyles.Bold);
            UiFactory.Region(card.LockText.Label.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.44f), new Vector4(30f, 0f, 30f, 0f));

            var captured = card;
            button.onClick.AddListener(() => Choose(captured));
            return card;
        }

        // Navegación de la rejilla: izquierda/derecha por fila, arriba/abajo por columna, y "Atrás" debajo de la fila de abajo.
        private void LinkGrid(int columns)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % columns > 0) nav.selectOnLeft = cards[i - 1].Button;
                if (i % columns < columns - 1 && i + 1 < cards.Count) nav.selectOnRight = cards[i + 1].Button;
                if (i >= columns) nav.selectOnUp = cards[i - columns].Button;
                nav.selectOnDown = i + columns < cards.Count ? cards[i + columns].Button : back;
                cards[i].Button.navigation = nav;
            }
            var backNav = new Navigation { mode = Navigation.Mode.Explicit };
            backNav.selectOnUp = cards.Count > 0 ? cards[0].Button : null;
            back.navigation = backNav;
        }

        public override void OnShow()
        {
            var data = Services.IsReady && Services.Progression != null ? Services.Progression.Data : new Save.SaveData();
            foreach (var card in cards)
            {
                bool unlocked = ProgressionRules.IsModeUnlocked(card.Mode.id, data);
                card.LockBox.SetActive(!unlocked);
                if (!unlocked)
                {
                    var requirement = ModeRequirement(card.Mode.id);
                    card.LockText.SetKey(requirement.key, requirement.args);
                }
            }
        }

        // Texto del requisito de desbloqueo de cada modo (docs/06 §1).
        public static (string key, object[] args) ModeRequirement(string modeId)
        {
            switch (modeId)
            {
                case ProgressionRules.MODE_TIME_TRIAL: return ("lock.any_level", new object[0]);
                case ProgressionRules.MODE_MIRROR: return ("lock.level", new object[] { "W5-5" });
                case ProgressionRules.MODE_MODIFIERS: return ("lock.stars", new object[] { 30 });
                case ProgressionRules.MODE_PARTY:
                case ProgressionRules.MODE_TEAM_RACE: return ("lock.level", new object[] { "W1-5" });
                case ProgressionRules.MODE_ENDLESS:
                case ProgressionRules.MODE_DAILY: return ("lock.level", new object[] { "W2-5" });
                default: return ("lock.title", new object[0]);
            }
        }

        private void Choose(Card card)
        {
            var data = Services.IsReady && Services.Progression != null ? Services.Progression.Data : new Save.SaveData();
            if (!ProgressionRules.IsModeUnlocked(card.Mode.id, data))
            {
                var requirement = ModeRequirement(card.Mode.id);
                App.Toast(requirement.key, requirement.args);
                return;
            }
            if (!card.Mode.implemented)
            {
                App.Toast("ui.soon");
                return;
            }
            if (Services.IsReady)
                Services.Session.ModeId = card.Mode.id;
            App.Push<PlayTypeScreen>();
        }
    }

    // Local / Crear sala online / Unirse con código (docs/07 §1). Las salas online llegan con M8.
    public class PlayTypeScreen : MenuScreen
    {
        private Button local;
        private Button create;
        private Button join;
        private Button back;

        public override Selectable FirstSelected => local;
        public Button LocalButton => local;
        public Button CreateButton => create;
        public Button JoinButton => join;

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "mode.select.title", 74f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 110f), new Vector2(0f, -60f));

            var list = UiFactory.VerticalList(Root, "Buttons", 22f);
            UiFactory.Place((RectTransform)list.transform, new Vector2(0.5f, 0.5f), new Vector2(760f, 4 * 132f), new Vector2(0f, -30f));
            local = UiFactory.MakeButton(list.transform, "Local", "mode.select.local", ButtonStyle.Green, new Vector2(760f, 120f), () => App.Push<LobbyScreen>(), 50f);
            create = UiFactory.MakeButton(list.transform, "Create", "mode.select.create_room", ButtonStyle.Blue, new Vector2(760f, 120f), () => App.Toast("ui.soon"), 46f);
            join = UiFactory.MakeButton(list.transform, "Join", "mode.select.join_room", ButtonStyle.Blue, new Vector2(760f, 120f), () => App.Toast("ui.soon"), 46f);
            back = UiFactory.MakeButton(list.transform, "Back", "ui.back", ButtonStyle.Red, new Vector2(760f, 120f), () => App.Back(), 46f);
            UiFactory.LinkVertical(local, create, join, back);
        }
    }
}
