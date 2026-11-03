using System.Collections;
using GasSafari.Abilities;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class AlexTests
    {
        private class BreakProbe : MonoBehaviour, IBreakable
        {
            public int Breaks;
            public void Break(Vector3 source) => Breaks++;
        }

        private class ScareProbe : MonoBehaviour, IScareable
        {
            public float Duration;
            public void Scare(float duration, Vector3 source) => Duration = duration;
        }

        private RaftTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
        }

        private static T Obstacle<T>(Vector3 position, HazardTag tag) where T : Component
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.layer = GameLayers.Hazard;
            go.transform.position = position;
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<HazardTags>().Configure(tag);
            return go.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator Roar_BreaksFragileAndScaresFaunaInsideCone_AndBrakes()
        {
            var alex = rig.AddAvatar(180f, "Alex");
            var roar = (RoarAbility)alex.GetComponent<AbilityRunner>().Ability;
            var front = Obstacle<BreakProbe>(new Vector3(0f, 0.5f, roar.range - 2f), HazardTag.Fragile);
            var behind = Obstacle<BreakProbe>(new Vector3(0f, 0.5f, -(roar.range - 2f)), HazardTag.Fragile);
            var far = Obstacle<BreakProbe>(new Vector3(0f, 0.5f, roar.range + 3f), HazardTag.Fragile);
            var fauna = Obstacle<ScareProbe>(new Vector3(1f, 0.5f, roar.range - 3f), HazardTag.Fauna);
            yield return new WaitForFixedUpdate();

            alex.Command.AbilityPresses++;
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, front.Breaks, "Fragile delante y en rango");
            Assert.AreEqual(0, behind.Breaks, "fuera del cono");
            Assert.AreEqual(0, far.Breaks, "fuera de alcance");
            Assert.AreEqual(roar.scareDuration, fauna.Duration, 1e-4f);
            Assert.Less(rig.Get<Rigidbody>().velocity.z, -0.5f, "frena hacia atrás");

            Object.Destroy(front.gameObject);
            Object.Destroy(behind.gameObject);
            Object.Destroy(far.gameObject);
            Object.Destroy(fauna.gameObject);
        }

        [UnityTest]
        public IEnumerator Roar_RespectsCooldown()
        {
            var alex = rig.AddAvatar(180f, "Alex");
            var runner = alex.GetComponent<AbilityRunner>();
            alex.Command.AbilityPresses++;
            yield return null;
            Assert.IsFalse(runner.Cooldown.IsReady);
            Assert.AreEqual(runner.Ability.cooldown, runner.Cooldown.Duration);
            Assert.IsFalse(runner.TryActivate());
        }

        [UnityTest]
        public IEnumerator ShowPassive_WidensWindowOnlyWhenAlexSyncs()
        {
            var alex = rig.AddAvatar(150f, "Alex");
            var other = rig.AddAvatar(210f);
            var third = rig.AddAvatar(270f);
            var sync = rig.Get<SyncDetector>();
            yield return null;

            other.Command.FartPresses++;
            third.Command.FartPresses++;
            yield return null;
            Assert.AreEqual(0f, sync.ExtraWindow, 1e-5f, "grupo sin Alex");

            yield return new WaitForSeconds(1f);
            alex.Command.FartPresses++;
            other.Command.FartPresses++;
            yield return null;
            Assert.AreEqual(rig.Balance.showWindowBonus, sync.ExtraWindow, 1e-5f);

            yield return new WaitForSeconds(rig.Balance.showBonusDuration + 0.1f);
            Assert.AreEqual(0f, sync.ExtraWindow, 1e-5f);
        }
    }
}
