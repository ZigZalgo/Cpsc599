using C45NCDB.RuleEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB
{
    class CodifyInput
    {
        static void Main(string[] args)
        {
            StreamReader reader = new StreamReader(args[0]);
            string line = reader.ReadLine();
            //Set up our string ids to be the ids of the file
            CollisionEntry.headers = removeLast(line.Split(',')); 
            //Create a list of entries for our file
            List<CollisionEntry> collisions = new List<CollisionEntry>();
            while ((line = reader.ReadLine()) != null && line != string.Empty)
            {
                string[] stringCode = removeLast(line.Split(','));
                //set up our int array for the collision entry
                byte[] intCode = new byte[stringCode.Length];
                //For each string value in this collisions
                for (int i = 0; i < stringCode.Length; i++)
                {
                    //Custom number for the year :P
                    if(i == 0)
                    {
						byte result;
                        if (byte.TryParse(stringCode[i].Substring(2), out result))
                        {
                            intCode[0] = result;
                        }
                        else
                        {
                            if (stringCode[i].Contains("U"))
                                intCode[i] = 255;
                            if (stringCode[i].Contains("X"))
                                intCode[i] = 254;
                            if (stringCode[i].Contains("Q"))
                                intCode[i] = 253;
                            if (stringCode[i].Contains("N"))
                                intCode[i] = 252;
                        }
                    }
					byte resCode;
                    if(byte.TryParse(stringCode[i], out resCode))
                    {
                        intCode[i] = resCode;
                    }
                    else
                    {
                        if (stringCode[i].Contains("U"))
                            intCode[i] = 255;
                        if (stringCode[i].Contains("X"))
                            intCode[i] = 254;
                        if (stringCode[i].Contains("Q"))
                            intCode[i] = 253;
                        if (stringCode[i].Contains("N"))
                            intCode[i] = 252;
                        if (stringCode[i].Contains("M"))
                            intCode[i] = 0;
                        if (stringCode[i].Contains("F"))
                            intCode[i] = 1;
                    }
                }
                CollisionEntry toBeAdded = new CollisionEntry(intCode);
                collisions.Add(toBeAdded);
            }
            CollisionEntry.WriteCollisionsToFile(collisions);
        }
        /// <summary>
        /// Used top remove the C_CASE values because they are entirely unique thus do not do anything for learning
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static string[] removeLast(string[] toTrim)
        {
            string[] retVal = new string[toTrim.Length - 1];
            for (int i = 0; i < toTrim.Length - 1; i++)
            {
                retVal[i] = toTrim[i];
            }
            return retVal;
        }
    }

    public class CollisionEntry
    {
        public static string[] headers;
        public byte[] vals;
        public CollisionEntry(byte[] input)
        {
            vals = input;
        }

        public static void EvaluateEntries(List<CollisionEntry> entries, List<CollisionEntry> failed, List<CollisionEntry> passed, Rule rule)
        {
            foreach(CollisionEntry entry in entries)
            {
                if(rule.Evaluate(entry))
                {
                    passed.Add(entry);
                }
                else
                {
                    failed.Add(entry);
                }
            }
        }

        /// <summary>
        /// 
        /// <para>FILE FORMAT</para>
        /// #of collisions (N), #of fields, field names, collision1, collision2, ..., collisionN
        /// </summary>
        /// <param name="collisions"></param>
        /// <param name="names"></param>
        /// <param name="codes"></param>
        public static void WriteCollisionsToFile(List<CollisionEntry> collisions)
        {
            Console.WriteLine("Writing "+collisions.Count() + " to file");
            BinaryWriter writer = new BinaryWriter(File.Open("NCDB.binary", FileMode.Create));
            writer.Write(collisions.Count);
            writer.Write(CollisionEntry.headers.Length);
            for (int i = 0; i < CollisionEntry.headers.Length; i++)
                writer.Write(CollisionEntry.headers[i]);
            foreach (CollisionEntry c in collisions)
                WriteCollisionToFile(c, writer);
            writer.Close();
        }

        public static void WriteCollisionToFile(CollisionEntry entry, BinaryWriter writer)
        {
            for(int i = 0; i < entry.vals.Length; i++)
            {
                writer.Write(entry.vals[i]);
            }
        }

        public static CollisionEntry[] ReadCollisionsFromFile(string fileName)
        {
            Console.Write("Reading ");
            BinaryReader reader = new BinaryReader(File.OpenRead(fileName));
            int numCollisions = reader.ReadInt32();
            Console.WriteLine(numCollisions + " from File");
            int length = reader.ReadInt32();
            CollisionEntry.headers = new string[length];
            CollisionEntry[] retVal = new CollisionEntry[numCollisions];
            for(int i = 0; i < length; i++)
            {
                headers[i] = reader.ReadString();
            }
            for(int i = 0; i < numCollisions; i++)
            {
                retVal[i] = ReadCollisionFromFile(reader, length);
            }
            reader.Close();
            return retVal;
        }

        public static CollisionEntry ReadCollisionFromFile(BinaryReader reader, int numOfFields)
        {
            byte[] vals = new byte[numOfFields];
            for(int i = 0; i < vals.Length; i++)
            {
                vals[i] = reader.ReadByte();
            }
            CollisionEntry c = new CollisionEntry(vals);
            return c;
        }

    }
}
