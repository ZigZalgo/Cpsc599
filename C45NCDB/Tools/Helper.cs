using C45NCDB.RuleEngine;
using C45NCDB.DecisionTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB.Tools
{
    public static class Helper
    {

        public static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static Rule GenerateRuleFromEvenDivisions(List<CollisionEntry> collection)
        {
            double information_gain_best = double.MaxValue;
            int header_corresponding = -1;
            int value_corresponding = -1;

            for(int i = 0; i < CollisionEntry.headers.Length; i++)
            {
                Dictionary<int, int> count = new Dictionary<int, int>();
                foreach(CollisionEntry c in collection)
                {
                    if (count.ContainsKey(c.vals[i]))
                        count[c.vals[i]]++;
                    else
                    {
                        count.Add(c.vals[i], 0);
                    }
                }
                int maxKey = count.FirstOrDefault(x => x.Value == count.Values.Max()).Key;
                double maxVal = (double)count[maxKey] / (double)collection.Count;
                double closeToHalf = Math.Abs(maxVal - 0.5);
                if (closeToHalf < information_gain_best)
                {
                    information_gain_best = closeToHalf;
                    header_corresponding = i;
                    value_corresponding = maxKey;
                }
            }

            Rule r = new Rule(header_corresponding, value_corresponding, HValType.HeaderCompVal, Operator.Equals);
            return r;
        }

        internal static Rule GenerateRuleFromInformationGain(List<CollisionEntry> currentEntries, int header_To_Predict)
        {
            if (header_To_Predict == -1)
                throw new Exception("You forgot to se the Header to predict");
            return GenerateRuleFromEvenDivisions(currentEntries);
        }

        public static List<Rule> GenerateRulesFromCoverage(List<CollisionEntry> currentNodeCollection, List<CollisionEntry> total)
        {
            //double information_gain_best = double.MinValue;
            //RuleInfo[] informationGainBest = new RuleInfo[C4p5.MaxBreadth];
            List<RuleInfo> informationGainBest = new List<RuleInfo>();

            for (int i = 0; i < CollisionEntry.headers.Length; i++)
            { 
                //Tally values into the dictionary
                Dictionary<int, int> count = new Dictionary<int, int>();
                foreach (CollisionEntry c in currentNodeCollection)
                {
                    if (count.ContainsKey(c.vals[i]))
                        count[c.vals[i]]++;
                    else
                    {
                        count.Add(c.vals[i], 0);
                    }
                }
                //get P(maxKey)
                int maxKey = count.FirstOrDefault(x => x.Value == count.Values.Max()).Key;
                double maxVal = (double)count[maxKey] / (double)currentNodeCollection.Count;

                //tally total occurances of maxkey in the whole db
                int maxKeyTotalOccurances = 0;
                foreach (CollisionEntry c in total)
                {
                    if (c.vals[i] == count[maxKey]) maxKeyTotalOccurances++;
                }

                //calculate information gain.
                /*
                 * Gonna use as close to what exists in the slides as possible
                 * IG = H(A) - H(A|B)
                 * where 
                 *  A = the condition under consideration
                 *  B = All previous rules
                 *  H(A) = the number of entries that match A out of all entries
                 *  H(A|B) = the number of entries that match B and then A
                 */
                double infoGain = (maxKeyTotalOccurances / total.Count) - maxVal;
                if (informationGainBest.Count < C4p5.MaxBreadth)
                {
                    //add it to the list if not enough good rules have yet been found
                    informationGainBest.Add(new RuleInfo(infoGain, i, maxKey));
                }
                else if (infoGain > informationGainBest[C4p5.MaxBreadth].informationGain)
                {
                    //pop off the least best and replace it with this new better value
                    //Then sort all the most best to ensure list integrity
                    informationGainBest[C4p5.MaxBreadth - 1] = new RuleInfo(infoGain, i, maxKey);
                    informationGainBest.Sort();
                }
            }

            //Rule r = new Rule(header_corresponding, value_corresponding, HValType.HeaderCompVal, Operator.Equals);
            List<Rule> rules = new List<Rule>();
            foreach (RuleInfo ruleInfo in informationGainBest)
            {
                rules.Add(ruleInfo.MakeRule());
            }
            return rules;
        }
    }

    public class RuleInfo : IComparable<RuleInfo>
    {
        public double informationGain;
        public int headerCorresponding;
        public int valueCorresponding;

        public RuleInfo(double informationGain, int headerCorresponding, int valueCorresponding)
        {
            this.informationGain = informationGain;
            this.headerCorresponding = headerCorresponding;
            this.valueCorresponding = valueCorresponding;
        }

        public Rule MakeRule()
        {
            return new Rule(headerCorresponding, valueCorresponding, HValType.HeaderCompVal, Operator.Equals);
        }

        public int CompareTo(RuleInfo other)
        {
            return informationGain.CompareTo(other.informationGain);
        }
    }
}
