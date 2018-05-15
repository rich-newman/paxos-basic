using System;

namespace Paxos
{
    /// <summary>
    /// Models the Paxos process in a single thread with artificial failures
    /// </summary>
    public class Paxos
    {
        public int numberOfAgents = 5;  // Number of proposer/acceptor/learners
        // To see it just work set agentFailProbability to 0.0.  
        // For some fun, set agentFailProbability to 0.8 and agentResumeProbability to 0.3.  It will eventually work.  Then increase the number of agents.
        public double agentFailProbability = 0.8;   // Probability that an up agent will go down when FailResumeAgents is called
        public double agentResumeProbability = 0.3; // Probability that a down agent will come back up when FailResumeAgents is called
        private Proposer[] proposers;
        private Acceptor[] acceptors;
        private int currentProposer = 0;

        /// <summary>
        /// Run Paxos with agents going down randomly based on the probabilities in agentFailProbability and agentResumeProbability
        /// Both proposers and acceptors can go down or come back up at the start of each cycle and after a prepare round
        /// By altering the probabilities (above) we can see the effects of instability on the system
        /// </summary>
        public void RunContinually()
        {
            Console.WriteLine($"Running algorithm with {numberOfAgents} agents, with fail probability of {agentFailProbability} and resume probability of {agentResumeProbability}.");
            Console.WriteLine("The algorithm fails and resumes agents at the start of every proposal cycle, and after a successful prepare.");
            if (agentFailProbability == 0.8 && agentResumeProbability == 0.3)
                Console.WriteLine("With the default probabilities above it can take a long time for there to even be agents available.\n" +
                    "However eventually Paxos will succeed.");
            Console.WriteLine($"Proposers always propose a value which is 10,000,000 plus their agent identifier (0-{numberOfAgents - 1}).");
            SetUpAgents();
            Random random = new Random();
            while (true)
            {
                bool chosen = ProposeWithRandomFailures(random);
                if (chosen) break;
            }
            // Could write something here to continually send chosen messages to all acceptors until they all have the value
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

        private bool ProposeWithRandomFailures(Random random)
        {
            FailResumeAgents(random);
            SelectCurrentProposer();  // Tries to select a new proposer if the current one is down
            if (!proposers[currentProposer].IsUp) return false; // All proposers may be down
            int nextValue = 10000000 + currentProposer;  // Make each proposer always propose the same value
            Console.WriteLine($"Preparing value {nextValue} using proposer {currentProposer}, next proposal number {proposers[currentProposer].NextProposalNumber}");
            if (proposers[currentProposer].Prepare(nextValue, out int proposalNumber, out int valueToPropose))
            {
                DebugDump("AFTER PREPARE");
                FailResumeAgents(random);
                if (!proposers[currentProposer].IsUp) { Console.WriteLine($"Abandoning: Proposer {currentProposer} has gone down"); return false; }
                Console.WriteLine($"Accepting value {valueToPropose} for proposal {proposalNumber} using proposer {currentProposer}");
                bool chosen = proposers[currentProposer].Accept(proposalNumber, valueToPropose);
                DebugDump("AFTER ACCEPT");
                if (chosen) { Console.WriteLine($"Value {valueToPropose} has been chosen."); return true; }
            }
            return false;
        }

        private void SelectCurrentProposer()
        {
            if (!proposers[currentProposer].IsUp)
                for (int i = 0; i < proposers.Length; i++)
                {
                    if (proposers[i].IsUp) { currentProposer = i; break; }
                }
            if (!proposers[currentProposer].IsUp) Console.WriteLine("All proposers are down: call fails");
        }

        public void FailResumeAgents(Random random)
        {
            FailResumeAgents(acceptors, random, agentFailProbability, agentResumeProbability);
            FailResumeAgents(proposers, random, agentFailProbability, agentResumeProbability);
        }

        public static void FailResumeAgents(IAgent[] agents, Random random, double agentFailProbability, double agentResumeProbability)
        {
            string comeUp = ""; string goneDown = "";  // Used solely to construct a console log message
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i].IsUp && random.NextDouble() < agentFailProbability)
                {
                    agents[i].IsUp = false;
                    goneDown += (goneDown == "" ? "" : ",") + i;
                }
                else if (!agents[i].IsUp && random.NextDouble() < agentResumeProbability)
                {
                    agents[i].IsUp = true;
                    comeUp += (comeUp == "" ? "" : ",") + i;
                }
            }
            DebugDumpFailResume(agents[0] as Acceptor != null, comeUp, goneDown);
        }

        public static void DebugDumpFailResume(bool isAcceptors, string comeUp, string goneDown)
        {
            // Not good code: a load of confusing magic that simply says, e.g., 'No Acceptors have come up', 'Acceptor 3 has come up',
            // or 'Acceptors 4,5 have come up' and then the same for 'gone down'.  Or it can be called for Proposers.
            string title = isAcceptors ? "Acceptor" : "Proposer";
            string upMessage = comeUp == "" ? $"No {title}s have come up, " : 
                $"{title}{(comeUp.Length > 1 ? "s" : "")} {comeUp} {(comeUp.Length > 1 ? "have" : "has")} come up, ";
            string downMessage = goneDown == "" ? $"no {title}s have gone down." :
                $"{title}{(goneDown.Length > 1 ? "s" : "")} {goneDown} {(goneDown.Length > 1 ? "have" : "has")} gone down.";
            Console.WriteLine($"{upMessage}{downMessage}");
        }

        public void DebugDump(string identifier = "")
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine($"CURRENT SYSTEM STATE {identifier}");
            //for (int i = 0; i < proposers.Length; i++) proposers[i].DebugDump();
            Console.WriteLine($"Current proposer is {currentProposer}, is {(proposers[currentProposer].IsUp ? "up" : "down")}, next proposal number {proposers[currentProposer].NextProposalNumber}");
            for (int i = 0; i < acceptors.Length; i++) acceptors[i].DebugDump(i);
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
