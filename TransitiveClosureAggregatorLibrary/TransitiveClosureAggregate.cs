using System;
using System.IO;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;

namespace TransitiveClosure
{
    public class Groups: List<Group>
    {
        private Group _values = new Group();

        public Groups FindInGroups(Pair p)
        {
            var result = new Groups();

            foreach (var g in this)
            {
                if (g.ContainsKey(p.X) || (g.ContainsKey(p.Y)))
                {
                    result.Add(g);
                }
            }

            return result;
        }

        public new void Add(Group g)
        {                       
            base.Add(g);
        }
    }

    public class Group: Dictionary<int, bool>
    {
        public KeyCollection Elements => this.Keys;

        public void AddUnique(Pair pair)
        {
            this.AddUnique(pair.X);
            this.AddUnique(pair.Y);
        }

        public void AddUnique(int element)
        {
            if (!this.ContainsKey(element))
            {
                this.Add(element, true);
            }
        }

        public void MergeWith(Group source)
        {
            foreach (var e in source)
            {
                this.AddUnique(e.Key);
            }
        }
    }

    public class Pair
    {
        public int X;
        public int Y;       
    }

    [Serializable]
    [SqlUserDefinedAggregateAttribute(Format.UserDefined, MaxByteSize = -1)]
    public class Aggregate : IBinarySerialize
    {
        private Groups _groups;

        public void Init()
        {
            _groups = new Groups();
        }

        public void Accumulate(int inputValue1, int inputValue2)
        {
            Pair p = new Pair() { X = inputValue1, Y = inputValue2 };

            //Find if the inputValue is already in a group
            var foundInGroups = _groups.FindInGroups(p);

            // no item matches: create a new group and add both the inputValues to it
            if (foundInGroups.Count == 0)
            {
                var ng = new Group();
                ng.AddUnique(p.X);
                ng.AddUnique(p.Y);

                _groups.Add(ng);
                //Console.WriteLine("New group created. Count: {0}", _groups.Count);
            }

            // one item match, add the related item to the same group
            if (foundInGroups.Count == 1)
            {
                var g = foundInGroups[0];
                g.AddUnique(p);                
            }

            // if there is a match for both items but in two different groups
            // merge them into just one group and delete the other
            if (foundInGroups.Count >= 2)
            {
                var g1 = foundInGroups[0];
                for (int i = 1; i < foundInGroups.Count; i++)
                {
                    var g2 = foundInGroups[i];
                    g1.MergeWith(g2);
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
                foreach (var ce in g)
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
            int c = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var g in this._groups)
            {
                sb.Append("\"" + c + "\":[");

                var ea = new int[g.Keys.Count];
                g.Keys.CopyTo(ea, 0);

                sb.Append(string.Join(",", ea));
                
                sb.Append("],");

                c += 1;
            }
            if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            return sb.ToString();
        }

        public void Read(BinaryReader r)
        {
            if (r == null) throw new ArgumentNullException("r");
            _groups = new Groups();

            // Group Count
            int g = r.ReadInt32();

            // For Each Group
            for (int j = 0; j < g; j++)
            {
                var l = new Group();               

                // List Size (or Values Count)
                int s = r.ReadInt32();

                // Read values and put them in the list
                for (int i = 0; i < s; i++)
                {
                    l.Add(r.ReadInt32(), true);
                }

                // Add list to dictionary
                _groups.Add(l);
            }
        }

        public void Write(BinaryWriter w)
        {
            if (w == null) throw new ArgumentNullException("w");

            // Group count
            w.Write(_groups.Count);

            foreach (var g in _groups)
            {
                // Values Count
                w.Write(g.Count);

                // Values
                foreach (var e in g)
                {
                    w.Write(e.Key);
                }
            }
        }
    }
}