using System;
using System.Collections.Generic;
using System.Diagnostics;


using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

using System.Text;
using Newtonsoft.Json;

namespace AoWSMLauncher
{
    class AppConfig
    {

        public static AppConfig LoadPrices(string filename)
        {
            TextReader reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            reader.Close();

            AppConfig config = JsonConvert.DeserializeObject<AppConfig>(json);

            return config;
        }

        public string MainAppParam { get; set; }

        public string MainAppExe { get; set; }
        public string CompatAppParam { get; set; }

        public string CompatAppExe { get; set; }

        public string WindowName { get; set; }

    }
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] 
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetWindowTitle(IntPtr hWnd)
        {
            var length = GetWindowTextLength(hWnd) + 1;
            var title = new StringBuilder(length);
            GetWindowText(hWnd, title, length);
            return title.ToString();
        }

        static void Main(string[] args)
        {
            AppConfig gameConfig;


            if (File.Exists("AoW.exe") && File.Exists("AoWCompat.exe"))
            {
                gameConfig = AppConfig.LoadPrices("settings - Age of Wonders.json");
            }
            else {
                if (File.Exists("AoW2.exe") && File.Exists("AoW2Compat.exe"))
                {
                    gameConfig = AppConfig.LoadPrices("settings - Age of Wonders 2.json");
                }
                else {
                    if (File.Exists("AoWSM.exe") && File.Exists("AoWSMCompat.exe"))
                    {
                        gameConfig = AppConfig.LoadPrices("settings - Age of Wonders Shadow Magic.json");
                    }
                    else {
                        gameConfig = AppConfig.LoadPrices("settings.json");
                    }
                }
            }

            

            

            Console.WriteLine("Game Config: "+ gameConfig.MainAppParam+" " +gameConfig.MainAppExe + " " +gameConfig.CompatAppParam + " "+ gameConfig.CompatAppExe+ " "+ gameConfig.WindowName);
            string launchApp="", windowName= "Age of Wonders - Shadow Magic";
            if ((gameConfig.WindowName != null) && (gameConfig.WindowName != ""))
            {
                windowName = gameConfig.WindowName;
            }
            bool showConsole = false, normalLaunch=false;
            int finalExitCode = 1;
            int xsize = 1280, ysize = 800, changeSizeDelay=1;
            Console.WriteLine("parameters:");
            foreach (string param in args) {
                Console.WriteLine(param);
            }

            if (args.Length > 0)
            {
                launchApp = args[0];
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-showconsole":
                            showConsole = true;
                            break;
                        case "-normallaunch":
                            normalLaunch = true;
                            break;
                        case "-xsize":
                            if ((i + 1) < args.Length)
                            {
                                int numericValue;
                                if (int.TryParse(args[i + 1], out numericValue))
                                {
                                    if (numericValue >= 800)
                                    {
                                        xsize = numericValue;
                                    }
                                }
                            }
                            break;
                        case "-ysize":
                            if ((i + 1) < args.Length)
                            {
                                int numericValue;
                                if (int.TryParse(args[i + 1], out numericValue))
                                {
                                    if (numericValue >= 600)
                                    {
                                        ysize = numericValue;
                                    }

                                }
                            }
                            break;
                        case "-changesizedelay":
                            if ((i + 1) < args.Length)
                            {
                                int numericValue;
                                if (int.TryParse(args[i + 1], out numericValue))
                                {
                                    changeSizeDelay = numericValue;
                                }
                            }
                            break;
                        case "-windowname":
                            if ((i + 1) < args.Length)
                            {
                                windowName = args[i + 1].Replace("\"", "");
                            }
                            break;
                    }
                }
            } else {
                System.Environment.Exit(finalExitCode);
            }
            

            Console.WriteLine("options selected:  showConsole: " + showConsole+ " normalLaunch: "+ normalLaunch+ " xsize: "+ xsize+ " ysize: "+ ysize+ " changeSizeDelay: "+ changeSizeDelay+ " windowName: "+ windowName);

            string executable;
            ProcessStartInfo start = new ProcessStartInfo();

            if ((gameConfig.MainAppParam != null && gameConfig.MainAppParam != "") && (gameConfig.MainAppExe != null && gameConfig.MainAppExe != "") &&
                (gameConfig.CompatAppParam != null && gameConfig.CompatAppParam != "") && (gameConfig.CompatAppExe != null && gameConfig.CompatAppExe != ""))
            {
                if ((launchApp == gameConfig.MainAppParam) || (launchApp == gameConfig.CompatAppParam))
                {
                    Console.WriteLine("using json settings");
                    if (launchApp == gameConfig.MainAppParam)
                    {
                        executable = gameConfig.MainAppExe;
                    }
                    else
                    {
                        executable = gameConfig.CompatAppExe;
                    }
                    // Enter in the command line arguments, everything you would enter after the executable name itself
                    start.Arguments = "";
                    // Enter the executable to run, including the complete path
                    start.FileName = executable;
                    //this option doesnt work, dont know why, but i dont think its important to fix it
                    if (!showConsole)
                    {
                        start.WindowStyle = ProcessWindowStyle.Hidden;
                        start.CreateNoWindow = true;
                    }

                    // Run the external process & wait for it to finish
                    using (Process proc = Process.Start(start))
                    {
                        
                        if (!normalLaunch)
                        {
                            string windowtitle;
                            bool found = false;
                            for (int i = 0; i < 30; i++)
                            {
                                Thread.Sleep(1000);
                                foreach (var handle in EnumerateProcessWindowHandles(proc.Id))
                                {
                                    //   Thread.Sleep(5000);
                                    windowtitle = GetWindowTitle(handle);
                                    Console.WriteLine("handle:" + handle);
                                    Console.WriteLine("window title:" + windowtitle);
                                    if (windowtitle == windowName)
                                    {
                                        Console.WriteLine("window found!");
                                        Thread.Sleep(changeSizeDelay * 1000);
                                        MoveWindow(handle, 0, 0, xsize, ysize, false);
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                {
                                    break;
                                }
                            }
                        }

                        proc.WaitForExit();

                        // Retrieve the app's exit code
                        finalExitCode = proc.ExitCode;
                        Console.WriteLine("exit code: " + finalExitCode);

                    }
                }
                else
                {
                    Console.WriteLine("invalid application to launch: " + launchApp);
                    Thread.Sleep(10000);
                }
            }
            else 
            {
                switch (launchApp)
                {
                    case "AoWSMCompat":
                    case "AoWSM":
                        executable = launchApp + ".exe";
                        // Enter in the command line arguments, everything you would enter after the executable name itself
                        start.Arguments = "";
                        // Enter the executable to run, including the complete path
                        start.FileName = executable;
                        //this option doesnt work, dont know why, but i dont think its important to fix it
                        if (!showConsole)
                        {
                            start.WindowStyle = ProcessWindowStyle.Hidden;
                            start.CreateNoWindow = true;
                        }

                        // Run the external process & wait for it to finish
                        using (Process proc = Process.Start(start))
                        {
                            
                            if (!normalLaunch)
                            {
                                string windowtitle;
                                bool found = false;
                                for (int i = 0; i < 30; i++)
                                {
                                    Thread.Sleep(1000);
                                    foreach (var handle in EnumerateProcessWindowHandles(proc.Id))
                                    {
                                        //   Thread.Sleep(5000);
                                        windowtitle = GetWindowTitle(handle);
                                        Console.WriteLine("handle:" + handle);
                                        Console.WriteLine("window title:" + windowtitle);
                                        if (windowtitle == windowName)
                                        {
                                            Console.WriteLine("window found!");
                                            Thread.Sleep(changeSizeDelay * 1000);
                                            MoveWindow(handle, 0, 0, xsize, ysize, false);
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                    {
                                        break;
                                    }
                                }
                            }

                            proc.WaitForExit();

                            // Retrieve the app's exit code
                            finalExitCode = proc.ExitCode;
                            Console.WriteLine("exit code: " + finalExitCode);

                        }
                        break;
                    //this code is unnessesary, the Launcher.exe is not used for these apps
                    /*case "AoWSMEd":
                    case "AoWSMSetup":
                        executable = launchApp + ".exe";

                        // Enter in the command line arguments, everything you would enter after the executable name itself
                        start.Arguments = "";
                        // Enter the executable to run, including the complete path
                        start.FileName = executable;
                        //this option doesnt work, dont know why, but i dont think its important to fix it
                        if (!showConsole)
                        {
                            start.WindowStyle = ProcessWindowStyle.Hidden;
                            start.CreateNoWindow = true;
                        }
                        // Run the external process & wait for it to finish
                        using (Process proc = Process.Start(start))
                        {
                            proc.WaitForExit();

                            // Retrieve the app's exit code
                            finalExitCode = proc.ExitCode;
                            Console.WriteLine("exit code: " + finalExitCode);
                        }

                        break;
                        */
                    default:
                        Console.WriteLine("invalid application to launch: " + launchApp);
                        Thread.Sleep(10000);
                        break;
                }

            }

            


            System.Environment.Exit(finalExitCode);
        }
    }
}
