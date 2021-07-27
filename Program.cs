using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Console;
using LiveSplit.ComponentUtil;

namespace print_demo_info_immediate
{
    class Program
    {
        public static string ListDemoPath { get; set; }
        public static string GameExe { get; set; }

        private static SettingsHandler settings = new SettingsHandler();

        static void Main(string[] args)
        {
            if (File.Exists("config.xml"))
                settings.ReadSettings();
            else settings.FirstTimeSettings();

            MemoryHandler handler = new MemoryHandler();


        }
    }
}
