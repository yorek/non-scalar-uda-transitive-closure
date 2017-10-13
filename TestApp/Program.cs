using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
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

            a1.Accumulate(17, 24);
            a1.Accumulate(18, 24);
            a1.Accumulate(18, 25);
            a1.Accumulate(18, 17);
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
            a2.Terminate();

            Console.WriteLine("Second accumulation result:");          
            Console.WriteLine(a2);
            Console.WriteLine();

            a1.Merge(a2);

            Console.WriteLine("Merge result:");        
            Console.WriteLine(a1);
            Console.WriteLine();

            Console.WriteLine("Writing to stream...");
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            a1.Write(bw);

            Console.WriteLine("Reading from stream...");
            ms.Position = 0;
            BinaryReader br = new BinaryReader(ms);            
            Aggregate a3 = new Aggregate();
            a3.Read(br);

            Console.WriteLine("Read/Write cycle result:");
            Console.WriteLine(a3);
            Console.WriteLine();

            Console.ReadLine();

            Console.WriteLine("High cardinality accumulation...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Aggregate a4 = new Aggregate();
            a4.Init();
            var rnd = new Random();
            foreach (var n in Enumerable.Range(0, 100000))
            {
                a4.Accumulate(rnd.Next(100000), rnd.Next(100000));
                if (n % 1000 == 0) Console.WriteLine("Accumulated {0} values in {1} groups...", n, a4.Groups);
            }
            a4.Terminate();
            sw.Stop();
            //Console.WriteLine("High cardinality accumulation result:");
            //Console.WriteLine(a4);
            Console.WriteLine();
            Console.WriteLine("Time taken: {0} ms", sw.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
