using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Sobre cada animal (docs/07 §3): anillo de gas del color del jugador y sus pings.
    public class AvatarOverlay : MonoBehaviour
    {
        private const float PING_SECONDS = 2f;

        [SerializeField] private Image ring;
        [SerializeField] private TMP_Text pingText;

        private PlayerAvatar avatar;
        private UIStrings strings;
        private Camera cam;
        private float pingTimer;

        public void Bind(PlayerAvatar target, UIStrings uiStrings)
        {
            avatar = target;
            strings = uiStrings;
            ring.color = target.PlayerColor;
            pingText.color = target.PlayerColor;
            pingText.gameObject.SetActive(false);
            GameEvents.OnPing += OnPing;
        }

        private void OnDestroy()
        {
            GameEvents.OnPing -= OnPing;
        }

        private void OnPing(PlayerAvatar who, PingType type)
        {
            if (who != avatar)
                return;
            pingText.SetText(strings.Get(KeyFor(type)));
            // Accesibilidad: pings de alto contraste (borde oscuro y negrita).
            bool contrast = Progression.GameSettings.HighContrastPings;
            pingText.outlineWidth = contrast ? 0.4f : 0f;
            pingText.outlineColor = Color.black;
            pingText.fontStyle = contrast ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            pingText.gameObject.SetActive(true);
            pingTimer = PING_SECONDS;
        }

        private static string KeyFor(PingType type)
        {
            switch (type)
            {
                case PingType.Now: return "ping.now";
                case PingType.Wait: return "ping.wait";
                case PingType.TurnLeft: return "ping.left";
                case PingType.TurnRight: return "ping.right";
                default: return "hud.no_gas";
            }
        }

        private void LateUpdate()
        {
            if (avatar == null)
                return;
            ring.fillAmount = avatar.Gas.Normalized;
            if (pingTimer > 0f)
            {
                pingTimer -= Time.deltaTime;
                if (pingTimer <= 0f)
                    pingText.gameObject.SetActive(false);
            }
            if (cam == null)
                cam = Camera.main;
            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }
    }
}
