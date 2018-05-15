using System;

namespace Paxos
{
    class Program
    {
        static void Main(string[] args)
        {
            // Comment out whichever of these you don't want to run, obviously
            new Paxos().RunContinually();
            //new PaxosSimpleTest().RunTest();
            //new PaxosSimpleTest().RunSOExample();
            //new PaxosMultipleProposers().RunContinually();
            Console.ReadLine();
        }
    }
}
