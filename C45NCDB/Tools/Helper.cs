using C45NCDB.RuleEngine;
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
        }
    }
}
