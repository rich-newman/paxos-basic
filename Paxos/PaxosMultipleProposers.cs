using System;
using System.Linq;

namespace Paxos
{
    class PaxosMultipleProposers
    {
        public int numberOfAcceptors = 5;
        public int numberOfProposers = 2;
        public double agentFailProbability = 0.5;   // Probability that an up agent will go down when FailResumeAgents is called
        public double agentResumeProbability = 0.3; // Probability that a down agent will come back up when FailResumeAgents is called
        private Proposer[] proposers;
        private Acceptor[] acceptors;

        /// <summary>
        /// Run Paxos with agents going down randomly based on the probabilities in agentFailProbability and agentResumeProbability
        /// Both proposers and acceptors can go down or come back up at the start of each cycle and after an accept round
        /// By altering the probabilities (above) we can see the effects of instability on the system
        /// </summary>
        public void RunContinually()
        {
            int nextValue = 10000000;
            SetUpAgents();
            Random random = new Random();
            while (true)
            {
                bool chosen = ProposeWithRandomFailures(nextValue++, random);
                if (chosen) break;
            }
            // Could write something here to continually send chosen messages to all acceptors until they all have the value
        }

        private void SetUpAgents()
        {
            acceptors = new Acceptor[numberOfAcceptors];
            proposers = new Proposer[numberOfProposers];
            for (int i = 0; i < numberOfAcceptors; i++)
            {
                acceptors[i] = new Acceptor();
            }
            for (int i = 0; i < numberOfProposers; i++)
            {
                proposers[i] = new Proposer(i, numberOfProposers, acceptors);
            }
        }

        private bool ProposeWithRandomFailures(int nextValue, Random random)
        {
            FailResumeAgents(random);
            if(!proposers.Any(p => p.IsUp))
            {
                Console.WriteLine("All proposers are down: call fails");
                return false;
            }

            // We avoid interleaving by just not allowing it: we do prepare/accept for each proposer separately
            // We can only do this because this is a fake messaging system of course
            for (int proposer = 0; proposer < numberOfProposers; proposer++)
            {
                if (!proposers[proposer].IsUp) continue;
                if (proposers[proposer].Prepare(nextValue, out int proposalNumber, out int valueToPropose))
                {
                    DebugDump($"AFTER PREPARE FOR PROPOSER {proposer}");
                    FailResumeAgents(random);
                    if (!proposers[proposer].IsUp) { Console.WriteLine($"Proposer {proposer} has gone down: call fails"); return false; }
                    Console.WriteLine($"Accepting value {valueToPropose} for proposal {proposalNumber} using proposer {proposer}");
                    bool chosen = proposers[proposer].Accept(proposalNumber, valueToPropose);
                    DebugDump("AFTER ACCEPT");
                    if (chosen) { Console.WriteLine($"Value {valueToPropose} has been chosen."); return true; }
                }
            }
            return false;
        }

        public void FailResumeAgents(Random random)
        {
            Paxos.FailResumeAgents(acceptors, random, agentFailProbability, agentResumeProbability);
            Paxos.FailResumeAgents(proposers, random, agentFailProbability, agentResumeProbability);
        }


        public void DebugDump(string identifier = "")
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine($"CURRENT SYSTEM STATE {identifier}");
            //for (int i = 0; i < proposers.Length; i++) proposers[i].DebugDump(); // Maybe just show the current proposer and next number
            //Console.WriteLine($"Current proposer is {currentProposer}, is {(proposers[currentProposer].IsUp ? "up" : "down")}, next proposal number {proposers[currentProposer].NextProposalNumber}");
            for (int i = 0; i < proposers.Length; i++) proposers[i].DebugDump();
            for (int i = 0; i < acceptors.Length; i++) acceptors[i].DebugDump(i);
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
