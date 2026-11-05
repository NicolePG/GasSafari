using System.Collections;
using System.Collections.Generic;
using GasSafari.Abilities;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Input;
using GasSafari.Raft;
using GasSafari.Team;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M4-02: sincronía, pings, agarrarse, Súper Pedo y habilidades de los bots.
    public class BotSkillsTests
    {
        private class FaunaHazard : HazardBase { }

        private RaftTestRig rig;
        private BotConfig quiet;
        private DifficultyData previousDifficulty;
        private DifficultyData difficulty;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            // Un bot que no navega (velocidad de crucero negativa): solo dispara si la sincronía, un ping o la regla
            // del equipo detenido se lo piden.
            quiet = ScriptableObject.CreateInstance<BotConfig>();
            quiet.cruiseSpeed = -100f;
            quiet.idleLimit = 1000f;
            // Copia de la dificultad activa (ventana de sync, auto-reaparición...) con la probabilidad de sincronía al 100 %.
            previousDifficulty = Services.Session.Difficulty;
            difficulty = Object.Instantiate(previousDifficulty);
            difficulty.botSyncChance = 1f;
            Services.Session.Difficulty = difficulty;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Services.Session.Difficulty = previousDifficulty;
            foreach (var go in spawned)
                Object.Destroy(go);
            spawned.Clear();
            rig.Destroy();
            yield return null;
        }

        private BotBrain AddBot(float angle, string character = "")
        {
            string path = character == ""
                ? "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab"
                : "Assets/_Project/Prefabs/Characters/Avatar_" + character + ".prefab";
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path));
#endif
            spawned.Add(go);
            go.GetComponent<PlayerSeat>().Sit(rig.Get<RaftSeats>(), angle);
            var brain = go.AddComponent<BotBrain>();
            brain.Attach(new BotCommandSource(), quiet);
            return brain;
        }

        private static int CountFarts(Component avatar, List<float> times)
        {
            avatar.GetComponent<FartThruster>().Farted += (type, impulse, multiplier) => times.Add(Time.time);
            return 0;
        }

        // ---------------------------------------------------------------- sincronía y pings
        [UnityTest]
        public IEnumerator Bot_FiresRightAfterAHumanPuff()
        {
            var human = rig.AddAvatar(180f);
            var bot = AddBot(120f);
            var humanFarts = new List<float>();
            var botFarts = new List<float>();
            CountFarts(human, humanFarts);
            CountFarts(bot, botFarts);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, botFarts.Count, "el bot no dispara solo");

            human.Command.FartPresses++;
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(1, humanFarts.Count);
            Assert.AreEqual(1, botFarts.Count, $"el bot acompaña el pedo (bot={string.Join(",", botFarts)} humano={string.Join(",", humanFarts)})");
            Assert.LessOrEqual(botFarts[0] - humanFarts[0], 0.4f, "dentro de la ventana de sync");
        }

        [UnityTest]
        public IEnumerator Bot_IgnoresAHumanPuff_WhenTheChanceIsZero()
        {
            difficulty.botSyncChance = 0f;
            var human = rig.AddAvatar(180f);
            var bot = AddBot(120f);
            var botFarts = new List<float>();
            CountFarts(bot, botFarts);
            yield return new WaitForSeconds(0.3f);

            human.Command.FartPresses++;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, botFarts.Count);
        }

        [UnityTest]
        public IEnumerator WaitPing_SilencesTheBotForOnePointFiveSeconds()
        {
            var human = rig.AddAvatar(180f);
            var bot = AddBot(120f);
            var botFarts = new List<float>();
            CountFarts(bot, botFarts);
            yield return new WaitForSeconds(0.3f);

            human.Command.Ping = PingType.Wait;
            yield return new WaitForSeconds(0.2f);
            human.Command.Ping = PingType.None;
            human.Command.FartPresses++;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0, botFarts.Count, "callado durante la espera");

            yield return new WaitForSeconds(1f);
            human.Command.FartPresses++;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, botFarts.Count, "pasada la espera, vuelve a acompañar");
        }

        [UnityTest]
        public IEnumerator NowPing_MakesTheBotFireAtOnce()
        {
            var human = rig.AddAvatar(180f);
            var bot = AddBot(120f);
            var botFarts = new List<float>();
            CountFarts(bot, botFarts);
            yield return new WaitForSeconds(0.3f);

            human.Command.Ping = PingType.Now;
            yield return new WaitForSeconds(0.5f);
            Assert.GreaterOrEqual(botFarts.Count, 1);
        }

        // ---------------------------------------------------------------- agarrarse
        [UnityTest]
        public IEnumerator FaunaWarning_MakesTheBotGrabAndThenLetGo()
        {
            var bot = AddBot(180f);
            var states = bot.GetComponent<AvatarStateMachine>();
            yield return new WaitForSeconds(0.3f);

            var go = new GameObject("Fossa");
            spawned.Add(go);
            go.transform.position = rig.Raft.transform.position + new Vector3(0f, 0f, 10f);
            go.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
            var hazard = go.AddComponent<FaunaHazard>();
            var config = ScriptableObject.CreateInstance<HazardConfig>();
            config.warningTime = 0.6f;
            hazard.Configure(config);

            hazard.Warn();
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(AvatarState.Grabbing, states.State, "se agarra con el aviso");

            // El aviso dura 0.6 s y se suelta 0.5 s después.
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(AvatarState.Seated, states.State, "se suelta después");
        }

        // ---------------------------------------------------------------- Súper Pedo
        [UnityTest]
        public IEnumerator SuperFart_BotPressesWhenTheWindowOpens()
        {
            var human = rig.AddAvatar(180f);
            AddBot(120f);
            float power = -1f;
            GameEvents.OnSuperFart += p => power = p;
            yield return new WaitForSeconds(0.3f);

            rig.Get<TeamMeter>().Set(rig.Get<TeamMeter>().Max);
            yield return null;
            human.Command.SuperHeld = true;
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(1f, power, 0.001f, "los dos pulsaron: potencia completa");
        }

        [UnityTest]
        public IEnumerator SuperFart_BotsAloneNeverOpenTheWindow()
        {
            AddBot(120f);
            AddBot(240f);
            bool launched = false;
            GameEvents.OnSuperFart += p => launched = true;
            yield return new WaitForSeconds(0.3f);

            rig.Get<TeamMeter>().Set(rig.Get<TeamMeter>().Max);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(launched, "sin un humano que abra la ventana, los bots no se lanzan");
        }

        // ---------------------------------------------------------------- habilidades
        private static IEnumerator WaitForActivation(BotBrain bot, float seconds, System.Action<bool> done)
        {
            bool activated = false;
            bot.GetComponent<AbilityRunner>().Activated += ability => activated = true;
            float t = 0f;
            while (t < seconds && !activated)
            {
                yield return new WaitForSeconds(0.1f);
                t += 0.1f;
            }
            done(activated);
        }

        [UnityTest]
        public IEnumerator Alex_RoarsAtSomethingFragileAhead()
        {
            var bot = AddBot(180f, "Alex");
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(crate);
            crate.layer = GameLayers.Hazard;
            crate.transform.position = rig.Raft.transform.position + new Vector3(0f, 0.5f, 5f);
            crate.AddComponent<HazardTags>().Configure(HazardTag.Fragile);

            bool activated = false;
            yield return WaitForActivation(bot, 2f, a => activated = a);
            Assert.IsTrue(activated);
        }

        [UnityTest]
        public IEnumerator Alex_DoesNotRoarWithNothingAhead()
        {
            var bot = AddBot(180f, "Alex");
            bool activated = false;
            yield return WaitForActivation(bot, 1.5f, a => activated = a);
            Assert.IsFalse(activated);
        }

        [UnityTest]
        public IEnumerator Marty_GallopsBeforeARamp()
        {
            var bot = AddBot(180f, "Marty");
            var hint = new GameObject("RampHint");
            spawned.Add(hint);
            hint.transform.position = rig.Raft.transform.position + new Vector3(0f, 0f, 8f);
            hint.AddComponent<BotHint>().Configure(BotHintType.Ramp, 3f);

            bool activated = false;
            yield return WaitForActivation(bot, 2f, a => activated = a);
            Assert.IsTrue(activated);
        }

        [UnityTest]
        public IEnumerator Gloria_BellyFlopsWhenTheRaftSpins()
        {
            var bot = AddBot(180f, "Gloria");
            yield return new WaitForSeconds(0.3f);
            bool activated = false;
            bot.GetComponent<AbilityRunner>().Activated += ability => activated = true;
            for (int i = 0; i < 15 && !activated; i++)
            {
                rig.Get<Rigidbody>().angularVelocity = new Vector3(0f, 3f, 0f);
                yield return new WaitForSeconds(0.1f);
            }
            Assert.IsTrue(activated);
        }

        [UnityTest]
        public IEnumerator Julien_StartsThePartyWithTwoAnimalsAndAnEmptyMeter()
        {
            rig.AddAvatar(180f);
            var bot = AddBot(120f, "Julien");
            bool activated = false;
            yield return WaitForActivation(bot, 2f, a => activated = a);
            Assert.IsTrue(activated);
        }

        [UnityTest]
        public IEnumerator Melman_HooksASwimmerWithinReach()
        {
            var bot = AddBot(180f, "Melman");
            var human = rig.AddAvatar(120f);
            yield return new WaitForSeconds(0.3f);
            bool activated = false;
            bot.GetComponent<AbilityRunner>().Activated += ability => activated = true;

            // Despedido hacia afuera de la balsa: cae al agua, no sobre la cubierta.
            var outward = human.transform.position - rig.Raft.transform.position;
            outward.y = 0f;
            human.GetComponent<AvatarFall>().Fall(outward.normalized * 6f + Vector3.up * 2f);
            for (int i = 0; i < 20 && !activated; i++)
                yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(activated);
        }
    }
}
