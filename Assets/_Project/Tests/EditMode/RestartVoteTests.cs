using GasSafari.Level;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // docs/02 §10: el reinicio desde el checkpoint se aplica si la mayoría acepta.
    public class RestartVoteTests
    {
        [Test]
        public void TwoVoters_NeedBoth()
        {
            var vote = new RestartVote();
            Assert.AreEqual(VoteResult.Pending, vote.Start(2, 0));
            Assert.AreEqual(VoteResult.Accepted, vote.Cast(1, true));
        }

        [Test]
        public void ThreeVoters_TwoYesAccept()
        {
            var vote = new RestartVote();
            Assert.AreEqual(VoteResult.Pending, vote.Start(3, 0));
            Assert.AreEqual(VoteResult.Accepted, vote.Cast(2, true));
        }

        [Test]
        public void FourVoters_ThreeYesNeeded()
        {
            var vote = new RestartVote();
            vote.Start(4, 0);
            Assert.AreEqual(VoteResult.Pending, vote.Cast(1, true));
            Assert.AreEqual(VoteResult.Accepted, vote.Cast(2, true));
        }

        [Test]
        public void NoVotes_RejectWhenMajorityIsImpossible()
        {
            var vote = new RestartVote();
            vote.Start(2, 0);
            Assert.AreEqual(VoteResult.Rejected, vote.Cast(1, false));

            vote.Start(4, 0);
            Assert.AreEqual(VoteResult.Pending, vote.Cast(1, false));
            Assert.AreEqual(VoteResult.Rejected, vote.Cast(2, false));
        }

        [Test]
        public void SameVoterTwice_IsIgnored_AndClosedVoteStaysClosed()
        {
            var vote = new RestartVote();
            vote.Start(3, 0);
            vote.Cast(0, true);
            Assert.AreEqual(1, vote.Yes);
            Assert.IsTrue(vote.HasVoted(0));

            vote.Cast(1, false);
            vote.Cast(2, false);
            Assert.AreEqual(VoteResult.Rejected, vote.Result);
            Assert.AreEqual(VoteResult.Rejected, vote.Cast(1, true));
        }
    }
}
