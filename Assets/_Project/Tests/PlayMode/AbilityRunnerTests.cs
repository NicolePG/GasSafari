using System.Collections;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class AbilityRunnerTests
    {
        private class CountingAbility : AbilityBase
        {
            public int Activations;
            public float CastTime;
            public AbilityContext LastContext;

            public override void Activate(AbilityContext context)
            {
                Activations++;
                LastContext = context;
                if (CastTime > 0f)
                    context.Runner.Cast(CastTime);
            }
        }

        private class CountingPassive : PassiveBase
        {
            public int Boards;
            public int Leaves;
            protected override void OnBoard(RaftSeats raft) => Boards++;
            protected override void OnLeave(RaftSeats raft) => Leaves++;
        }

        private RaftTestRig rig;
        private ScriptedCommandSource input;
        private AbilityRunner runner;
        private CountingAbility ability;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            input = rig.AddAvatar(180f);
            runner = input.GetComponent<AbilityRunner>();
            ability = ScriptableObject.CreateInstance<CountingAbility>();
            ability.id = "test";
            ability.cooldown = 1f;
            runner.Ability = ability;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
            Object.Destroy(ability);
        }

        private IEnumerator Press()
        {
            input.Command.AbilityPresses++;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Press_ActivatesAndRespectsCooldown()
        {
            yield return Press();
            Assert.AreEqual(1, ability.Activations);
            Assert.IsNotNull(ability.LastContext.Raft);
            Assert.IsNotNull(ability.LastContext.Sync);
            Assert.IsNotNull(ability.LastContext.Meter);

            yield return Press();
            Assert.AreEqual(1, ability.Activations, "en recarga");

            yield return new WaitForSeconds(ability.cooldown);
            yield return Press();
            Assert.AreEqual(2, ability.Activations);
        }

        [UnityTest]
        public IEnumerator Cast_BlocksFartsThenReturnsToSeated()
        {
            ability.CastTime = 0.3f;
            yield return Press();
            var states = input.GetComponent<AvatarStateMachine>();
            Assert.AreEqual(AvatarState.AbilityCast, states.State);

            input.Command.FartPresses++;
            yield return null;
            var gas = input.GetComponent<GasTank>();
            Assert.AreEqual(gas.Capacity, gas.Gas, 0.01f, "no dispara durante la habilidad");

            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(AvatarState.Seated, states.State);
        }

        [UnityTest]
        public IEnumerator Overboard_CannotActivate()
        {
            input.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -3f));
            yield return Press();
            Assert.AreEqual(0, ability.Activations);
        }

        [UnityTest]
        public IEnumerator Passive_GetsBoardAndLeaveEvents()
        {
            var passive = input.gameObject.AddComponent<CountingPassive>();
            Assert.AreEqual(1, passive.Boards, "ya estaba sentado");

            input.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -3f));
            Assert.AreEqual(1, passive.Leaves);

            input.GetComponent<AvatarFall>().ReturnToRaft();
            Assert.AreEqual(2, passive.Boards);
            yield return null;
        }
    }
}
