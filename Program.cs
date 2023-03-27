using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TestingTaskFramework;

namespace TestingTask
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            int seed = 0;
            if (args.Length > 0) int.TryParse(args[0], out seed);

            Framework.Run(new ShipBehavior(), new World(), seed);
        }
    }
}
