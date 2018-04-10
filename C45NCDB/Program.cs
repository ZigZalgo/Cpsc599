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
			if (args.Length == 0 || args[0].Equals("help") || args[0].Equals("--help") || args[0].Equals("-h") || args.Length < 4) {
				Console.WriteLine("Args: <Binary File> <Rules File> <Output File> <Header Config File> [Header to Predict]\n");
				Console.WriteLine("<Binary File> - Pregenerated Binary file of the data to build the tree with\n");
				Console.WriteLine("<Rules File> - Rules to build the tree with before running the C4.5 algorithm.\n" +
					"\tOne rule per line. Ex. C_HOUR (==,>,<) X");
				Console.WriteLine("<Output File> - File where all of the generated rules and final statistics will be output\n");
				Console.WriteLine("<Header Config File> - File where headers can be configured. \n\tYou can either tell the algorithm to" +
					" ignore a header or mark a header as continuous. \n\tOne config per line. Ex. (ignore,cont) C_HOUR\n");
				Console.WriteLine("[Header to Predict] - One of the headers. Ex. C_HOUR. \n\tOptional, but required if you want to run the " +
					"C4.5 algorithm. \n\tIf not specified, then a binary tree will be created.\n");
			} else {
				CollisionEntry[] collisions = CollisionEntry.ReadCollisionsFromFile(args[0]);
				RulesGenerator gen = new RulesGenerator(args[1]);
				C4p5.SetSpecialHeaders(Helper.ReadSpecialHeaders(args[3]));
				C4p5 alg = new C4p5(collisions.ToList(), gen.CurrentRules);
				if (args.Length == 5) {
					C4p5.SetHeaderToPredict(args[4]);
				}
				Node root = alg.Root;
				C4p5.MaxDepth = 10;
				C4p5.MinDivSize = 100000;
				C4p5.MaxContinuousSplits = 5;
				alg.Learn();
				alg.PrintRulesSorted(args[2]);
			}
        }
    }
}
