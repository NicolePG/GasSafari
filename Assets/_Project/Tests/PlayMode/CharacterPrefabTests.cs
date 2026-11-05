using System.Collections;
using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Prefabs finales de personaje (M2-12/M2-13): modelo Humanoid, Animator con su override, sonidos y huesos.
    public class CharacterPrefabTests
    {
        private static readonly string[] Names = { "Alex", "Marty", "Melman", "Gloria", "Julien" };

        private RaftTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Destroy();
            yield return null;
        }

        private GameObject Spawn(string name, out ScriptedCommandSource input)
        {
            input = rig.AddAvatar(180f, name);
            return input.gameObject;
        }

        [UnityTest]
        public IEnumerator EachPrefab_HasHumanoidModelAnimatorAndSounds([ValueSource(nameof(Names))] string name)
        {
            var go = Spawn(name, out _);
            yield return new WaitForSeconds(0.3f);

            var data = go.GetComponent<PlayerAvatar>().Character;
            var animator = go.GetComponent<CharacterAnimator>().Animator;
            Assert.IsNotNull(animator, name);
            Assert.IsNotNull(animator.runtimeAnimatorController, name);
            Assert.AreSame(data.overrideController, animator.runtimeAnimatorController, name);
            Assert.IsTrue(animator.avatar != null && animator.avatar.isHuman, $"{name} debe ser Humanoid");
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Seated"), $"{name} arranca sentado");

            Assert.IsNotNull(data.sfxSet, name);
            Assert.Greater(data.sfxSet.puff.clips.Length, 0, name);
            Assert.Greater(data.sfxSet.mega.clips.Length, 0, name);
            Assert.AreEqual(data.height, go.GetComponent<CapsuleCollider>().height, 0.01f, name);

            Assert.IsNotNull(FindDeep(go.transform, "Hips"), name);
            var head = FindDeep(go.transform, "Head");
            Assert.IsNotNull(head, name);
            Assert.IsNotNull(head.Find("HatSocket"), $"{name} necesita HatSocket en la cabeza");
            Assert.IsEmpty(go.GetComponentsInChildren<MeshFilter>(), $"{name}: sin mallas provisionales");
        }

        [UnityTest]
        public IEnumerator Animator_FollowsFartsAndCharge([ValueSource(nameof(Names))] string name)
        {
            var go = Spawn(name, out var input);
            var animator = go.GetComponent<CharacterAnimator>().Animator;
            yield return new WaitForSeconds(0.5f);

            input.Command.FartPresses++;
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Fart_Puff") || animator.IsInTransition(0), $"{name}: toque");
            Assert.GreaterOrEqual(Services.Audio.ActiveFarts, 1, $"{name}: suena su pedo");

            yield return new WaitForSeconds(1f);
            input.Command.HoldHeld = true;
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Charge"), $"{name}: carga");
            input.Command.HoldHeld = false;
        }

        [UnityTest]
        public IEnumerator Seated_ModelSitsOnTheRaft([ValueSource(nameof(Names))] string name)
        {
            var go = Spawn(name, out _);
            yield return new WaitForSeconds(0.5f);

            var hips = FindDeep(go.transform, "Hips");
            var local = rig.Raft.transform.InverseTransformPoint(hips.position);
            // Las caderas de un animal sentado quedan cerca del borde, no flotando ni dentro del agua.
            Assert.Greater(local.y, 0f, $"{name}: caderas bajo la balsa ({local})");
            Assert.Less(local.y, go.GetComponent<PlayerAvatar>().Character.height, $"{name}: caderas muy altas ({local})");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }
    }
}
