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
		public static List<int> continuousHeaders = new List<int>();
        public static List<Node> nextIterationNodes;

        public static int numberOfLeafNodes = 0;

        public static int Header_To_Predict { get; set; } = -1;
        public static int MaxDepth { get; set; } = 5;
        public static int MinDivSize { get; set; } = 1000;
        public static int LeafNodeMinimum { get; set; } = 10;
		public static int MaxContinuousSplits { get; set; } = 5;
		public static bool predict = false;

        //more static fields for -o.
        //I think these can be non-static, but following what's already here.
        //TODO: optimize default values.
        public static int MaxBreadth { get; set; } = 2;
        public static double MinimumInformationGain { get; set; } = .2;
        public static List<CollisionEntry> allEntries;


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

        public static void SetSpecialHeaders(List<string> headers)
        {
            foreach (string s in headers)
            {
				string[] temp = s.Split();
                if (!CollisionEntry.headers.Contains(temp[1]))
                    throw new Exception(s + " does not exist in collision dataset");

				switch (temp[0]) {
					case "ignore":
						ignoreHeaders.Add(CollisionEntry.headers.ToList().IndexOf(temp[1]));
						break;
					case "cont":
						continuousHeaders.Add(CollisionEntry.headers.ToList().IndexOf(temp[1]));
						break;
				}
            }
        }

        public C4p5(List<CollisionEntry> collisions, List<Rule> Pre_Generated_Rules)
        {
            Root = new Node(collisions, 0, new List<Rule>(), null, ignoreHeaders, new Dictionary<int, int>());
            currentNodes = new List<Node>();
            nextIterationNodes = new List<Node>();
            unusedRules = Pre_Generated_Rules;
            currentNodes.Add(Root);

            //need for InfoGain calculation
            allEntries = collisions;
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
				int total = currentNodes.Count();
				int current = 0;
                foreach (Node toBeDivided in currentNodes)
                {
                    toBeDivided.Divide(predict);
					current++;
					if (current % 10 == 0) Console.WriteLine("Progress: " + current + "/" + total);
				}
            }
            currentNodes = nextIterationNodes;
            Console.WriteLine(currentNodes.Count + " Number of unlearned nodes");
            nextIterationNodes = new List<Node>();
            Learn();
        }

        public void LearnOrTree(Node node)
        {
            //generally the top of the recursion. Applys all the unused rules to the node,
            //ignoring data which doesn't match the rule list
            if (unusedRules.Count > 0)
            {
                node.ApplyAllRules(unusedRules);
            }
            //main recursive case, generate rules from coverage finds the MaxBreadth best rules
            //for each rule, recur into the node created by applying that rule to the current node's collision set.
            List<Rule> bestRules = Helper.GenerateRulesFromCoverage(node.currentEntries, node.headersToIgnore);
            foreach (Rule rule in bestRules)
            {
                Node next = node.ChildByRule(rule);
                if (next.CoverageRatioIsIncreasing())
                    LearnOrTree(next);
            }
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

        public List<int> headersToIgnore;
		public Dictionary<int, int> continuousSplits;
        public int depth;
        public List<Node> children;

        public Node(List<CollisionEntry> collisions, int depth, List<Rule> previousRules, Node parent, List<int> ignore, Dictionary<int, int> contSplits)
        {
            headersToIgnore = new List<int>(ignore);
			continuousSplits = new Dictionary<int, int>(contSplits);
			usedRules = new List<Rule>(previousRules);
            currentEntries = collisions;
            Parent = parent;
            this.depth = depth;
            headersToIgnore = new List<int>(ignore);
        }

        internal void Divide(Rule rule, List<Node> nextIterationNodes)
        {
            if (depth > C4p5.MaxDepth)
            {
                C4p5.numberOfLeafNodes++;
                return;
            }
            if (currentEntries.Count < C4p5.MinDivSize)
            {
                C4p5.numberOfLeafNodes++;
                return;
            }
            createChildrenFromRule(rule);

        }

        internal void Divide(bool Predict)
        {
            if (depth > C4p5.MaxDepth)
            {
                C4p5.numberOfLeafNodes++;
                return;
            }
            if (currentEntries.Count < C4p5.MinDivSize)
            {
                C4p5.numberOfLeafNodes++;
                return;
            }

            if (!Predict)
            {
                Rule usedRule = Helper.Generate_Rule_From_Even_Divisions(currentEntries, headersToIgnore);
                if (usedRule == null)
                {
                    C4p5.numberOfLeafNodes++;
                    return;
                }
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
                C4p5.numberOfLeafNodes++;
                return;
            }
            currentEntries = null;
            List<Rule> passedRules = new List<Rule>(usedRules)
            {
                rule
            };
            children = new List<Node>();
            Node c2 = new Node(passed, depth + 1, passedRules, this, headersToIgnore, continuousSplits);
            children.Add(c2);
            C4p5.nextIterationNodes.Add(c2);
        }

        public void createChildrenFromEntropy()
        {
            if (C4p5.Header_To_Predict == -1)
                throw new Exception("You forgot to set the Header to predict");
            //If they are all the same class we are done
            if (Helper.AllSame(currentEntries, C4p5.Header_To_Predict))
            {
                C4p5.numberOfLeafNodes++;
                return;
            }

            Tuple<int, int> Header_To_Split = Helper.AttributeWithBestInfoGain(currentEntries, C4p5.Header_To_Predict, headersToIgnore);
            if (Header_To_Split.Item1 == -1) // If the info gain is -1, then there is no good choice anymore
            {
                C4p5.numberOfLeafNodes++;
                return;
            }

			// Now determine whether the returned header is discrete or continuous
			if (C4p5.continuousHeaders.Contains(Header_To_Split.Item1)) {
				//Console.WriteLine("Continuous split");

				if (continuousSplits.ContainsKey(Header_To_Split.Item1)) { // Tally the number of times this had been used to split
					continuousSplits[Header_To_Split.Item1]++;
					if (continuousSplits[Header_To_Split.Item1] >= C4p5.MaxContinuousSplits) {
						headersToIgnore.Add(Header_To_Split.Item1);
					}
				} else {
					continuousSplits.Add(Header_To_Split.Item1, 1);
				}

				children = new List<Node>();

				Rule rule1 = new Rule(Header_To_Split.Item1, Header_To_Split.Item2, HValType.HeaderCompVal, Operator.LessThan);
				Rule rule2 = new Rule(Header_To_Split.Item1, Header_To_Split.Item2, HValType.HeaderCompVal, Operator.GreaterThanEqualTo);

				List<CollisionEntry> passed1 = new List<CollisionEntry>();
				List<CollisionEntry> failed1 = new List<CollisionEntry>();
				CollisionEntry.EvaluateEntries(currentEntries, failed1, passed1, rule1);
				if (passed1.Count > 1) {
					List<Rule> passedRules = new List<Rule>(usedRules) { rule1 };
					Node n = new Node(passed1, depth + 1, passedRules, this, headersToIgnore, continuousSplits);
					children.Add(n);
					C4p5.nextIterationNodes.Add(n);
				}

				List<CollisionEntry> passed2 = new List<CollisionEntry>();
				List<CollisionEntry> failed2 = new List<CollisionEntry>();
				CollisionEntry.EvaluateEntries(currentEntries, failed2, passed2, rule2);
				if (passed2.Count > 1) {
					List<Rule> passedRules = new List<Rule>(usedRules) { rule2 };
					Node n = new Node(passed2, depth + 1, passedRules, this, headersToIgnore, continuousSplits);
					children.Add(n);
					C4p5.nextIterationNodes.Add(n);
				}

			} else { // else perform a discrete split
				//Console.WriteLine("Discrete Split");
				int[] values = Helper.GetValuesOfHeaders(currentEntries)[Header_To_Split.Item1];
				headersToIgnore.Add(Header_To_Split.Item1);
				children = new List<Node>();
				foreach (int v in values) {
					Rule rule = new Rule(Header_To_Split.Item1, v, HValType.HeaderCompVal, Operator.Equals);
					List<CollisionEntry> passed = new List<CollisionEntry>();
					List<CollisionEntry> failed = new List<CollisionEntry>();
					CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);
					if (passed.Count < 1) {
						continue;
					}
					List<Rule> passedRules = new List<Rule>(usedRules)
						{
						rule
					};
					Node n = new Node(passed, depth + 1, passedRules, this, headersToIgnore, continuousSplits);
					children.Add(n);
					C4p5.nextIterationNodes.Add(n);
				}
			}

            if (children.Count == 0)
            {
                C4p5.numberOfLeafNodes++;
                children = null;
            }
        }

        /**
         * Creates a child rule to this node by applying the rule to its current entries
         * Open to method name suggestions.
         */
        internal Node ChildByRule(Rule rule)
        {
            List<CollisionEntry> passed = new List<CollisionEntry>();
            List<CollisionEntry> failed = new List<CollisionEntry>();
            CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);

            List<Rule> childRules = new List<Rule>(usedRules) { rule };
            headersToIgnore.Add(rule.v1);
            Node child = new Node(passed, depth + 1, childRules, this, headersToIgnore);
            if (children == null) children = new List<Node>() { child };
            else children.Add(child);
            return child;
        }

        internal void ApplyAllRules(List<Rule> rules)
        {
            List<CollisionEntry> passed;
            List<CollisionEntry> failed;
            foreach (Rule rule in rules)
            {
                headersToIgnore.Add(rule.v1);
                passed = new List<CollisionEntry>();
                failed = new List<CollisionEntry>();
                //Console.WriteLine("Applying rule " + rule.ToString());
                CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);
                currentEntries = passed;
            }
        }

        internal bool CoverageRatioIsIncreasing()
        {
            double grandparentCount;
            double grandparentRatio;
            double parentCount;
            double parentRatio;
            if (Parent == null) return true;
            if (Parent.Parent == null)
                grandparentCount = C4p5.allEntries.Count();
            else
                grandparentCount = Parent.Parent.currentEntries.Count();

            parentCount = Parent.currentEntries.Count();
            grandparentRatio = parentCount / grandparentCount;
            parentRatio = (double)currentEntries.Count() / parentCount;

            return parentRatio >= grandparentRatio;
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
					Array.Sort(values);
					double total = (double)currentEntries.Count();
					foreach (int val in values)
                    {
						int count = (currentEntries.Where(x => x.vals[C4p5.Header_To_Predict] == val).Count());
						writer.WriteLine("Predicted Column with Value: " + val + " has count = " + count + " or %" + Math.Round((double)count / total * 100.0, 3));
                    }
                }

                if(C4p5.numberOfLeafNodes >= C4p5.LeafNodeMinimum)
                {
                    PrintStatistics(writer);
                }
                writer.WriteLine();
            }
            else
            {
                foreach (Node child in children)
                    child.PrintRules(writer);
            }
        }

        public void PrintStatistics(StreamWriter writer)
        {
            Dictionary<int, Dictionary<int, int>> count = RetrieveStatistics();
            List<int> sorted = count.Keys.ToList();
            sorted.Sort();
            foreach(int key in sorted)
            {
                String retVal = CollisionEntry.headers[key]+"===> ";
                Dictionary<int, int> Values = count[key];
                List<int> sorted2 = Values.Keys.ToList();
                sorted2.Sort();
                foreach(int val in sorted2)
                {
                    int countOfValue = Values[val];
                    retVal += val + ": " + ((float)countOfValue / (float)currentEntries.Count);
                }
                writer.WriteLine(retVal);
            }

        }

        /// <summary>
        /// Calculates the current count for each value of each attribute for the current list
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Dictionary<int, int>> RetrieveStatistics()
        {
            Dictionary<int, Dictionary<int, int>> retVal = new Dictionary<int, Dictionary<int, int>>();
            foreach(CollisionEntry c in currentEntries)
            {
                for(int i = 0; i < CollisionEntry.headers.Length; i++)
                {
                    if (retVal.ContainsKey(i))
                    {
                        if (retVal[i].ContainsKey(c.vals[i]))
                        {
                            retVal[i][c.vals[i]]++;
                        }
                        else
                        {
                            retVal[i].Add(c.vals[i], 1);
                        }
                    }
                    else
                    {
                        retVal.Add(i, new Dictionary<int, int>());
                    }
                }
            }
            return retVal;
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
					Array.Sort(values);
					double total = (double)currentEntries.Count();
					foreach (int val in values) {
						int count = (currentEntries.Where(x => x.vals[C4p5.Header_To_Predict] == val).Count());
						sb.AppendLine("Predicted Column with Value: " + val + " has count = " + count + " or " + Math.Round((double)count / total * 100.0, 3) + "%");
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
