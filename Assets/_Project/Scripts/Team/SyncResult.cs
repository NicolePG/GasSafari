namespace GasSafari.Team
{
    public readonly struct SyncResult
    {
        public readonly int GroupSize;
        public readonly float Multiplier;
        public readonly float MeterGain;

        public SyncResult(int groupSize, float multiplier, float meterGain)
        {
            GroupSize = groupSize;
            Multiplier = multiplier;
            MeterGain = meterGain;
        }
    }
}
