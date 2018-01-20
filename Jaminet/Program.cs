using System;
using System.Threading.Tasks;

namespace HeurekaGrab
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Heureka heureka = new Heureka();
            heureka.GetProductParameters("KHX-HSCP-RD", "Kingston");
            //Console.Write(specifications);
        }
    }
}
