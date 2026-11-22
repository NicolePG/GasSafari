using System.Collections;
using GasSafari.Progression;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-02: 4 bots terminan en modo Espejo un nivel de cada mundo con el mismo límite que en Historia (Plata x1.3).
    // El Espejo invierte imagen y entrada, no el mundo: estas corridas vigilan que el modo (cámara a textura, textos
    // volteados, récords propios) no rompa el nivel ni a los bots.
    public class MirrorBotTests
    {
        [UnityTest, Timeout(600000)]
        public IEnumerator W1_2_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N02_W1-2_Alcantarillas", "W1-2-espejo", 310f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W2_2_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N07_W2-2_Tormenta", "W2-2-espejo", 295f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W3_1_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N11_W3-1_PlayaLlegada", "W3-1-espejo", 245f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W4_1_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N16_W4-1_CascadasEscalonadas", "W4-1-espejo", 240f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W5_4_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N24_W5-4_Represa", "W5-4-espejo", 235f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W6_1_Mirror_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return W1LevelBotTests.RunLevel("N26_W6-1_FiestaEspejo", "W6-1-espejo", 290f * 1.3f, null, ProgressionRules.MODE_MIRROR);
        }
    }
}
