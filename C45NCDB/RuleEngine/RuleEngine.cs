using C45NCDB.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB.RuleEngine
{
    public enum HValType
    {
        HeaderCompVal,
        HeaderCompHeader
    }

    public enum Operator
    {
        LessThan,
        GreaterThan,
        Equals,
        NotEquals,
        LessThanEqualTo,
        GreaterThanEqualTo
    }

    public class Rule
    {
        private static Dictionary<string, Operator> opVals = new Dictionary<string, Operator>(){
                { ">", Operator.GreaterThan},
                { "<", Operator.LessThan},
                { "==", Operator.Equals},
                { "!=", Operator.NotEquals },
                { "<=", Operator.LessThanEqualTo },
                { ">=", Operator.GreaterThanEqualTo },
                { "=<", Operator.LessThanEqualTo },
                { "=>", Operator.GreaterThanEqualTo }
            };
        public HValType type;
        public int v1;
        public Operator op;
        public int v2;
        string[] inputted_Rule;

        public static string WriteRule(string[] rule)
        {
            string line = "(";
            foreach (string item in rule)
            {
                line += item + " ";
            }
            line += ")";
            return line;
        }

        public Rule(int valOne, int valTwo, HValType t, Operator o)
        {
            type = t;
            v1 = valOne;
            op = o;
            v2 = valTwo;
        }

        public Rule(string[] rule)
        {
            inputted_Rule = rule;
            if (inputted_Rule.Length != 3)
                throw new Exception("Illegal number of arguments in rule " + WriteRule(inputted_Rule));
            if (!opVals.ContainsKey(rule[1]))
                throw new Exception("[" + inputted_Rule[1] + "] from " + WriteRule(inputted_Rule) + " is an illegal operator");
            if (!CollisionEntry.headers.Contains(inputted_Rule[0]))
                throw new Exception("[" + inputted_Rule[0] + "] from " + WriteRule(inputted_Rule) + "not found in collisions headers");

            v1 = CollisionEntry.headers.ToList().IndexOf(inputted_Rule[0]);
            //If we're comparing headers
            if ((int.TryParse(inputted_Rule[2], out int vTwo)))
            {
                type = HValType.HeaderCompVal;
                op = opVals[rule[1]];
                v2 = vTwo;
                
            }
            //If we're comparing vals
            else
            {
                if (CollisionEntry.headers.ToList().Contains(inputted_Rule[2]))
                {
                    type = HValType.HeaderCompHeader;
                    op = opVals[rule[1]];
                    v2 = CollisionEntry.headers.ToList().IndexOf(inputted_Rule[2]);
                }
                else
                    throw new Exception("[" + inputted_Rule[2] + "] from " + WriteRule(inputted_Rule) + "not found in collisions headers");
            }

        }

        public override string ToString()
        {
            string ret = CollisionEntry.headers[v1] + " ";
            if (op == Operator.Equals)
                ret += "== ";
            if (op == Operator.NotEquals)
                ret += "!= ";
            if (op == Operator.LessThan)
                ret += "< ";
            if (op == Operator.GreaterThan)
                ret += "> ";
            if (op == Operator.GreaterThanEqualTo)
                ret += ">= ";
            if (op == Operator.LessThanEqualTo)
                ret += "<= ";

            if (type == HValType.HeaderCompHeader)
                ret += CollisionEntry.headers[v1];
            else
                ret += v2;
            return ret;

        }

        public Rule Invert()
        {
            switch (op)
            {
                case Operator.Equals:
                    return new Rule(v1, v2, type, Operator.NotEquals);
                case Operator.NotEquals:
                    return new Rule(v1, v2, type, Operator.Equals);
                case Operator.GreaterThan:
                    return new Rule(v1, v2, type, Operator.LessThanEqualTo);
                case Operator.LessThan:
                    return new Rule(v1, v2, type, Operator.GreaterThanEqualTo);
                case Operator.LessThanEqualTo:
                    return new Rule(v1, v2, type, Operator.GreaterThan);
                case Operator.GreaterThanEqualTo:
                    return new Rule(v1, v2, type, Operator.LessThan);
                default:
                    //Should not happen
                    return null;
            }
        }

        #region Evaluation functions
        internal bool Evaluate(CollisionEntry entry)
        {
            if (type == HValType.HeaderCompVal)
            {
                return ValEvaluate(entry);
            }
            else
            {
                return HeaderEvaluate(entry);
            }
        }

        private bool ValEvaluate(CollisionEntry entry)
        {
            if (op == Operator.LessThan)
                return entry.vals[v1] < v2;
            if (op == Operator.GreaterThan)
                return entry.vals[v1] > v2;
            if (op == Operator.Equals)
                return entry.vals[v1] == v2;
            if (op == Operator.NotEquals)
                return entry.vals[v1] != v2;
            throw new Exception("this should never happen RuleEngine ValEvaluateFunction");
        }

        private bool HeaderEvaluate(CollisionEntry entry)
        {
            if (op == Operator.LessThan)
                return entry.vals[v1] < entry.vals[v2];
            if (op == Operator.GreaterThan)
                return entry.vals[v1] > entry.vals[v2];
            if (op == Operator.Equals)
                return entry.vals[v1] == entry.vals[v2];
            if (op == Operator.NotEquals)
                return entry.vals[v1] != entry.vals[v2];
            if (op == Operator.GreaterThanEqualTo)
                return entry.vals[v1] >= entry.vals[v2];
            if (op == Operator.LessThanEqualTo)
                return entry.vals[v1] <= entry.vals[v2];
            throw new Exception("this should never happen RuleEngine HeaderEvaluateFunction");
        }
        #endregion
    }

    public class RulesGenerator
    {
        public List<Rule> CurrentRules;

        public RulesGenerator(string RulesFilePath)
        {
            CurrentRules = new List<Rule>();
            StreamReader reader = new StreamReader(RulesFilePath);
            string line = "";
            HashSet<Rule> rules = new HashSet<Rule>(new RulesComparer());
            int lineNum = 0;
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    Rule r = new Rule(line.Trim().Split());
                    if (!rules.Add(r))
                        continue;
                    CurrentRules.Add(r);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error on line " + lineNum);
                    throw e;
                }
                lineNum++;
            }
            
        }
    }
}
