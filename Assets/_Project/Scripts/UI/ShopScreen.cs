using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Tienda de cosméticos (docs/06 §2, docs/07 §2): pestañas Sombreros / Gas / Balsas / Celebraciones, saldo de frijolitos,
    // vista previa 3D giratoria, comprar y equipar. Sombreros, gas y celebraciones son del jugador 1; la balsa es del equipo.
    public class ShopScreen : MenuScreen
    {
        public const int COLUMNS = 4;
        private static readonly CosmeticCategory[] Categories =
            { CosmeticCategory.Hat, CosmeticCategory.Gas, CosmeticCategory.Raft, CosmeticCategory.Celebration };
        private static readonly string[] TabKeys = { "shop.tab.hats", "shop.tab.gas", "shop.tab.rafts", "shop.tab.celebrations" };

        public class Card
        {
            public CosmeticData Item;
            public Button Button;
            public Image Icon;
            public LocalizedText Name;
            public LocalizedText Status;
            public Image BeanIcon;
        }

        private readonly Button[] tabs = new Button[4];
        private readonly List<Card> cards = new List<Card>();
        private RectTransform gridRect;
        private GridLayoutGroup grid;
        private Button back;
        private Button action;
        private LocalizedText actionLabel;
        private LocalizedText balance;
        private LocalizedText detailName;
        private LocalizedText detailInfo;
        private LocalizedText message;
        private RawImage previewImage;
        private Image gasImage;
        private ShopPreview preview;
        private int tab;
        private Card selected;
        private int ownerPlayer;

        public IReadOnlyList<Card> Cards => cards;
        public Card Selected => selected;
        public int CurrentTab => tab;
        public Button[] Tabs => tabs;
        public Button ActionButton => action;
        public string BalanceText => balance != null ? balance.Label.text : string.Empty;
        public string MessageText => message != null ? message.Label.text : string.Empty;
        public ShopPreview Preview => preview;

        public override Selectable FirstSelected => cards.Count > 0 ? cards[0].Button : tabs[tab];

        // ---------------------------------------------------------------- construcción
        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "shop.title", 74f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0f, 1f), new Vector2(600f, 100f), new Vector2(60f, -22f));

            var bean = UiFactory.Picture(Root, "BeanIcon", "Icon_Bean", Color.white);
            UiFactory.Place(bean.rectTransform, new Vector2(1f, 1f), new Vector2(66f, 66f), new Vector2(-440f, -34f));
            balance = UiFactory.Label(Root, "Balance", "shop.balance", 42f, TextAlignmentOptions.MidlineLeft, UiFactory.Gold, FontStyles.Bold, 0).Outlined();
            UiFactory.Place(balance.Label.rectTransform, new Vector2(1f, 1f), new Vector2(360f, 80f), new Vector2(-60f, -28f));

            var tabRow = UiFactory.HorizontalList(Root, "Tabs", 14f);
            UiFactory.Place((RectTransform)tabRow.transform, new Vector2(0f, 1f), new Vector2(1180f, 86f), new Vector2(50f, -126f));
            for (int i = 0; i < tabs.Length; i++)
            {
                int captured = i;
                tabs[i] = UiFactory.MakeButton(tabRow.transform, "Tab" + i, TabKeys[i], ButtonStyle.Blue, new Vector2(280f, 84f), () => SetTab(captured), 30f);
            }
            UiFactory.LinkHorizontal(tabs);

            // Rejilla de artículos.
            var panel = UiFactory.Panel(Root, "ItemsPanel", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(1200f, 740f), new Vector2(50f, 130f));
            panel.raycastTarget = false;
            grid = UiFactory.Grid(panel.transform, "Grid", new Vector2(268f, 216f), new Vector2(14f, 14f), COLUMNS);
            gridRect = (RectTransform)grid.transform;
            UiFactory.Place(gridRect, new Vector2(0.5f, 1f), new Vector2(COLUMNS * 268f + 3 * 14f, 3 * 216f + 2 * 14f), new Vector2(0f, -22f));
            grid.childAlignment = TextAnchor.UpperCenter;

            // Vista previa y detalle.
            var detail = UiFactory.Panel(Root, "Detail", "Panel_Light");
            UiFactory.Place(detail.rectTransform, new Vector2(1f, 0f), new Vector2(620f, 740f), new Vector2(-50f, 130f));
            detail.raycastTarget = false;
            preview = ShopPreview.Create(transform);
            var previewBack = UiFactory.Panel(detail.transform, "PreviewFrame", "Panel_Inset");
            UiFactory.Place(previewBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(500f, 400f), new Vector2(0f, -26f));
            previewBack.raycastTarget = false;
            var raw = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
            raw.transform.SetParent(previewBack.transform, false);
            previewImage = raw.GetComponent<RawImage>();
            previewImage.texture = preview.Texture;
            previewImage.raycastTarget = false;
            UiFactory.Stretch(previewImage.rectTransform, 26f);
            var hint = UiFactory.Label(previewBack.transform, "Hint", "shop.preview_hint", 24f, TextAlignmentOptions.Bottom, new Color(0.9f, 0.9f, 1f, 0.9f));
            UiFactory.Region(hint.Label.rectTransform, Vector2.zero, new Vector2(1f, 0.16f), new Vector4(20f, 8f, 20f, 0f));
            gasImage = UiFactory.Picture(previewBack.transform, "GasPreview", (Sprite)null, Color.white);
            UiFactory.Place(gasImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(300f, 300f), Vector2.zero);
            gasImage.gameObject.SetActive(false);

            detailName = UiFactory.Label(detail.transform, "Name", "shop.hat.zoo_cap", 44f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            DetailRow(detailName.Label.rectTransform, 430f, 64f);
            detailInfo = UiFactory.Label(detail.transform, "Info", "shop.starter", 30f, TextAlignmentOptions.Center, new Color(0.35f, 0.3f, 0.5f), FontStyles.Bold);
            DetailRow(detailInfo.Label.rectTransform, 494f, 44f);
            action = UiFactory.MakeButton(detail.transform, "Action", null, ButtonStyle.Green, new Vector2(400f, 100f), DoAction, 44f);
            UiFactory.Place((RectTransform)action.transform, new Vector2(0.5f, 0f), new Vector2(400f, 100f), new Vector2(0f, 90f));
            actionLabel = UiFactory.Label(action.transform, "Label", "shop.buy", 44f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(actionLabel.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(20f, 20f, 20f, 12f));
            message = UiFactory.Label(detail.transform, "Message", "shop.not_enough", 28f, TextAlignmentOptions.Center, new Color(0.8f, 0.2f, 0.2f), FontStyles.Bold);
            UiFactory.Place(message.Label.rectTransform, new Vector2(0.5f, 0f), new Vector2(560f, 56f), new Vector2(0f, 28f));
            message.gameObject.SetActive(false);

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(300f, 80f), () => App.Back(), 38f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(300f, 80f), new Vector2(50f, 34f));

            BuildCards();
            SetTab(0, false);
        }

        private static void DetailRow(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(30f, -top - height);
            rect.offsetMax = new Vector2(-30f, -top);
        }

        // Una tarjeta por artículo del catálogo; solo se ven las de la pestaña elegida.
        private void BuildCards()
        {
            var catalog = GameCatalog.Instance;
            if (catalog == null)
                return;
            foreach (var item in catalog.cosmetics)
                cards.Add(BuildCard(item));
        }

        private Card BuildCard(CosmeticData item)
        {
            var button = UiFactory.MakeCard(grid.transform, "Item_" + item.id, new Vector2(268f, 216f));
            var card = new Card { Item = item, Button = button };
            card.Icon = UiFactory.Picture(button.transform, "Icon", item.icon, Color.white);
            UiFactory.Place(card.Icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 110f), new Vector2(0f, -10f));
            card.Name = UiFactory.Label(button.transform, "Name", item.nameKey, 26f, TextAlignmentOptions.Top, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(card.Name.Label.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.46f), new Vector4(10f, 0f, 10f, 0f));
            card.Status = UiFactory.Label(button.transform, "Status", "shop.owned", 26f, TextAlignmentOptions.Center, new Color(0.3f, 0.5f, 0.3f), FontStyles.Bold, 0);
            UiFactory.Region(card.Status.Label.rectTransform, new Vector2(0f, 0.01f), new Vector2(1f, 0.25f), new Vector4(56f, 0f, 10f, 2f));
            card.BeanIcon = UiFactory.Picture(button.transform, "Bean", "Icon_Bean", Color.white);
            UiFactory.Place(card.BeanIcon.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(18f, 8f));
            if (item.provisional)
                card.Icon.color = Color.Lerp(Color.white, item.tint, 0.5f);

            var captured = card;
            button.onClick.AddListener(() => Choose(captured));
            var follower = button.gameObject.AddComponent<CardFocus>();
            follower.Owner = this;
            follower.Card = card;
            return card;
        }

        private class CardFocus : MonoBehaviour, ISelectHandler, IPointerEnterHandler
        {
            public ShopScreen Owner;
            public Card Card;

            public void OnSelect(BaseEventData eventData) => Owner.Show(Card);
            public void OnPointerEnter(PointerEventData eventData) => Owner.Show(Card);
        }

        // ---------------------------------------------------------------- pestañas
        public void SetTab(int index, bool refresh = true)
        {
            tab = Mathf.Clamp(index, 0, tabs.Length - 1);
            for (int i = 0; i < tabs.Length; i++)
                SetTabStyle(tabs[i], i == tab ? "Green" : "Blue");
            var visible = new List<Card>();
            foreach (var card in cards)
            {
                bool inTab = card.Item.category == Categories[tab];
                card.Button.gameObject.SetActive(inTab);
                if (inTab)
                    visible.Add(card);
            }
            LinkGrid(visible);
            selected = visible.Count > 0 ? visible[0] : null;
            if (refresh)
                Refresh();
        }

        private static void SetTabStyle(Button button, string style)
        {
            button.GetComponent<Image>().sprite = UiFactory.Sprite("Button_" + style + "_Normal");
            button.spriteState = new SpriteState
            {
                highlightedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                selectedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                pressedSprite = UiFactory.Sprite("Button_" + style + "_Pressed"),
                disabledSprite = UiFactory.Sprite("Button_" + style + "_Disabled")
            };
        }

        private void LinkGrid(List<Card> visible)
        {
            for (int i = 0; i < visible.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % COLUMNS > 0) nav.selectOnLeft = visible[i - 1].Button;
                if (i % COLUMNS < COLUMNS - 1 && i + 1 < visible.Count) nav.selectOnRight = visible[i + 1].Button;
                else nav.selectOnRight = action;
                nav.selectOnUp = i >= COLUMNS ? visible[i - COLUMNS].Button : tabs[tab];
                nav.selectOnDown = i + COLUMNS < visible.Count ? visible[i + COLUMNS].Button : back;
                visible[i].Button.navigation = nav;
            }
            for (int i = 0; i < tabs.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = i > 0 ? tabs[i - 1] : null;
                nav.selectOnRight = i < tabs.Length - 1 ? tabs[i + 1] : null;
                nav.selectOnDown = visible.Count > 0 ? visible[0].Button : back;
                tabs[i].navigation = nav;
            }
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = visible.Count > 0 ? visible[visible.Count - 1].Button : tabs[tab] };
            action.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = visible.Count > 0 ? visible[Mathf.Min(COLUMNS - 1, visible.Count - 1)].Button : null, selectOnUp = tabs[tab] };
        }

        // ---------------------------------------------------------------- estado
        public override void OnShow()
        {
            if (Services.Cosmetics != null)
                Services.Cosmetics.Changed += Refresh;
            Localization.Changed += Refresh;
            preview.gameObject.SetActive(true);
            SetTab(tab);
        }

        public override void OnHide()
        {
            if (Services.Cosmetics != null)
                Services.Cosmetics.Changed -= Refresh;
            Localization.Changed -= Refresh;
            preview.gameObject.SetActive(false);
        }

        protected override void Teardown()
        {
            if (Services.Cosmetics != null)
                Services.Cosmetics.Changed -= Refresh;
            Localization.Changed -= Refresh;
        }

        public void Refresh()
        {
            var service = Services.Cosmetics;
            int beans = Services.IsReady && Services.Progression != null ? Services.Progression.Beans : 0;
            balance.SetKey("shop.balance", beans);
            foreach (var card in cards)
                RefreshCard(card, service);
            if (selected != null)
                Show(selected);
        }

        private void RefreshCard(Card card, CosmeticsService service)
        {
            var item = card.Item;
            bool owned = service != null && service.IsOwned(item);
            string equipped = service != null ? service.Equipped(ownerPlayer, item.category) : string.Empty;
            bool isEquipped = owned && equipped == item.id;
            card.BeanIcon.gameObject.SetActive(!owned && item.source == CosmeticSource.Price);
            card.Icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.8f);
            card.Status.gameObject.SetActive(true);
            if (isEquipped)
                card.Status.SetKey("shop.equipped");
            else if (owned)
                card.Status.SetKey(item.source == CosmeticSource.Starter ? "shop.starter" : "shop.owned");
            else
                card.Status.SetLiteral(StatusText(item));
        }

        // Precio en frijolitos, o cómo se consigue un premio.
        private static string StatusText(CosmeticData item)
        {
            var table = LocalizedText.Table;
            switch (item.source)
            {
                case CosmeticSource.Price: return item.price.ToString();
                case CosmeticSource.MangoMilestone: return table != null ? string.Format(table.Get("shop.reward_mangos"), item.mangosRequired) : item.mangosRequired.ToString();
                case CosmeticSource.FinishW65: return table != null ? table.Get("shop.reward_w65") : "W6-5";
                default: return string.Empty;
            }
        }

        // Muestra un artículo en la vista previa y el detalle.
        public void Show(Card card)
        {
            selected = card;
            if (card == null)
                return;
            var item = card.Item;
            var service = Services.Cosmetics;
            bool owned = service != null && service.IsOwned(item);
            bool isEquipped = owned && service.Equipped(ownerPlayer, item.category) == item.id;

            detailName.SetKey(item.nameKey);
            gasImage.gameObject.SetActive(item.category == CosmeticCategory.Gas);
            previewImage.gameObject.SetActive(item.category != CosmeticCategory.Gas);
            if (item.category == CosmeticCategory.Gas)
            {
                gasImage.sprite = item.icon;
                gasImage.enabled = item.icon != null;
            }
            else
            {
                preview.Show(item, PreviewCharacter(), GameCatalog.Instance != null ? GameCatalog.Instance.hatAnchors : null);
            }

            if (item.teamItem)
                detailInfo.SetKey("shop.team_item");
            else if (isEquipped)
                detailInfo.SetKey("shop.equipped");
            else if (owned)
                detailInfo.SetKey(item.source == CosmeticSource.Starter ? "shop.starter" : "shop.owned");
            else
                detailInfo.SetLiteral(StatusText(item));

            if (isEquipped && (item.category == CosmeticCategory.Hat || item.category == CosmeticCategory.Celebration))
                actionLabel.SetKey("shop.equipped");
            else if (isEquipped)
                actionLabel.SetKey("shop.equipped");
            else if (owned)
                actionLabel.SetKey("shop.equip");
            else
                actionLabel.SetKey("shop.buy");
            action.interactable = !isEquipped && (owned || item.source == CosmeticSource.Price);
            message.gameObject.SetActive(false);
        }

        // El animal de la vista previa: el del primer jugador, o Alex.
        private CharacterData PreviewCharacter()
        {
            var catalog = GameCatalog.Instance;
            if (catalog == null || catalog.characters.Length == 0)
                return null;
            if (Services.IsReady)
            {
                foreach (var slot in Services.Session.Players)
                {
                    if (!slot.IsBot && slot.Character != null)
                        return slot.Character;
                }
            }
            return catalog.FindCharacter("Alex") ?? catalog.characters[0];
        }

        // ---------------------------------------------------------------- acciones
        private void Choose(Card card)
        {
            Show(card);
            DoAction();
        }

        public void DoAction()
        {
            if (selected == null || Services.Cosmetics == null)
                return;
            var item = selected.Item;
            var service = Services.Cosmetics;
            if (!service.IsOwned(item))
            {
                if (item.source != CosmeticSource.Price)
                    return;
                if (!service.CanAfford(item))
                {
                    message.SetKey("shop.not_enough");
                    message.gameObject.SetActive(true);
                    return;
                }
                if (!service.TryBuy(item))
                    return;
            }
            // Todo lo comprado se pone al instante; el sombrero y la celebración se quitan eligiendo de nuevo en la selección.
            service.Equip(ownerPlayer, item.category, item);
            Refresh();
        }

        private void Update()
        {
            if (gasImage != null && gasImage.gameObject.activeInHierarchy)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 3f);
                gasImage.rectTransform.localScale = Vector3.one * pulse;
            }
        }
    }
}
