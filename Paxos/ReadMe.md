# Paxos Implementation

This is yet another program written to help understand and demonstrate the basic Paxos algorithm.  If loaded in Visual Studio on Windows it should just run if you hit F5.  There's enough logging that you should be able to work out what's happened from the console output.

The code cheats a lot: it's single-threaded and there's no async messaging layer as a result.  We just simulate this with appropriate sync calls.  

It models the selection of one numbered command/slot/decree in the Paxos sequence only.

In the basic example, agents/nodes go down and come back up randomly based on probabilities set in the code.  This only happens at certain points in the cycle, namely at the start of every proposal cycle and after a prepare message has been sent.

 ## Original Sources

http://lamport.azurewebsites.net/pubs/lamport-paxos.pdf  
http://lamport.azurewebsites.net/pubs/paxos-simple.pdf

## Comments  

 * The code assumes messages are always delivered instantaneously, but agents (computers) can go down
 * All agents are numbered 0-n.  For the first cut every agent is both a proposer and an acceptor.
 * Commands/values are just 10,000,000 plus the proposer ID number (0-number of proposers).  So proposer 0 always proposes value 10,000,000, proposer 1 always proposes 10,000,001, and so on.
 * We continually propose.  If a proposer goes down the next agent that is up becomes the proposer automatically: we don't model the election.
 * For the basic algorithm we assume one single proposer is the main proposer at any given time, and is the only proposer that can propose.
 * Once a value is chosen we terminate: in the real algorithm you continually propose the value until all acceptors are up and have it, or use some other mechanism to propagate.
 * A lot of the code here is so we can log what's going on accurately: the core of the classes is quite small.