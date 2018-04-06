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
        public static List<Node> nextIterationNodes;

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
            predict = true;
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
        public List<Node> children;

        public Node(List<CollisionEntry> collisions, int depth, List<Rule> previousRules, Node parent)
        {
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
                Rule usedRule = Helper.Generate_Rule_From_Even_Divisions(currentEntries);
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
            Rule failedRule = rule.Invert();
            List<Rule> failedRules = new List<Rule>(usedRules)
            {
                failedRule
            };
            List<Rule> passedRules = new List<Rule>(usedRules)
            {
                rule
            };
            children = new List<Node>();
            Node c1 = new Node(failed, depth + 1, failedRules, this);
            Node c2 = new Node(passed, depth + 1, passedRules, this);
            children.Add(c1); children.Add(c2);
            C4p5.nextIterationNodes.Add(c1);
            C4p5.nextIterationNodes.Add(c2);
        }

        public void createChildrenFromEntropy()
        {
            if (C4p5.Header_To_Predict == -1)
                throw new Exception("You forgot to se the Header to predict");
            //If they are all the same class we are done
            if (Helper.AllSame(currentEntries, C4p5.Header_To_Predict))
                return;

            int Header_To_Split = Helper.AttributeWithBestInfoGain(currentEntries, C4p5.Header_To_Predict);

            int[] values = Helper.GetValuesOfHeaders(currentEntries)[Header_To_Split];

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
                Node n = new Node(passed, depth + 1, passedRules, this);
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


    }
}
