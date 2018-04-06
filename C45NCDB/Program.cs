using C45NCDB.DecisionTree;
using C45NCDB.RuleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CollisionEntry[] collisions = CollisionEntry.ReadCollisionsFromFile(args[0]);
            RulesGenerator gen = new RulesGenerator(args[1]);
            C4p5 alg = new C4p5(collisions.ToList(), gen.CurrentRules);
            if(args.Length == 4)
            {
                if (args[3] == "-o")
                {
                    alg.LearnOrTree(alg.Root);
                    return;
                }
                else
                {
                    C4p5.SetHeaderToPredict(args[3]);
                }
            }
            Node root = alg.Root;
            C4p5.MaxDepth = 4;
            C4p5.MinDivSize = 10000;
            alg.Learn();
            alg.PrintRules(args[2]);  
        }
    }
}
