using System;

namespace Paxos
{
    class PaxosSimpleTest
    {
        public int numberOfAgents = 5;  // Number of proposer/acceptor/learners
        private Proposer[] proposers;
        private Acceptor[] acceptors;
        private int currentProposer = 0;

        /// <summary>
        /// Alternative entry point
        /// Here the code controls which agents are up and down whenever we call propose
        /// We can write our own experiments to see how the algorithm works
        /// </summary>
        /// <remarks>
        /// In the example below only two acceptors out of five are up for the first proposal
        /// Then three are up for the second proposal from a different proposer
        /// Now we can see what happens when everything comes back up
        /// Actually much more interesting if we let acceptors go down after they've prepared
        /// </remarks>
        public void RunTest()
        {
            int nextValue = 10000000;
            SetUpAgents();
            // Only acceptors 3 and 4 are up (out of 5 acceptors), current proposer 0 is up
            // Because we don't have a majority of acceptors available our proposal will be abandoned
            acceptors[0].IsUp = acceptors[1].IsUp = acceptors[2].IsUp = false;
            acceptors[3].IsUp = acceptors[4].IsUp = true;
            Propose(nextValue++);
            DebugDump();
            // Acceptors 0, 1 and 2 are up, proposer 0 has gone down (1 is up)
            acceptors[0].IsUp = acceptors[1].IsUp = acceptors[2].IsUp = true;
            acceptors[3].IsUp = acceptors[4].IsUp = false;
            proposers[0].IsUp = false;
            Propose(nextValue++);
            DebugDump();
            // We expect acceptors 0, 1, 2 to have accepted values
            // All agents come back up.  What happens?
            acceptors[0].IsUp = acceptors[1].IsUp = acceptors[2].IsUp = true;
            acceptors[3].IsUp = acceptors[4].IsUp = true;
            proposers[0].IsUp = true;
            Propose(nextValue++);
            DebugDump();
        }

        // Fixed version of problem on link below
        //https://stackoverflow.com/questions/29880949/contradiction-in-lamports-paxos-made-simple-paper?rq=1
        public void RunSOExample()
        {
            numberOfAgents = 3;
            int nextValue = 10000000;
            SetUpAgents();
            acceptors[0].IsUp = true;
            acceptors[1].IsUp = true;
            acceptors[2].IsUp = false;
            proposers[currentProposer].Prepare(nextValue, out int proposalNumber0, out int valueToPropose0);  // Steps 1,2
            //Propose(nextValue++);
            DebugDump();

            nextValue++;
            proposers[0].IsUp = false; // Step 3
            currentProposer = 1;
            proposers[currentProposer].Prepare(nextValue, out int proposalNumber1, out int valueToPropose1);  // Steps 4,5
            DebugDump();

            acceptors[0].IsUp = false;
            acceptors[1].IsUp = true;
            acceptors[2].IsUp = true;
            bool chosen = proposers[currentProposer].Accept(proposalNumber1, valueToPropose1); // Steps 6,7.  chosen is true.
            DebugDump();
            if (chosen) Console.WriteLine($"Value {valueToPropose1} has been chosen.");

            proposers[0].IsUp = true;
            proposers[1].IsUp = false;
            currentProposer = 0;
            chosen = proposers[currentProposer].Accept(proposalNumber0, valueToPropose0); // Step 8.
            DebugDump();
        }

        private bool Propose(int nextValue)
        {
            EnsureCurrentProposerIsUp();
            if (proposers[currentProposer].IsUp) // All proposers may be down
            {
                Console.WriteLine($"Proposing value {nextValue} using proposer {currentProposer}");
                bool chosen = proposers[currentProposer].Propose(nextValue, out int proposedValue);
                if (chosen) { Console.WriteLine($"Value {proposedValue} has been chosen."); return true; }
            }
            return false;
        }

        private void SetUpAgents()
        {
            acceptors = new Acceptor[numberOfAgents];
            proposers = new Proposer[numberOfAgents];
            for (int i = 0; i < numberOfAgents; i++)
            {
                acceptors[i] = new Acceptor();
                proposers[i] = new Proposer(i, numberOfAgents, acceptors);
            }
        }

        private void EnsureCurrentProposerIsUp()
        {
            if (!proposers[currentProposer].IsUp)
                for (int i = 0; i < proposers.Length; i++) { if (proposers[i].IsUp) { currentProposer = i; break; } }
            if (!proposers[currentProposer].IsUp) Console.WriteLine("All proposers are down: call fails");
        }

        public void DebugDump(string identifier = "")
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine($"CURRENT SYSTEM STATE {identifier}");
            //for (int i = 0; i < proposers.Length; i++) proposers[i].DebugDump(); // Maybe just show the current proposer and next number
            Console.WriteLine($"Current proposer is {currentProposer}, is {(proposers[currentProposer].IsUp ? "up" : "down")}, next proposal number {proposers[currentProposer].NextProposalNumber}");
            for (int i = 0; i < acceptors.Length; i++) acceptors[i].DebugDump(i);
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
