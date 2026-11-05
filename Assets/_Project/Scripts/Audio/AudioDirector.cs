using UnityEngine;

namespace GasSafari.Audio
{
    // Único punto para reproducir efectos (docs/08 §2): pool de 16 fuentes 3D + 4 2D.
    public class AudioDirector : MonoBehaviour
    {
        private const int SOURCES_3D = 16;
        private const int SOURCES_2D = 4;
        private const int MAX_FARTS = 4;

        private struct Voice
        {
            public AudioSource Source;
            public SoundDef Def;
            public Transform Follow;
            public float StartTime;
            public int Handle;
        }

        [SerializeField] private SoundLibrary library;

        private Voice[] voices3D;
        private Voice[] voices2D;
        private int nextHandle = 1;

        public int ActiveFarts
        {
            get
            {
                int count = 0;
                for (int i = 0; i < voices3D.Length; i++)
                {
                    if (voices3D[i].Def != null && voices3D[i].Def.isFart && voices3D[i].Source.isPlaying)
                        count++;
                }
                return count;
            }
        }

        private void Awake()
        {
            voices3D = CreateVoices(SOURCES_3D, true);
            voices2D = CreateVoices(SOURCES_2D, false);
        }

        private Voice[] CreateVoices(int count, bool spatial)
        {
            var voices = new Voice[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject(spatial ? "Voice3D" : "Voice2D");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = spatial ? 1f : 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                voices[i].Source = source;
            }
            return voices;
        }

        public int Play(SoundId id, Vector3 position, float gainDb = 0f)
        {
            return Play(library.Get(id), position, gainDb, null, false);
        }

        // Ráfaga, carga, agua: suena hasta Stop(handle) y sigue a 'follow'.
        public int PlayLoop(SoundId id, Transform follow)
        {
            return Play(library.Get(id), follow.position, 0f, follow, true);
        }

        public void Stop(int handle)
        {
            int index = Find(voices3D, handle);
            if (index >= 0)
                Release(ref voices3D[index]);
            index = Find(voices2D, handle);
            if (index >= 0)
                Release(ref voices2D[index]);
        }

        public bool IsPlaying(int handle)
        {
            int index = Find(voices3D, handle);
            if (index >= 0)
                return voices3D[index].Source.isPlaying;
            index = Find(voices2D, handle);
            return index >= 0 && voices2D[index].Source.isPlaying;
        }

        public void SetPitch(int handle, float pitch)
        {
            int index = Find(voices3D, handle);
            if (index >= 0)
                voices3D[index].Source.pitch = pitch;
        }

        public float GetVolume(int handle)
        {
            int index = Find(voices3D, handle);
            if (index >= 0)
                return voices3D[index].Source.volume;
            index = Find(voices2D, handle);
            return index >= 0 ? voices2D[index].Source.volume : 0f;
        }

        public int Play(SoundDef def, Vector3 position, float gainDb, Transform follow, bool loop)
        {
            if (def == null || def.clips == null || def.clips.Length == 0)
                return 0;

            var pool = def.spatial ? voices3D : voices2D;
            int slot = def.isFart && ActiveFarts >= MAX_FARTS ? OldestFart(pool) : PickSlot(pool);

            ref var voice = ref pool[slot];
            var source = voice.Source;
            source.Stop();
            source.clip = def.NextClip();
            source.outputAudioMixerGroup = def.mixerGroup;
            source.volume = def.volume * Mathf.Pow(10f, gainDb / 20f);
            source.pitch = Random.Range(def.pitchMin, def.pitchMax);
            source.maxDistance = def.maxDistance;
            source.loop = loop;
            source.transform.position = position;
            source.Play();

            voice.Def = def;
            voice.Follow = follow;
            voice.StartTime = Time.unscaledTime;
            voice.Handle = nextHandle++;
            return voice.Handle;
        }

        private void LateUpdate()
        {
            for (int i = 0; i < voices3D.Length; i++)
            {
                if (voices3D[i].Follow != null)
                    voices3D[i].Source.transform.position = voices3D[i].Follow.position;
            }
        }

        // Libre; si no hay, la más antigua de menor prioridad.
        private static int PickSlot(Voice[] pool)
        {
            int best = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].Source.isPlaying)
                    return i;
                int p = pool[i].Def.priority;
                int bp = pool[best].Def.priority;
                if (p < bp || (p == bp && pool[i].StartTime < pool[best].StartTime))
                    best = i;
            }
            return best;
        }

        private static int OldestFart(Voice[] pool)
        {
            int best = -1;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Def == null || !pool[i].Def.isFart || !pool[i].Source.isPlaying)
                    continue;
                if (best < 0 || pool[i].StartTime < pool[best].StartTime)
                    best = i;
            }
            return best;
        }

        private static int Find(Voice[] pool, int handle)
        {
            if (handle == 0)
                return -1;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Handle == handle)
                    return i;
            }
            return -1;
        }

        private static void Release(ref Voice voice)
        {
            voice.Source.Stop();
            voice.Follow = null;
            voice.Handle = 0;
        }
    }
}
