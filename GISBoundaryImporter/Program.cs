// Program.cs
using System;
using System.Windows.Forms;  // needed for Application.*

namespace GISBoundaryImporter
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}