using C45NCDB.RuleEngine;
using C45NCDB.DecisionTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;


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

        /// <summary>
        /// Generates a rule that divides the dataset in a way that causes for the most equal split
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static Rule Generate_Rule_From_Even_Divisions(List<CollisionEntry> collection)
        {
            double information_gain_best = double.MaxValue;
            int header_corresponding = -1;
            int value_corresponding = -1;

            for (int i = 0; i < CollisionEntry.headers.Length; i++)
            {
                Dictionary<int, int> count = new Dictionary<int, int>();
                foreach (CollisionEntry c in collection)
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
     
        
        /// <summary>
        /// Finds the attribute with the highest information gain
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="header_To_Predict"></param>
        /// <returns></returns>
        public static int AttributeWithBestInfoGain(List<CollisionEntry> entries, int header_To_Predict)
        {
            Dictionary<int, int[]> valuesOfHeaders = GetValuesOfHeaders(entries);

            //Entropy of whole
            double IS = Calculate_Entropy(entries, header_To_Predict);

            //Current best Values
            double InfoGain = double.MinValue;
            int attributeWithBest = -1;

            //For each attribute i
            for (int i = 0; i < CollisionEntry.headers.Length; i++)
            {
                if (i == header_To_Predict)
                    continue;

                double infoGainForAttributeI = double.MinValue;

                //For each value of attribute i
                for (int ValueIndex = 0; ValueIndex < valuesOfHeaders[i].Length; ValueIndex++)
                {
                    int Value = valuesOfHeaders[i][ValueIndex];
                    List<CollisionEntry> subset = entries.Where(x => x.vals[i] == Value).ToList();
                    double entropyOfDivisions = Calculate_Entropy(subset, header_To_Predict);
                    double Probability = (double)subset.Count / (double)entries.Count;
                    infoGainForAttributeI += Probability * entropyOfDivisions;
                }
                double current = IS - infoGainForAttributeI;
                if (current > InfoGain)
                {
                    InfoGain = current;
                    attributeWithBest = i;
                }
            }
            return attributeWithBest;
        }
         
        /// <summary>
        /// Calculates the entropy for a given list of collisions based on the header we are trying to predict
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="Predicted"></param>
        /// <returns></returns>
        public static double Calculate_Entropy(List<CollisionEntry> entries, int Predicted)
        {
            //Add all unique values for the predicted value to a dictionary that keeps track of their count
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < entries.Count; i++)
            {

                int val = entries[i].vals[Predicted];
                if (counts.ContainsKey(val))
                    counts[val]++;
                else
                    counts.Add(val, 0);
            }

            double entropy = 0;
            int countOfExamples = entries.Count();
            foreach (KeyValuePair<int, int> pair in counts)
            {
                double d1 = (double)(pair.Value) / (double)countOfExamples;
                double d2 = Math.Log(d1, 2);
                entropy -= d1 * d2;
            }
            return entropy;
        }

        /// <summary>
        /// Returns true if all the elements in the collisions entries contain the same value ofr header c
        /// </summary>
        /// <param name="e"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool AllSame(List<CollisionEntry> e, int c)
        {
            int prev = e[0].vals[c];
            for (int i = 1; i < e.Count; i++)
            {
                if (e[i].vals[c] != prev)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a dictionary of all the headers with an array of their possible values
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static Dictionary<int, int[]> GetValuesOfHeaders(List<CollisionEntry> entries)
        {
            Dictionary<int, HashSet<int>> vals = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = 0; j < CollisionEntry.headers.Length; j++)
                {
                    if (vals.ContainsKey(j))
                    {
                        vals[j].Add(entries[i].vals[j]);
                    }
                    else
                    {
                        vals.Add(j, new HashSet<int>() { entries[i].vals[j] });
                    }
                }
            }
            Dictionary<int, int[]> retVal = new Dictionary<int, int[]>();
            foreach (KeyValuePair<int, HashSet<int>> pair in vals)
            {
                retVal.Add(pair.Key, pair.Value.ToArray());
            }

            return retVal;

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
