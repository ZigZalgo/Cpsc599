using C45NCDB.RuleEngine;
using C45NCDB.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB.DecisionTree
{
    public class C4p5
    {
        public Node Root;

        private List<Rule> unusedRules;
        private List<Node> currentNodes;

        private static List<int> ignoreHeaders = new List<int>();
        public static List<Node> nextIterationNodes;

        public static int Header_To_Predict { get; set; } = -1;
        public static int MaxDepth { get; set; } = 5;
        public static int MinDivSize { get; set; } = 1000;
        public static bool predict = false;


        public static void SetHeaderToPredict(string header)
        {
            if (!CollisionEntry.headers.Contains(header))
                throw new Exception(header + " does not exist in collision dataset");
            else
            {
                Header_To_Predict = CollisionEntry.headers.ToList().IndexOf(header);
            }
            predict = true;
        }

        public static void SetIgnoreHeaders(List<string> headers)
        {
            foreach (string s in headers)
            {
                if (!CollisionEntry.headers.Contains(s))
                    throw new Exception(s + " does not exist in collision dataset");
                
                ignoreHeaders.Add(CollisionEntry.headers.ToList().IndexOf(s));
                
            }
        }

        public C4p5(List<CollisionEntry> collisions, List<Rule> Pre_Generated_Rules)
        {
            Root = new Node(collisions, 0, new List<Rule>(), null, ignoreHeaders);
            currentNodes = new List<Node>();
            nextIterationNodes = new List<Node>();
            unusedRules = Pre_Generated_Rules;
            currentNodes.Add(Root);
        }

        public void Learn()
        {
            if (currentNodes.Count < 1)
            {
                Console.WriteLine("Finished Learning");
                return;
            }
            if (unusedRules.Count > 0)
            {
                foreach (Node toBeDivided in currentNodes)
                {
                    toBeDivided.Divide(unusedRules[0], nextIterationNodes);
                }
                unusedRules.RemoveAt(0);
            }
            else
            {
                foreach (Node toBeDivided in currentNodes)
                {
                    toBeDivided.Divide(nextIterationNodes, predict);
                }
            }
            currentNodes = nextIterationNodes;
            Console.WriteLine(currentNodes.Count + " Number of unlearned nodes");
            nextIterationNodes = new List<Node>();
            Learn();
        }

        public void PrintRulesSorted(string FilePath)
        {
            StreamWriter writer = new StreamWriter(FilePath);
			List<Tuple<int, string>> allRules = new List<Tuple<int, string>>();
			Root.GetRules(allRules);
			allRules.Sort((x, y) => y.Item1.CompareTo(x.Item1));
			int totalCases = 0;
			foreach (Tuple<int, string> temp in allRules) {
				totalCases += temp.Item1;
				writer.WriteLine(temp.Item2);
			}
			writer.WriteLine("\nTotal cases: " + totalCases);
            writer.Close();
        }

		public void PrintRules(string FilePath)
		{
			StreamWriter writer = new StreamWriter(FilePath);
			Root.PrintRules(writer);
			writer.Close();
		}
    }

    public class Node
    {
        public Node Parent;
        public List<Rule> usedRules;

        public List<CollisionEntry> currentEntries;

        public List<int> headersToIgnore;
        public int depth;
        public List<Node> children;

        public Node(List<CollisionEntry> collisions, int depth, List<Rule> previousRules, Node parent, List<int> ignore)
        {
            headersToIgnore = new List<int>(ignore);
            usedRules = new List<Rule>(previousRules);
            currentEntries = collisions;
            Parent = parent;
            this.depth = depth;
        }

        internal void Divide(Rule rule, List<Node> nextIterationNodes)
        {
            if (depth > C4p5.MaxDepth)
            {
                return;
            }
            if (currentEntries.Count < C4p5.MinDivSize)
            {
                return;
            }
            createChildrenFromRule(rule);

        }

        internal void Divide(List<Node> nextIterationNodes, bool Predict)
        {
            if (depth > C4p5.MaxDepth)
            {
                return;
            }
            if (currentEntries.Count < C4p5.MinDivSize)
            {
                return;
            }

            if (!Predict)
            {
                Rule usedRule = Helper.Generate_Rule_From_Even_Divisions(currentEntries, headersToIgnore);
                if (usedRule == null)
                    return;
                createChildrenFromRule(usedRule);
            }
            else
            {
                createChildrenFromEntropy();
            }

        }

        public void createChildrenFromRule(Rule rule)
        {
            List<CollisionEntry> failed = new List<CollisionEntry>();
            List<CollisionEntry> passed = new List<CollisionEntry>();

            CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);
            if (failed.Count < 1)
            {
                return;
            }
            currentEntries = null;
            List<Rule> passedRules = new List<Rule>(usedRules)
            {
                rule
            };
            children = new List<Node>();
            Node c2 = new Node(passed, depth + 1, passedRules, this, headersToIgnore);
            children.Add(c2);
            C4p5.nextIterationNodes.Add(c2);
        }

        public void createChildrenFromEntropy()
        {
            if (C4p5.Header_To_Predict == -1)
                throw new Exception("You forgot to se the Header to predict");
            //If they are all the same class we are done
            if (Helper.AllSame(currentEntries, C4p5.Header_To_Predict))
                return;

            int Header_To_Split = Helper.AttributeWithBestInfoGain(currentEntries, C4p5.Header_To_Predict, headersToIgnore);
            if (Header_To_Split == -1)
                return;
            int[] values = Helper.GetValuesOfHeaders(currentEntries)[Header_To_Split];
            headersToIgnore.Add(Header_To_Split);
            children = new List<Node>();
            foreach (int v in values)
            {
                Rule rule = new Rule(Header_To_Split, v, HValType.HeaderCompVal, Operator.Equals);
                List<CollisionEntry> passed = new List<CollisionEntry>();
                List<CollisionEntry> failed = new List<CollisionEntry>();
                CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);
                if (passed.Count < 1)
                {
                    continue;
                }
                List<Rule> passedRules = new List<Rule>(usedRules)
                    {
                        rule
                    };
                Node n = new Node(passed, depth + 1, passedRules, this, headersToIgnore);
                children.Add(n);
                C4p5.nextIterationNodes.Add(n);
            }

            if (children.Count == 0)
                children = null;
            return;
        }


        public void PrintRules(StreamWriter writer)
        {
            if (children == null)
            {
                string retVal = "";
                foreach (Rule r in usedRules)
                {
                    writer.WriteLine(retVal + r.ToString());
                }
                writer.WriteLine(currentEntries.Count + " collisions");

                if (C4p5.predict)
                {
                    int[] values = Helper.GetValuesOfHeaders(currentEntries)[C4p5.Header_To_Predict];
                    foreach (int val in values)
                    {
                        writer.WriteLine("Predicted Column with Value: " + val + " has count = " + (currentEntries.Where(x => x.vals[C4p5.Header_To_Predict] == val).Count()));
                    }
                }
                writer.WriteLine();
            }
            else
            {
                foreach (Node child in children)
                    child.PrintRules(writer);
            }
        }

		public void GetRules(List<Tuple<int, string>> list) {
			if (children == null) {
				StringBuilder sb = new StringBuilder();
				int collisionNum = 0;
				foreach (Rule r in usedRules) {
					sb.AppendLine(r.ToString());
				}
				collisionNum = currentEntries.Count;
				sb.AppendLine(collisionNum + " collisions");

				if (C4p5.predict) {
					int[] values = Helper.GetValuesOfHeaders(currentEntries)[C4p5.Header_To_Predict];
					foreach (int val in values) {
						sb.AppendLine("Predicted Column with Value: " + val + " has count = " + (currentEntries.Where(x => x.vals[C4p5.Header_To_Predict] == val).Count()));
					}
				}
				sb.AppendLine();
				list.Add(new Tuple<int, string>(collisionNum, sb.ToString()));
			} else {
				foreach (Node child in children)
					child.GetRules(list);
			}

		}
    }
}
