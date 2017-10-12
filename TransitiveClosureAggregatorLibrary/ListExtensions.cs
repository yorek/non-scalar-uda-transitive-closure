using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransitiveClosure
{
    public static class DictionaryExtensions
    {
        public static void AddUnique<T, S>(this Dictionary<T, S> dictionary, T key, S value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
        }

        public static void UnionWith<T, S>(this Dictionary<T, S> target, Dictionary<T, S> source)
        {
            foreach (var e in source)
            {
                if (!target.ContainsKey(e.Key))
                {
                    target.Add(e.Key, e.Value);
                }
            }
        }
    }
}
