using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Exploreader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
       public static void Main(string[] args)
        {
            string SelectedFile = "-";
            if (args.Length > 0)
                SelectedFile = Convert.ToString(args[0]);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain(SelectedFile));
        }
    }
}
