using System;
using System.IO;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Collections;

namespace TransitiveClosure
{
    public class Pair
    {
        private int _x;
        private int _y;

        public int X => _x;
        public int Y => _y;

        public Pair(int x, int y)
        {
            if (x <= y)
            {
                _x = x;
                _y = y;
            } else
            {
                _x = y;
                _y = x;
            }
        }
    }

    public class Group: IEnumerable<int>
    {
        private Dictionary<int, bool> _group = new Dictionary<int, bool>();

        public Dictionary<int, bool>.KeyCollection Elements => _group.Keys;

        public int Count => _group.Keys.Count;

        public bool ContainsElement(int element)
        {
            return _group.ContainsKey(element);
        }

        public void AddUnique(Pair pair)
        {
            this.AddUnique(pair.X);
            this.AddUnique(pair.Y);
        }

        public void Add(int element)
        {
            _group.Add(element, true);
        }

        public void AddUnique(int element)
        {
            if (!_group.ContainsKey(element))
            {
                _group.Add(element, true);
            }
        }

        public void MergeWith(Group source)
        {
            foreach (var e in source.Elements)
            {
                this.AddUnique(e);
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            foreach(var e in _group.Keys)
            {
                yield return e;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
    }

    public class GroupSet: IEnumerable<Group>
    {
        private List<Group> _groupSet = new List<Group>();

        public int Count => _groupSet.Count;

        public void Add(Group group)
        {
            _groupSet.Add(group);
        }

        public List<Group> FindInGroups(Pair p)
        {
            var result = new List<Group>();

            foreach (Group g in _groupSet)
            {
                if (g.ContainsElement(p.X) || (g.ContainsElement(p.Y)))
                {
                    result.Add(g);
                    if (result.Count >= 2) break;
                }
            }

            return result;
        }

        public void AddPair(Pair p)
        {
            //Find if the inputValue is already in a group
            var foundInGroups = FindInGroups(p);

            // no item matches: create a new group and add both the inputValues to it
            if (foundInGroups.Count == 0)
            {
                var ng = new Group();
                ng.AddUnique(p.X);
                ng.AddUnique(p.Y);

                _groupSet.Add(ng);
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
                    _groupSet.Remove(g2);
                }
            }
        }

        public IEnumerator<Group> GetEnumerator()
        {
            foreach(var g in _groupSet)
            {
                yield return g;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
    }
  
    [Serializable]
    [SqlUserDefinedAggregateAttribute(Format.UserDefined, MaxByteSize = -1)]
    public class Aggregate : IBinarySerialize
    {
        private GroupSet _groupSet;

        public void Init()
        {
            _groupSet = new GroupSet();
        }

        public void Accumulate(int inputValue1, int inputValue2)
        {
            Pair p = new Pair(inputValue1, inputValue2);

            _groupSet.AddPair(p);                       
        }

        public void Merge(Aggregate value)
        {
            foreach (var g in value._groupSet)
            {
                int? pe = null;
                foreach (var ce in g)
                {
                    if (pe.HasValue)
                    {
                        this.Accumulate(pe.Value, ce);
                    }
                    pe = ce;
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
            foreach (var g in this._groupSet)
            {
                sb.Append("\"" + c + "\":[");

                var ea = new int[g.Elements.Count];
                g.Elements.CopyTo(ea, 0);

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
            _groupSet = new GroupSet();

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
                    l.Add(r.ReadInt32());
                }

                // Add list to dictionary
                _groupSet.Add(l);
            }
        }

        public void Write(BinaryWriter w)
        {
            if (w == null) throw new ArgumentNullException("w");

            // Group count
            w.Write(_groupSet.Count);

            foreach (var g in _groupSet)
            {
                // Values Count
                w.Write(g.Count);

                // Values
                foreach (var e in g)
                {
                    w.Write(e);
                }
            }
        }
    }
}