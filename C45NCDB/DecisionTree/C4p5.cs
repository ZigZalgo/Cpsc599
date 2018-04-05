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
        private List<Node> nextIterationNodes;

        public static int Header_To_Predict { get; set; } = -1;
        public static int MaxDepth { get; set; } = 8;
        public static int MinDivSize { get; set; } = 100;
        public static bool predict = false;


        public static void SetHeaderToPredict(string header)
        {
            if (!CollisionEntry.headers.Contains(header))
                throw new Exception(header + " does not exist in collision dataset");
            else
            {
                Header_To_Predict = CollisionEntry.headers.ToList().IndexOf(header);
            }
        }

        public C4p5(List<CollisionEntry> collisions, List<Rule> Pre_Generated_Rules)
        {
            Root = new Node(collisions, 0, new List<Rule>(), null);
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

        public int depth;
        public Node Left;
        public Node Right;

        public Node(List<CollisionEntry> collisions, int depth, List<Rule> previousRules, Node parent)
        {
            usedRules = new List<Rule>(previousRules);
            currentEntries = collisions;
            Parent = parent;
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

            List<CollisionEntry> failed = new List<CollisionEntry>();
            List<CollisionEntry> passed = new List<CollisionEntry>();

            CollisionEntry.EvaluateEntries(currentEntries, failed, passed, rule);
            if (failed.Count < 1)
            {
                return;
            }
            currentEntries = null;
            Rule failedRule = rule.Invert();
            List<Rule> failedRules = new List<Rule>(usedRules)
            {
                failedRule
            };
            List<Rule> passedRules = new List<Rule>(usedRules)
            {
                rule
            };
            Left = new Node(failed, depth+1, failedRules, this);
            Right = new Node(passed, depth+1, passedRules, this);
            nextIterationNodes.Add(Left);
            nextIterationNodes.Add(Right);
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

            List<CollisionEntry> failed = new List<CollisionEntry>();
            List<CollisionEntry> passed = new List<CollisionEntry>();

            Rule usedRule;
            if (!Predict)
                usedRule = Helper.GenerateRuleFromEvenDivisions(currentEntries);
            else
                usedRule = Helper.GenerateRuleFromInformationGain(currentEntries, C4p5.Header_To_Predict);

            CollisionEntry.EvaluateEntries(currentEntries, failed, passed, usedRule);
            currentEntries = null;
            if (failed.Count < 1)
            {
                return;
            }
            Rule failedRule = usedRule.Invert();
            List<Rule> failedRules = new List<Rule>(usedRules)
            {
                failedRule
            };
            List<Rule> passedRules = new List<Rule>(usedRules)
            {
                usedRule
            };
            Left = new Node(failed, depth + 1, failedRules, this);
            Right = new Node(passed, depth + 1, passedRules, this);
            nextIterationNodes.Add(Left);
            nextIterationNodes.Add(Right);
        }

        public void PrintRules(StreamWriter writer)
        {
            if(Left == null && Right == null)
            {
                string retVal = "";
                foreach(Rule r in usedRules)
                {
                    writer.WriteLine(retVal + r.ToString());
                }
                writer.WriteLine(currentEntries.Count + " collisions");
                writer.WriteLine();
            }
            else
            {
                if (Left != null)
                    Left.PrintRules(writer);
                if (Right != null)
                    Right.PrintRules(writer);
            }
        }
    }
}
