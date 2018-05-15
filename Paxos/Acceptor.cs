using System;

namespace Paxos
{
    /// <summary>
    /// Models a Paxos acceptor
    /// </summary>
    public class Acceptor : IAgent  // IAgent has IsUp boolean property only, allows us to generically fail/resume
    {
        public bool IsUp { get; set; } = true;
        private int acceptedProposalNumber = -1;
        private int acceptedProposalValue = -1;
        private int promisedProposalNumber = -1;

        public PrepareResult Prepare(int n)
        {
            if (n > promisedProposalNumber) promisedProposalNumber = n;
            // In the simple version Lamport implies we don't need to reply at all if n < promisedProposalNumber
            // However, the algorithm actually says to return the promised number (as an optimization) in this case
            return new PrepareResult(acceptedProposalNumber, acceptedProposalValue, promisedProposalNumber);
        }

        public bool Accept(int n, int v)
        {
            // We've promised to not accept values for any n < promisedProposalNumber
            if (n < promisedProposalNumber) return false;
            acceptedProposalNumber = n;
            acceptedProposalValue = v;
            // https://stackoverflow.com/a/29929052/4166522
            if (n > promisedProposalNumber) promisedProposalNumber = n;
            return true;
        }

        public void DebugDump(int i)
        {
            string acceptedProposal = acceptedProposalNumber == -1 ? "No proposal accepted" :
                $"Accepted proposal: number {acceptedProposalNumber} value {acceptedProposalValue}";
            Console.WriteLine($"Acceptor {i} {(IsUp ? "is up" : "is down")} {acceptedProposal} Promised proposal number: " +
                $"{(promisedProposalNumber == -1 ? "None" : promisedProposalNumber.ToString())}");
        }
    }

    /// <summary>
    /// Simple class to store three named values to make it easy to return the three values from a Prepare request
    /// </summary>
    public class PrepareResult
    {
        public int acceptedProposalNumber;
        public int acceptedProposalValue;
        public int promisedProposalNumber;
        public PrepareResult(int acceptedProposalNumber, int acceptedProposalValue, int promisedProposalNumber)
        {
            this.acceptedProposalNumber = acceptedProposalNumber; this.acceptedProposalValue = acceptedProposalValue; this.promisedProposalNumber = promisedProposalNumber;
        }
    }
}
