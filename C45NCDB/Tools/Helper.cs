using C45NCDB.RuleEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using C45NCDB.DecisionTree;

namespace C45NCDB.Tools {
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

        internal static List<string> ReadSpecialHeaders(string filePath)
        {
            StreamReader reader = new StreamReader(filePath);
            string line = "";
            int LineNumber = 1;
            List<String> retVal = new List<string>();
            while ((line = reader.ReadLine()) != null && line != String.Empty)
            {
                string header = line.Trim();
                if (header.Split().Count() != 2)
                    throw new Exception("Invalid header file syntax on line "+ LineNumber + ", expected syntax per line: ignore <header> OR cont <header>");
                retVal.Add(header);
            }
            reader.Close();
            return retVal;
        }

        /// <summary>
        /// Generates a rule that divides the dataset in a way that causes for the most equal split
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static Rule Generate_Rule_From_Even_Divisions(List<CollisionEntry> collection, List<int> headersToIgnore)
        {
            double information_gain_best = double.MaxValue;
            int header_corresponding = -1;
            int value_corresponding = -1;

            for (int i = 0; i < CollisionEntry.headers.Length; i++)
            {
                if (headersToIgnore.Contains(i))
                    continue;
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
            if (header_corresponding == -1)
                return null;
            headersToIgnore.Add(header_corresponding);
            Rule r = new Rule(header_corresponding, value_corresponding, HValType.HeaderCompVal, Operator.Equals);
            return r;
        }
     
        
        /// <summary>
        /// Finds the attribute with the highest information gain
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="header_To_Predict"></param>
        /// <returns></returns>
        public static Tuple<int, int> AttributeWithBestInfoGain(List<CollisionEntry> entries, int header_To_Predict, List<int> headersToIgnore)
        {
			Dictionary<int, int[]> valuesOfHeaders = GetValuesOfHeaders(entries);

            //Current best Values
            double InfoGain = double.MinValue;
            int attributeWithBest = -1;
			int thresholdValue = -1;

            //For each attribute i
            for (int i = 0; i < CollisionEntry.headers.Length; i++)
            {
                if (i == header_To_Predict)
                    continue;
                if (headersToIgnore.Contains(i))
                    continue;

				Tuple<double, int> current = null;

				if (C4p5.continuousHeaders.Contains(i)) {
					current = getContinuousInfoGain(entries, header_To_Predict, i);
					//Console.WriteLine(i + " C: " + current);
				} else {
					current = getDiscreteInfoGain(entries, header_To_Predict, valuesOfHeaders, i);
					//Console.WriteLine(i + " D: " + current);
				}
				

				if (current.Item1 > InfoGain)
                {
                    InfoGain = current.Item1;
                    attributeWithBest = i;
					thresholdValue = current.Item2;
				}
            }
            return new Tuple<int, int>(attributeWithBest, thresholdValue);
        }

		/// <summary>
		/// Calculates the information gain for discrete attributes
		/// </summary>
		/// <param name="entries"></param>
		/// <param name="header_To_Predict"></param>
		/// <param name="valuesOfHeaders"></param>
		/// <param name="headerIndex"></param>
		/// <returns></returns>
		public static Tuple<double, int> getDiscreteInfoGain (List<CollisionEntry> entries, int header_To_Predict, Dictionary<int, int[]> valuesOfHeaders, int headerIndex) {
			double entropyForAttributeI = 0;
			double splitForAttributeI = 0;
			int total = 0;
			//For each value of attribute i
			for (int ValueIndex = 0; ValueIndex < valuesOfHeaders[headerIndex].Length; ValueIndex++) {
				int Value = valuesOfHeaders[headerIndex][ValueIndex];
				List<CollisionEntry> subset = entries.Where(x => x.vals[headerIndex] == Value).ToList();
				total += subset.Count;
				double entropyOfDivisions = Calculate_Entropy(subset, header_To_Predict);
				double Probability = (double)subset.Count / (double)entries.Count;
				entropyForAttributeI += Probability * entropyOfDivisions;

				splitForAttributeI -= Probability * Math.Log(Probability, 2);
			}

			double totalEntropy = Calculate_Entropy(entries, header_To_Predict);
			double gainForAttributeI = (totalEntropy - entropyForAttributeI);
			if (splitForAttributeI == 0) return new Tuple<double, int>(-1, -1);
			return new Tuple<double, int>(gainForAttributeI / splitForAttributeI, -1);
		}

		class ContinuousThresholdData {
			public int thresholdIndex;
			public double gainToSplitRatio;
			public double gain;
			public double split;
		}

		/// <summary>
		/// Calculates the GAIN information for the continuous attributes
		/// </summary>
		/// <param name="entries"></param>
		/// <param name="header_To_Predict"></param>
		/// <param name="headerIndex"></param>
		/// <returns></returns>
		public static Tuple<double, int> getContinuousInfoGain (List<CollisionEntry> entries, int header_To_Predict, int headerIndex) {

			double averageGain = 0;
			double totalEntropy = Calculate_Entropy(entries, header_To_Predict);
			List<int> thresholdInts = getAllPresentAttributeValuesSorted(entries, headerIndex);
			List<ContinuousThresholdData> allThresholds = new List<ContinuousThresholdData>();
			//double penalty = Math.Log((thresholdInts.Count - 1), 2) / entries.Count;

			for (int ValueIndex = 1; ValueIndex < thresholdInts.Count; ValueIndex++) {
				int thresholdValue = thresholdInts[ValueIndex];
				List<CollisionEntry> subset1 = entries.Where(x => x.vals[headerIndex] < thresholdValue).ToList();
				List<CollisionEntry> subset2 = entries.Where(x => x.vals[headerIndex] >= thresholdValue).ToList();
				double entropyOfDivisions1 = Calculate_Entropy(subset1, header_To_Predict);
				double entropyOfDivisions2 = Calculate_Entropy(subset2, header_To_Predict);
				double Probability1 = (double)subset1.Count / (double)entries.Count;
				double Probability2 = (double)subset2.Count / (double)entries.Count;

				ContinuousThresholdData ctd = new ContinuousThresholdData();
				ctd.thresholdIndex = ValueIndex;
				ctd.split = -((Probability1 * Math.Log(Probability1, 2)) + (Probability2 * Math.Log(Probability2, 2)));
				ctd.gain = (totalEntropy - ((Probability1 * entropyOfDivisions1) + (Probability2 * entropyOfDivisions2)));// - penalty;
				ctd.gainToSplitRatio = ctd.gain / ctd.split;


				allThresholds.Add(ctd);
				averageGain += ctd.gain;
			}
			averageGain /= thresholdInts.Count - 1;

			double currentHighest = 0;
			ContinuousThresholdData currentCTD = null;

			// Select the based on Highest Gain Ratio - Normal method
			// Among those with at least average gain
			List<ContinuousThresholdData> onlyAboveAverage = allThresholds.Where(x => x.gain > averageGain).ToList();
			foreach (ContinuousThresholdData ctd in onlyAboveAverage) {
				if (ctd.gainToSplitRatio > currentHighest) {
					currentHighest = ctd.gainToSplitRatio;
					currentCTD = ctd;
				}
			}

			// Select the based on Highest Gain - Recommended Change Maybe, not sure if this is correct
			/*foreach (ContinuousThresholdData ctd in allThresholds) {
				if (ctd.gain > currentHighest) {
					currentHighest = ctd.gain;
					currentCTD = ctd;
				}
			}*/

			if (currentCTD == null) return new Tuple<double, int>(-1, -1);
			return new Tuple<double, int>(currentCTD.gainToSplitRatio, thresholdInts[currentCTD.thresholdIndex]);
		}

		/// <summary>
		/// Returns all of the present values for a given header in the entries in a sorted list
		/// </summary>
		/// <param name="entries"></param>
		/// <param name="headerIndex"></param>
		/// <returns></returns>
		public static List<int> getAllPresentAttributeValuesSorted (List<CollisionEntry> entries, int headerIndex) {
			HashSet<int> intsHash = new HashSet<int>();
			foreach (CollisionEntry ce in entries) {
				intsHash.Add(ce.vals[headerIndex]);
			}
			List<int> intsList = intsHash.ToList<int>();
			intsList.Sort();
			return intsList;
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
                    counts.Add(val, 1);
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
        }
    }
}
