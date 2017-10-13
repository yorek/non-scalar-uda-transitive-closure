﻿using System;
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
        public int X;
        public int Y;
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
            throw new NotImplementedException();
        }
    }

    public class GroupSet: IEnumerable<Group>
    {
        private int _groupCounter = 0;

        private Dictionary<int, Group> _groupSet = new Dictionary<int, Group>();

        public int Count => _groupSet.Count;

        public void Add(Group group)
        {
            _groupSet.Add(_groupCounter, group);
            _groupCounter += 1;
        }

        public List<int> FindInGroups(Pair p)
        {
            var result = new List<int>();

            foreach (int k in _groupSet.Keys)
            {
                Group g = _groupSet[k];
                if (g.ContainsElement(p.X) || (g.ContainsElement(p.Y)))
                {
                    result.Add(k);
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

                _groupSet.Add(_groupCounter, ng);
                _groupCounter += 1;
                //Console.WriteLine("New group created. Count: {0}", _groups.Count);
            }

            // one item match, add the related item to the same group
            if (foundInGroups.Count == 1)
            {
                var groupId = foundInGroups[0];
                _groupSet[groupId].AddUnique(p);
            }

            // if there is a match for both items but in two different groups
            // merge them into just one group and delete the other
            if (foundInGroups.Count >= 2)
            {
                var group1Id = foundInGroups[0];
                for (int i = 1; i < foundInGroups.Count; i++)
                {
                    var group2Id = foundInGroups[i];
                    _groupSet[group1Id].MergeWith(_groupSet[group2Id]);
                    _groupSet.Remove(group2Id);
                    //Console.WriteLine("Group merged. Count: {0}", _groups.Count);
                }
            }
        }

        public IEnumerator<Group> GetEnumerator()
        {
            foreach(var g in _groupSet.Values)
            {
                yield return g;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
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
            Pair p = new Pair() { X = inputValue1, Y = inputValue2 };

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