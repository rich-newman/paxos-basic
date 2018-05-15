using System;
using System.Linq;

namespace Paxos
{
    /// <summary>
    /// Models a Paxos proposer
    /// </summary>
    public class Proposer : IAgent  // IAgent has IsUp boolean property only, allows us to generically fail/resume
    {
        public bool IsUp { get; set; } = true;
        int sequenceNumber = 0;
        int proposerNumber;
        int numberOfProposers;
        Acceptor[] acceptors;

        public Proposer(int proposerNumber, int numberOfProposers, Acceptor[] acceptors)
        {
            this.proposerNumber = proposerNumber; this.numberOfProposers = numberOfProposers; this.acceptors = acceptors;
        }

        public bool Propose(int value, out int proposedValue)
        {
            proposedValue = value;
            if (Prepare(value, out int proposalNumber, out int valueToPropose))
            {
                proposedValue = valueToPropose;
                return Accept(proposalNumber, valueToPropose);
            }
            return false;
        }

        public bool Prepare(int value, out int proposalNumber, out int valueToPropose)
        {
            int nextProposalNumber = NextProposalNumber;
            sequenceNumber++;
            PrepareResult[] prepareResults = new PrepareResult[acceptors.Length];
            // Tell our acceptors to prepare for our proposal number if they are up
            // An optimization in the papers is to assume everything will normally be up and only send to a selected quorum (majority)
            // of acceptors, expecting them all to reply so we can proceed. Thus reducing the number of messages needed but adding to the confusion in the papers.
            for (int i = 0; i < acceptors.Length; i++)
            {
                prepareResults[i] = acceptors[i].IsUp ? acceptors[i].Prepare(nextProposalNumber) : null;
            }
            // Set our out values so we can abandon
            proposalNumber = nextProposalNumber; valueToPropose = value;

            // Check that we have a majority of acceptors responding saying they can accept the value
            int numberOfOKResponses = prepareResults.Count(p => p != null && p.promisedProposalNumber == nextProposalNumber);
            if ((double)numberOfOKResponses <= acceptors.Length / 2) { Console.WriteLine("Abandoning: insufficient acceptor responses"); return false; }

            // From the prepare results find whether we can propose our new value, have to propose an existing value, or our proposal
            // number is behind one that an acceptor has promised to use, in which case we abandon the proposal
            // TODO If our proposal number is behind a promised one (so we abandon) an optimization is to move our sequence number so our next choice is bigger than the promised one
            // Need to find where it says this
            valueToPropose = FindValueToPropose(nextProposalNumber, value, prepareResults, out bool abandon);
            if (abandon) { Console.WriteLine("Abandoning: later proposal number has been promised"); return false; }
            return true;
        }

        public bool Accept(int proposalNumber, int value)
        {
            // Tell our acceptors to accept our value if they are up
            Console.WriteLine($"Telling acceptors to accept value {value} for proposal {proposalNumber}");
            int acceptedCount = 0;
            for (int i = 0; i < acceptors.Length; i++)
            {
                if (!acceptors[i].IsUp) continue;
                bool accepted = acceptors[i].Accept(proposalNumber, value);
                if (accepted) acceptedCount++;
            }
            return ((double)acceptedCount > acceptors.Length / 2);  // true if we've chosen a value
        }

        private int FindValueToPropose(int proposalNumber, int value, PrepareResult[] prepareResults, out bool abandon)
        {
            abandon = false;
            int overallLastAcceptedProposalNumber = -1;
            int overallLastAcceptedProposalValue = -1;
            for (int i = 0; i < prepareResults.Length; i++)
            {
                // If the acceptor is down we can't do anything
                if (prepareResults[i] == null) continue;
                // If the acceptor has replied that it's already promised to only consider proposal numbers after this one
                // then we should abandon the proposal
                if (prepareResults[i].promisedProposalNumber > proposalNumber) { abandon = true; return -1; }
                // If any acceptor has already accepted something our proposal value must have the same value as the LAST accepted proposal overall
                if (prepareResults[i].acceptedProposalNumber != -1 &&
                    prepareResults[i].acceptedProposalNumber > overallLastAcceptedProposalNumber)
                {
                    overallLastAcceptedProposalNumber = prepareResults[i].acceptedProposalNumber;
                    overallLastAcceptedProposalValue = prepareResults[i].acceptedProposalValue;
                }
            }
            return overallLastAcceptedProposalValue == -1 ? value : overallLastAcceptedProposalValue;
        }

        // https://stackoverflow.com/questions/47967772/how-to-derive-a-sequence-number-in-paxos
        public int NextProposalNumber => sequenceNumber * numberOfProposers + proposerNumber;

        public void DebugDump()
        {
            Console.WriteLine($"Proposer {proposerNumber} {(IsUp ? "is up" : "is down")} Next proposal number: {NextProposalNumber}");
        }
    }
}
