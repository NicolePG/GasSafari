using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace GasSafari.Tests
{
    public class GasSafariControlsTests
    {
        private const string ASSET_PATH = "Assets/_Project/Settings/GasSafariControls.inputactions";

        private static readonly string[] PlayerActions =
        {
            "Move", "Aim", "Fart", "Hold", "Ability", "Grab",
            "SuperLeft", "SuperRight", "Ping", "Jump", "Pause"
        };

        private InputActionAsset asset;

        [SetUp]
        public void SetUp()
        {
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ASSET_PATH);
        }

        [Test]
        public void Asset_HasPlayerAndUiMaps()
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.FindActionMap("Player"));
            Assert.IsNotNull(asset.FindActionMap("UI"));
        }

        [Test]
        public void PlayerActions_HaveKeyboardAndGamepadBindings([ValueSource(nameof(PlayerActions))] string actionName)
        {
            var action = asset.FindActionMap("Player").FindAction(actionName);
            Assert.IsNotNull(action, actionName);
            Assert.IsTrue(HasBindingInGroup(action, "Keyboard"), actionName + " sin teclado");
            Assert.IsTrue(HasBindingInGroup(action, "Gamepad"), actionName + " sin mando");
        }

        [Test]
        public void KeyR_TriggersBothSuperActions()
        {
            var map = asset.FindActionMap("Player");
            Assert.IsTrue(HasPath(map.FindAction("SuperLeft"), "<Keyboard>/r"));
            Assert.IsTrue(HasPath(map.FindAction("SuperRight"), "<Keyboard>/r"));
        }

        private static bool HasBindingInGroup(InputAction action, string group)
        {
            foreach (var binding in action.bindings)
            {
                if (!binding.isComposite && binding.groups != null && binding.groups.Contains(group))
                    return true;
            }
            return false;
        }

        private static bool HasPath(InputAction action, string path)
        {
            foreach (var binding in action.bindings)
            {
                if (binding.path == path)
                    return true;
            }
            return false;
        }
    }
}
