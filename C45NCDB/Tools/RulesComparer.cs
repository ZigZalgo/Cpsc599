using C45NCDB.RuleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C45NCDB.Tools
{
    public class RulesComparer : IEqualityComparer<Rule>
    {
        public bool Equals(Rule x, Rule y)
        {
            return(x.op == y.op && x.type == y.type && x.v1 == y.v1 && x.v2 == y.v2);
        }

        public int GetHashCode(Rule obj)
        {
            byte[] a = Encoding.ASCII.GetBytes(obj.op.ToString());
            byte[] b = Encoding.ASCII.GetBytes(obj.type.ToString());
            byte[] c = Encoding.ASCII.GetBytes(obj.v1.ToString());
            byte[] d = Encoding.ASCII.GetBytes(obj.v2.ToString());
            List<byte> byteList = a.ToList();
            byteList.AddRange(b);
            byteList.AddRange(c);
            byteList.AddRange(d);
            a = byteList.ToArray();
            return a.GetHashCode();
        }
    }
}
