using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveSplit.ComponentUtil;
using System.IO;
using static System.Console;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace print_demo_info_immediate
{
    class MemoryHandler
    {
        [DllImport(@"src-console-sendmsg.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void writemsg(string input);
        [DllImport(@"src-console-sendmsg.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void init(string procname);

        private SigScanTarget _gameDirTarget;
        private SigScanTarget _demoRecorderTarget;
        private MemoryWatcher<bool> _demoIsRecording;
        private MemoryWatcher<int> _demoIndex;
        private StringWatcher _demoName;
        private string _gameProcesName;
        private int _startTickOffset = -1;
        private Process _game;
        private ListdemoHandler listdemo;
        private string _gameDir;
        public MemoryHandler()
        {
            _gameDirTarget = new SigScanTarget(0, "25732F736176652F25732E736176"); // "%s/save/%s.sav"
            _gameDirTarget.OnFound = (proc, scanner, ptr) => 
            {
                byte[] b = BitConverter.GetBytes(ptr.ToInt32());
                var target = new SigScanTarget(-4, $"68 {b[0]:X02} {b[1]:X02} {b[2]:X02} {b[3]:X02}");
                return proc.ReadPointer(scanner.Scan(target));
            };

            _demoRecorderTarget = new SigScanTarget(0, "416c7265616479207265636f7264696e672e");
            _demoRecorderTarget.OnFound = (proc, scanner, ptr) =>
            {
                byte[] b = BitConverter.GetBytes(ptr.ToInt32());
                var target = new SigScanTarget(-95, $"68 {b[0]:X02} {b[1]:X02} {b[2]:X02} {b[3]:X02}");

                IntPtr byteArrayPtr = scanner.Scan(target);
                if (byteArrayPtr == IntPtr.Zero)
                    return IntPtr.Zero;

                byte[] bytes = new byte[100];
                proc.ReadBytes(scanner.Scan(target), 100).CopyTo(bytes, 0);
                for (int i = 98; i >= 0; i--)
                {
                    if (bytes[i] == 0x8B && bytes[i + 1] == 0x0D)
                        return proc.ReadPointer(proc.ReadPointer(byteArrayPtr + i + 2));
                }

                return IntPtr.Zero;
            };

            _gameProcesName = Path.GetFileNameWithoutExtension(Program.GameExe);
            listdemo = new ListdemoHandler();

            Start();
        }

        private void Start()
        {
            while (true)
            {
                try
                {
                    WriteLine($"Scanning for game process \"{_gameProcesName}\".");
                retry:
                    _game = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower() == _gameProcesName.ToLower());
                    while (!Scan())
                    {
                        Thread.Sleep(750);
                        goto retry;
                    }
                }
                catch (Exception ex) // probably a Win32Exception on access denied to a process
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }

        private bool Scan()
        {
            if (_game == null || _game.HasExited)
                return false;

            ProcessModuleWow64Safe engine = _game.ModulesWow64Safe().FirstOrDefault(x => x.ModuleName.ToLower() == "engine.dll");
            if (engine == null)
                return false;

            init(Program.GameExe);

            SignatureScanner scanner = new SignatureScanner(_game, engine.BaseAddress, engine.ModuleMemorySize);

            IntPtr demoRecorderPtr, gameDirPtr;

            WriteLine("Scanning for game directory pointer...");
            gameDirPtr = scanner.Scan(_gameDirTarget);
            ReportPointer(gameDirPtr, "directory");
            if (gameDirPtr == IntPtr.Zero)
                throw new Exception();
            else
                _gameDir = _game.ReadString(gameDirPtr, 260);


            WriteLine("Scanning for g_pClientDemoPlayer pointer...");
            demoRecorderPtr = scanner.Scan(_demoRecorderTarget);
            ReportPointer(demoRecorderPtr, "g_pClientDemoPlayer");
            if (demoRecorderPtr == IntPtr.Zero)
                throw new Exception();
            else
            {
                IntPtr tmpPtr = _game.ReadPointer(_game.ReadPointer(demoRecorderPtr) + 0x4);
                SignatureScanner tmpScanner = new SignatureScanner(_game, tmpPtr, 0x70);
                SigScanTarget target = new SigScanTarget(2, "2B 86 ?? ?? 00 00");
                tmpPtr = tmpScanner.Scan(target);
                if (tmpPtr != IntPtr.Zero)
                {
                    _startTickOffset = _game.ReadValue<int>(tmpPtr);
                    WriteLine($"Found m_nStartTick offset at 0x{_startTickOffset:X}");
                }
                else
                {
                    _startTickOffset = 0x53C;
                    WriteLine("Couldn't find m_nStartTick offset, using default of 0x53C (unpack)");
                }

                _demoIsRecording = new MemoryWatcher<bool>(demoRecorderPtr + _startTickOffset + 4 + 260 + 1 + 1);
                _demoIndex = new MemoryWatcher<int>(demoRecorderPtr + _startTickOffset + 4 + 260 + 1 + 1 + 2);
                _demoName = new StringWatcher(demoRecorderPtr + _startTickOffset + 4, 100);
            }

            Monitor();

            return true;
        }

        private void ReportPointer(IntPtr pointer, string name = "")
        {
            name += " pointer";
            if (pointer != IntPtr.Zero)
                WriteLine("Found " + name + " at 0x" + pointer.ToString("X"));
            else
                WriteLine("Couldn't find " + name);
        }

        private void Monitor()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (true)
            {
                if (_game == null || _game.HasExited)
                    return;

                _demoIsRecording.Update(_game);
                _demoIndex.Update(_game);
                _demoName.Update(_game);

                if ((_demoIsRecording.Changed && !_demoIsRecording.Current) || 
                    (_demoIsRecording.Current && _demoIndex.Changed && _demoIndex.Current > 1))
                {
                    string path = _demoName.Current.ToString();

                    if (_demoIndex.Changed && _demoIndex.Old > 1)
                    {
                        if (File.Exists(Path.Combine(_gameDir, $"{_demoName.Current}_{_demoIndex.Old}.dem")))
                            path += $"_{_demoIndex.Old}";
                    }

                    listdemo.DemoStopNotify(path, _gameDir);
                }

                Thread.Sleep(10);
            }
        }
    }
}
