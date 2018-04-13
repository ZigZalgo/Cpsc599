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
			if (args.Length == 0 || args[0].Equals("help") || args[0].Equals("--help") || args[0].Equals("-h") || args.Length < 4) 
			{
				Console.WriteLine("Args: <Binary File> <Rules File> <Output File> <Header Config File> [Header to Predict] ([-Parameter] [Value of Parameter])*\n");
				Console.WriteLine("<Binary File> - Pregenerated Binary file of the data to build the tree with\n");
				Console.WriteLine("<Rules File> - Rules to build the tree with before running the C4.5 algorithm.\n" +
					"\tOne rule per line. Ex. C_HOUR (==,>,<) X");
				Console.WriteLine("<Output File> - File where all of the generated rules and final statistics will be output\n");
				Console.WriteLine("<Header Config File> - File where headers can be configured. \n\tYou can either tell the algorithm to" +
					" ignore a header or mark a header as continuous. \n\tOne config per line. Ex. (ignore,cont) C_HOUR\n");
				Console.WriteLine("[Header to Predict] - One of the headers. Ex. C_HOUR. \n\tOptional, but required if you want to run the " +
					"C4.5 algorithm. \n\tIf not specified, then a binary tree will be created.\n");
				Console.WriteLine("[Parameter] - One of the parameters for the tree. Available parameters include:\n\t-MaxDepth " +
					"\t\t(Defaults to 10)\n\t-MinDivSize \t\t(Defaults to 100000)\n\t-MaxContinuousSplits \t(Defaults to 5)\n\t-LeafNodeMinimum " + 
					"\t(Defaults to 5)\nIf not specified, default values will be used.");
				Console.WriteLine("[Value of Parameter] - The integer value to assign to the parameter that was specified.");
			} 
			else 
			{
				CollisionEntry[] collisions = CollisionEntry.ReadCollisionsFromFile(args[0]);
				RulesGenerator gen = new RulesGenerator(args[1]);
				C4p5.SetSpecialHeaders(Helper.ReadSpecialHeaders(args[3]));
				C4p5 alg = new C4p5(collisions.ToList(), gen.CurrentRules);
				
				/*C4p5.MaxDepth = 10;
				C4p5.MinDivSize = 100000;
				C4p5.MaxContinuousSplits = 5;
				C4p5.LeafNodeMinimum = 5;*/
				
				if (args[3].Equals("-o"))
				{
					alg.LearnOrTree(alg.Root);
					alg.PrintRules(args[2]);
					return;
				}
				if (args.Length >= 5) {
					C4p5.SetHeaderToPredict(args[4]);
				}
				if (args.Length > 5 && args.Length < 14){
	                for (int i = 0; i < args.Length - 5; i += 2){
	                	if (args[i + 5].Equals("-MaxDepth")){
	                		C4p5.MaxDepth = int.Parse(args[i+6]);
	                	}
	                	else if (args[i + 5].Equals("-MinDivSize")){
	                		C4p5.MinDivSize = int.Parse(args[i+6]);
	                	}
	                	else if (args[i + 5].Equals("-MaxContinuousSplits")){
	                		C4p5.MaxContinuousSplits = int.Parse(args[i+6]);
	                	}
	                	else if (args[i + 5].Equals("-LeafNodeMinimum")){
	                		C4p5.LeafNodeMinimum = int.Parse(args[i+6]);
	                	}
	                	
	                }
	            }
	            Console.WriteLine("Using MaxDepth: " + C4p5.MaxDepth);
	            Console.WriteLine("Using MinDivSize: " + C4p5.MinDivSize);
	            Console.WriteLine("Using MaxContinuousSplits: " + C4p5.MaxContinuousSplits);
	            Console.WriteLine("Using LeafNodeMinimum: " + C4p5.LeafNodeMinimum);

				Node root = alg.Root;
				alg.Learn();
				alg.PrintRulesSorted(args[2]);
			}
        }
    }
}
