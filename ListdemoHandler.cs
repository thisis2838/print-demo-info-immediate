using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace print_demo_info_immediate
{
    class ListdemoHandler
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);

        public ListdemoHandler()
        {

        }

        public void DemoStopNotify(string demoName, string gameDir)
        {
            Process listdemo = new Process();
            listdemo.StartInfo.FileName = Program.ListDemoPath;
            listdemo.StartInfo.Arguments = $"\"{gameDir}\\{demoName}.dem\"";
            listdemo.StartInfo.UseShellExecute = false;
            listdemo.StartInfo.CreateNoWindow = false;
            listdemo.StartInfo.RedirectStandardOutput = true;
            listdemo.Start();
            StreamReader sr = listdemo.StandardOutput;
            Thread.Sleep(500);
            if (!listdemo.HasExited)
                listdemo.Kill();
            string output = sr.ReadToEnd();
            listdemo.WaitForExit();

            WriteLine($"Demo {demoName} stopped recording, printing info...");
            WriteLine(output);
            MemoryHandler.writemsg(output);

        }
    }
}
