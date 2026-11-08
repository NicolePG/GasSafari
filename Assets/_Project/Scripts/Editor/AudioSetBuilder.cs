using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GasSafari.Audio;
using GasSafari.Characters;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // M2-13: SoundDef de cada sonido aprobado en M2-02, un SfxSet por personaje y la SoundLibrary con los clips
    // definitivos (reemplaza los fart_prov*). Se puede repetir.
    public static class AudioSetBuilder
    {
        private const string AUDIO = "Assets/_Project/Audio/";
        private const string SOUNDS = "Assets/_Project/Data/Sounds/";
        private const string CHARACTER_DATA = "Assets/_Project/Data/Characters/";
        // Margen para el +3 dB del pedo que completa un sync (docs/08 §2).
        private const float FART_VOLUME = 0.7f;
        private const float VOICE_VOLUME = 0.8f;

        private static readonly string[] Names = { "Alex", "Marty", "Melman", "Gloria", "Julien" };

        // SoundId → prefijo de los archivos (sin _nn).
        private static readonly (SoundId id, string prefix)[] Globals =
        {
            (SoundId.AbilityRoar, "ability_roar"), (SoundId.AbilityGallop, "ability_gallop"), (SoundId.AbilityNeck, "ability_neck"),
            (SoundId.AbilityBellyflop, "ability_bellyflop"), (SoundId.AbilityParty, "ability_party"), (SoundId.AbilityRepair, "ability_repair"),
            (SoundId.PickupBeans, "pickup_beans"), (SoundId.PickupLocoto, "pickup_locoto"), (SoundId.PickupMango, "pickup_mango"),
            (SoundId.PickupCoconut, "pickup_coconut"), (SoundId.PickupSoda, "pickup_soda"), (SoundId.PickupOnion, "pickup_onion"),
            (SoundId.PickupGoldenMango, "pickup_golden_mango"),
            (SoundId.GateOpen, "gate_open"), (SoundId.GateClose, "gate_close"), (SoundId.CrateBreak, "crate_break"),
            (SoundId.CraneWarn, "crane_warn"), (SoundId.CraneSlam, "crane_slam"), (SoundId.RaftBounce, "raft_bounce"),
            (SoundId.RaftHitHard, "raft_hit_hard"), (SoundId.WaterSplashBig, "water_splash_big"),
            (SoundId.RockWarn, "rock_warn"), (SoundId.RockImpact, "rock_impact"),
            (SoundId.LightsOut, "wind_gust"), (SoundId.LightsOn, "star_earn")
        };

        [MenuItem("Gas Safari/Setup/Crear sonidos y SfxSet (M2-13)")]
        public static void Build()
        {
            ReimportAudio();
            var entries = new List<SoundLibrary.Entry>();

            foreach (var (id, prefix) in Globals)
            {
                var def = Def(SOUNDS + id + ".asset", Clips(prefix), 1f, false);
                entries.Add(new SoundLibrary.Entry { id = id, sound = def });
            }

            SfxSet alex = null;
            foreach (var name in Names)
            {
                var set = BuildSet(name);
                if (name == "Alex")
                    alex = set;
            }

            // SoundIds genéricos de pedos (sin personaje): los de Alex, el líder.
            AddFart(entries, SoundId.FartPuff, alex.puff);
            AddFart(entries, SoundId.FartBlast, alex.blast);
            AddFart(entries, SoundId.FartCharge, alex.charge);
            AddFart(entries, SoundId.FartMega, alex.mega);
            AddFart(entries, SoundId.FartCough, alex.cough);

            var library = ProvisionalFartFeedbackBuilder.LoadOrCreate<SoundLibrary>(SOUNDS + "SoundLibrary.asset");
            library.entries = entries.ToArray();
            EditorUtility.SetDirty(library);

            foreach (var file in Directory.GetFiles(AUDIO + "SFX", "fart_prov*.wav"))
                AssetDatabase.DeleteAsset(file.Replace('\\', '/'));
            AssetDatabase.SaveAssets();
        }

        // Los FartX genéricos son los SoundDef de Alex: se copian los clips al archivo que ya existía en la biblioteca.
        private static void AddFart(List<SoundLibrary.Entry> entries, SoundId id, SoundDef source)
        {
            var def = Def(SOUNDS + id + ".asset", source.clips, FART_VOLUME, true);
            entries.Add(new SoundLibrary.Entry { id = id, sound = def });
        }

        private static SfxSet BuildSet(string name)
        {
            string key = name.ToLowerInvariant();
            string folder = SOUNDS + "Characters/" + name + "/";
            EnsureFolder(SOUNDS.TrimEnd('/'), "Characters");
            EnsureFolder(SOUNDS + "Characters", name);

            var set = ProvisionalFartFeedbackBuilder.LoadOrCreate<SfxSet>(SOUNDS + "Sfx_" + name + ".asset");
            set.puff = Def(folder + "fart_puff_" + key + ".asset", Clips("fart_puff_" + key), FART_VOLUME, true);
            set.blast = Def(folder + "fart_blast_" + key + ".asset", Clips("fart_blast_" + key), FART_VOLUME, true);
            set.charge = Def(folder + "fart_charge_" + key + ".asset", Clips("fart_charge_" + key), FART_VOLUME, true);
            set.mega = Def(folder + "fart_mega_" + key + ".asset", Clips("fart_mega_" + key), FART_VOLUME, true);
            set.cough = Def(folder + "fart_cough_" + key + ".asset", Clips("fart_cough_" + key), FART_VOLUME, true);
            set.voiceEffort = Def(folder + "voice_" + key + "_effort.asset", Clips("voice_" + key + "_effort"), VOICE_VOLUME, false);
            set.voiceFall = Def(folder + "voice_" + key + "_fall.asset", Clips("voice_" + key + "_fall"), VOICE_VOLUME, false);
            set.voiceLaugh = Def(folder + "voice_" + key + "_laugh.asset", Clips("voice_" + key + "_laugh"), VOICE_VOLUME, false);
            set.voiceCheer = Def(folder + "voice_" + key + "_cheer.asset", Clips("voice_" + key + "_cheer"), VOICE_VOLUME, false);
            EditorUtility.SetDirty(set);

            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(CHARACTER_DATA + name + ".asset");
            data.sfxSet = set;
            EditorUtility.SetDirty(data);
            return set;
        }

        private static SoundDef Def(string path, AudioClip[] clips, float volume, bool isFart)
        {
            var def = ProvisionalFartFeedbackBuilder.LoadOrCreate<SoundDef>(path);
            def.clips = clips;
            def.volume = volume;
            def.isFart = isFart;
            def.spatial = true;
            EditorUtility.SetDirty(def);
            if (clips.Length == 0)
                Debug.LogWarning("M2-13 sin clips: " + path);
            return def;
        }

        // Clips <prefix>_<nn> en cualquier subcarpeta de Audio/.
        private static AudioClip[] Clips(string prefix)
        {
            var rx = new Regex("^" + Regex.Escape(prefix) + @"_\d+$");
            return AssetDatabase.FindAssets(prefix + " t:AudioClip", new[] { AUDIO.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => rx.IsMatch(Path.GetFileNameWithoutExtension(p)))
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .ToArray();
        }

        // Aplica AudioImportRules a lo ya importado (las ráfagas y cargas pasan a ser loops).
        private static void ReimportAudio()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AUDIO.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("fart_blast") || path.Contains("fart_charge"))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
