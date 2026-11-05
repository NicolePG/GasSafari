namespace GasSafari.Level
{
    public enum VoteResult
    {
        None,
        Pending,
        Accepted,
        Rejected
    }

    // Votación para reiniciar desde el checkpoint (docs/02 §10): se aplica si la mayoría acepta.
    // Quien la pide cuenta como sí. Con un solo votante es inmediata.
    public sealed class RestartVote
    {
        private bool[] voted;
        private int yes;
        private int no;

        public VoteResult Result { get; private set; } = VoteResult.None;
        public int Voters { get; private set; }
        public int Yes => yes;
        public int No => no;

        public VoteResult Start(int voters, int requester)
        {
            Voters = voters;
            voted = new bool[voters];
            yes = 0;
            no = 0;
            Result = VoteResult.Pending;
            return Cast(requester, true);
        }

        public VoteResult Cast(int voter, bool accept)
        {
            if (Result != VoteResult.Pending || voter < 0 || voter >= Voters || voted[voter])
                return Result;

            voted[voter] = true;
            if (accept)
                yes++;
            else
                no++;

            if (yes * 2 > Voters)
                Result = VoteResult.Accepted;
            else if (no * 2 >= Voters)
                Result = VoteResult.Rejected;
            return Result;
        }

        public bool HasVoted(int voter) => voted != null && voter >= 0 && voter < Voters && voted[voter];

        public void Cancel()
        {
            Result = VoteResult.None;
        }
    }
}
