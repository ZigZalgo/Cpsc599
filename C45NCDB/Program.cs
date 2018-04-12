using C45NCDB.DecisionTree;
using C45NCDB.RuleEngine;
using C45NCDB.Tools;
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
            C4p5.SetIgnoreHeaders(Helper.ReaderIgnoreHeaders(args[3]));
            C4p5 alg = new C4p5(collisions.ToList(), gen.CurrentRules);  
            if(args.Length == 5)
            {
                C4p5.SetHeaderToPredict(args[4]);
            }
            Node root = alg.Root;
            C4p5.MaxDepth = 10;
            C4p5.MinDivSize = 1000;
            C4p5.LeafNodeMinimum = 5;
            alg.Learn();
            alg.PrintRules(args[2]);  
        }
    }
}
