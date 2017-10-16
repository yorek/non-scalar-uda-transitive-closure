using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TransitiveClosure;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Aggregate a1 = new Aggregate();
            a1.Init();

            a1.Accumulate(1, 2);
            a1.Accumulate(3, 4);
            a1.Accumulate(2, 3);

            a1.Accumulate(25, 24);

            a1.Accumulate(90, 89);

            a1.Accumulate(60, 61);
            a1.Accumulate(61, 60);
            a1.Accumulate(62, 62);
            a1.Accumulate(62, 61);
            a1.Accumulate(60, 61);

            a1.Accumulate(17, 24);
            a1.Accumulate(18, 24);
            a1.Accumulate(18, 25);
            a1.Accumulate(18, 17);

            a1.Accumulate(100, 103);
            a1.Terminate();

            Console.WriteLine("First accumulation result:");
            Console.WriteLine(a1);
            Console.WriteLine();

            Aggregate a2 = new Aggregate();
            a2.Init();

            a2.Accumulate(1, 2);
            a2.Accumulate(3, 4);
            a2.Accumulate(2, 3);

            a2.Accumulate(18, 14);
            a2.Accumulate(14, 20);

            a2.Accumulate(90, 88);

            a2.Accumulate(100, 101);
            a2.Accumulate(101, 102);
            a2.Accumulate(102, 100);

            a2.Accumulate(1000, 1001);
            a2.Accumulate(1000, 1002);
            a2.Accumulate(1000, 1003);

            a2.Accumulate(1100, 1001);
            a2.Accumulate(1100, 1002);
            a2.Accumulate(1100, 1003);

            a2.Accumulate(1100, 1000);

            a2.Terminate();

            Console.WriteLine("Second accumulation result:");
            Console.WriteLine(a2);
            Console.WriteLine();

            a1.Merge(a2);

            Console.WriteLine("Merge result:");
            Console.WriteLine(a1);
            Console.WriteLine();

            Console.WriteLine("Writing to stream...");
            Console.WriteLine("G: {0}, N: {1}", a1.Groups, a1.Numbers);
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            a1.Write(bw);
            Console.WriteLine();

            Console.WriteLine("Reading from stream...");
            ms.Position = 0;
            BinaryReader br = new BinaryReader(ms);
            Aggregate a3 = new Aggregate();
            a3.Read(br);
            Console.WriteLine("Read/Write cycle result:");
            Console.WriteLine("G: {0}, N: {1}", a3.Groups, a3.Numbers);
            Console.WriteLine(a3);
            Console.WriteLine();

            Console.ReadLine();

            Console.WriteLine("High cardinality accumulation...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Aggregate a4 = new Aggregate();
            a4.Init();
            int v = 0;
            foreach (var e in GenerateRandomGroups())
            {
                a4.Accumulate(e[0], e[1]);
                v += 1;
                if (v % 1000 == 0) Console.WriteLine("Accumulated {0} values in {1} groups [M: {2}]...", v, a4.Groups, a4.Merges);
            }
            a4.Terminate();
            sw.Stop();

            JObject j = JObject.Parse(a4.ToString());

            Console.WriteLine();
            foreach (var n in j.Children())
            {
                if (n.Type == JTokenType.Property)
                {
                    var c = n.Children().First();
                    if (c.Type == JTokenType.Array)
                    {
                        //Console.WriteLine("{0}: {1} => {2}", ((JProperty)n).Name, c.Count(), c.ToString(Formatting.None));
                        Console.WriteLine("{0}: {1}", ((JProperty)n).Name, c.Count());
                    }
                }
            }

            //Console.WriteLine("High cardinality accumulation result:");
            //Console.WriteLine(a4);
            Console.WriteLine();
            Console.WriteLine("Time taken: {0} ms", sw.ElapsedMilliseconds);
            Console.ReadLine();
        }

        static IEnumerable<int[]> GenerateWellKnownRandomGroups()
        {
            var rnd = new Random();
            int minGroups = 100;
            int rowsPerGroup = 1000;
            foreach (var g in Enumerable.Range(1, minGroups))
            {
                foreach (var r in Enumerable.Range(1, rowsPerGroup))
                {
                    int x = 0;
                    int y = 0;
                    while (x == y)
                    {
                        x = ((g - 1) * (rowsPerGroup + 1)) + r;
                        y = x + rnd.Next(rowsPerGroup * minGroups) % (rowsPerGroup + 1 - r) + 1;
                    }

                    yield return new int[] { x, y };
                }
            }
        }

        static IEnumerable<int[]> GenerateRandomGroups()
        {
            var rnd = new Random();
            int minGroups = 100;
            int rowsPerGroup = 1000;
            foreach (var g in Enumerable.Range(1, minGroups))
            {
                foreach (var r in Enumerable.Range(1, rowsPerGroup))
                {
                    int x = 0;
                    int y = 0;
                    while (x == y)
                    {
                        x = ((g - 1) * (rowsPerGroup + 1)) + r;
                        y = rnd.Next(minGroups * rowsPerGroup);
                    }

                    yield return new int[] { x, y };
                }
            }
        }
    }
}
