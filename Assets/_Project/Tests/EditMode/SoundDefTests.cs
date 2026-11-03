using GasSafari.Audio;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    public class SoundDefTests
    {
        [Test]
        public void NextClip_NeverRepeatsTwiceInARow()
        {
            var def = ScriptableObject.CreateInstance<SoundDef>();
            def.clips = new[]
            {
                AudioClip.Create("a", 10, 1, 44100, false),
                AudioClip.Create("b", 10, 1, 44100, false),
                AudioClip.Create("c", 10, 1, 44100, false)
            };

            var last = def.NextClip();
            for (int i = 0; i < 200; i++)
            {
                var next = def.NextClip();
                Assert.AreNotSame(last, next);
                last = next;
            }
            Object.DestroyImmediate(def);
        }
    }
}
