using System;
using System.IO;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;

namespace TransitiveClosure
{
    public class Pair
    {
        public int X;
        public int Y;
    }

    [Serializable]
    [SqlUserDefinedAggregateAttribute(Format.UserDefined, MaxByteSize = -1)]
    public class Aggregate : IBinarySerialize
    {
        private Dictionary<int, Dictionary<int, bool>> _groups;
        private int _groupCounter;
        private List<Pair> _notInGroup;

        public void Init()
        {
            _notInGroup = new List<Pair>();

            _groups = new Dictionary<int, Dictionary<int, bool>>();

            _groupCounter = 0;
        }

        public void Accumulate(int inputValue1, int inputValue2)
        {
            Pair p = new Pair() { X = inputValue1, Y = inputValue2 };

            List<int> found = new List<int>();
        
            List<Pair> toMoveInList = new List<Pair>();

            //Find if the inputValue is already in a group
            foreach (var g in _groups)
            {                
                if (g.Value.ContainsKey(p.X) || (g.Value.ContainsKey(p.Y)))
                {
                    found.Add(g.Key);
                }
            }

            // no item matches: create a new group and add both the inputValues to it
            if (found.Count == 0)
            {
                _groupCounter += 1;
                var ng = new Dictionary<int, bool>();
                ng.AddUnique(p.X, true);
                ng.AddUnique(p.Y, true);

                _groups.Add(_groupCounter, ng);
                //Console.WriteLine("New group created. Count: {0}", _groups.Count);
            }

            // one item match, add the related item to the same group
            if (found.Count == 1)
            {
                var g = found[0];
                _groups[g].AddUnique(p.X, true);
                _groups[g].AddUnique(p.Y, true);
            }

            // if there is a match for both items but in two different groups
            // merge them into just one group and delete the other
            if (found.Count >= 2)
            {
                var g1 = found[0];
                for (int i = 1; i < found.Count; i++)
                {
                    var g2 = found[i];
                    _groups[g1].UnionWith(_groups[g2]);
                    _groups.Remove(g2);
                    //Console.WriteLine("Group merged. Count: {0}", _groups.Count);
                }
            }                
        }

        public void Merge(Aggregate value)
        {
            foreach (var g in value._groups)
            {
                int? pe = null;
                foreach (var ce in g.Value)
                {
                    if (pe.HasValue)
                    {
                        this.Accumulate(pe.Value, ce.Key);
                    }
                    pe = ce.Key;
                }
            }
        }     

        public SqlString Terminate()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var g in this._groups)
            {
                sb.Append("\"" + g.Key + "\":[");

                if (g.Value != null)
                {
                    var ea = new int[g.Value.Keys.Count];
                    g.Value.Keys.CopyTo(ea, 0);

                    sb.Append(string.Join(",", ea));
                }
                sb.Append("],");
            }
            if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            return sb.ToString();
        }

        public void Read(BinaryReader r)
        {
            if (r == null) throw new ArgumentNullException("r");
            _groups = new Dictionary<int, Dictionary<int, bool>>();

            // Group Count
            int g = r.ReadInt32();

            // For Each Group
            for (int j = 0; j < g; j++)
            {
                var l = new Dictionary<int, bool>();

                // Group Key 
                int k = r.ReadInt32();

                // List Size (or Values Count)
                int s = r.ReadInt32();

                // Read values and put them in the list
                for (int i = 0; i < s; i++)
                {
                    l.Add(r.ReadInt32(), true);
                }

                // Add list to dictionary
                _groups.Add(k, l);
            }
        }

        public void Write(BinaryWriter w)
        {
            if (w == null) throw new ArgumentNullException("w");

            // Group count
            w.Write(_groups.Count);

            foreach (var g in _groups)
            {
                // Group Key
                w.Write(g.Key);

                // Values Count
                w.Write(g.Value.Count);

                // Values
                foreach (var e in g.Value)
                {
                    w.Write(e.Key);
                }
            }
        }
    }
}