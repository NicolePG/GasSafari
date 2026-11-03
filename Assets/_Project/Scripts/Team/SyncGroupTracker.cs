using GasSafari.Characters;

namespace GasSafari.Team
{
    // Grupos de sincronía (docs/02 §5). El grupo dura 'window' desde su primer pedo y solo
    // cuentan jugadores distintos. Al crecer, los miembros anteriores suben al multiplicador nuevo.
    public class SyncGroupTracker
    {
        private struct Entry
        {
            public ISyncMember Member;
            public FartType Type;
            public float BaseImpulse;
        }

        private readonly float[] multipliers;
        private readonly float[] meterGain;
        private readonly Entry[] entries;
        private int count;
        private float groupStart;
        private float applied;

        public int GroupSize => count;

        // multipliers y meterGain indexados por cantidad de jugadores en el grupo.
        public SyncGroupTracker(float[] multipliers, float[] meterGain)
        {
            this.multipliers = multipliers;
            this.meterGain = meterGain;
            entries = new Entry[multipliers.Length - 1];
        }

        public SyncResult Register(ISyncMember member, FartType type, float baseImpulse, float now, float window)
        {
            if (count > 0 && now - groupStart > window)
                count = 0;

            if (count == 0)
            {
                groupStart = now;
                applied = multipliers[1];
                Add(member, type, baseImpulse);
                return new SyncResult(1, applied, 0f);
            }

            if (Contains(member) || count == entries.Length)
                return new SyncResult(1, multipliers[1], 0f);

            float next = multipliers[count + 1];
            for (int i = 0; i < count; i++)
                entries[i].Member.ApplySyncTopUp(entries[i].Type, entries[i].BaseImpulse, applied, next);

            Add(member, type, baseImpulse);
            applied = next;
            return new SyncResult(count, next, meterGain[count] - meterGain[count - 1]);
        }

        public void Reset()
        {
            count = 0;
        }

        private void Add(ISyncMember member, FartType type, float baseImpulse)
        {
            entries[count++] = new Entry { Member = member, Type = type, BaseImpulse = baseImpulse };
        }

        public bool Contains(ISyncMember member)
        {
            for (int i = 0; i < count; i++)
            {
                if (entries[i].Member == member)
                    return true;
            }
            return false;
        }
    }
}
