using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

public static class YomiRestartRelay
{
    private static string _logPath = "";

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath,
                DateTime.Now.ToString("o") + " | relay-exe | " + message + Environment.NewLine);
        }
        catch { }
    }

    private static bool ControllerRunning(string installRoot)
    {
        try
        {
            string target = Path.GetFullPath(Path.Combine(installRoot, @"app\YomiControllerWpf.exe"));
            foreach (Process p in Process.GetProcessesByName("YomiControllerWpf"))
            {
                try
                {
                    if (String.Equals(Path.GetFullPath(p.MainModule.FileName), target,
                        StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
        }
        catch { }
        return false;
    }

    private static bool ShellOpen(string file, string arguments, string workingDirectory)
    {
        object shell = null;
        try
        {
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return false;
            shell = Activator.CreateInstance(shellType);
            shellType.InvokeMember("ShellExecute",
                BindingFlags.InvokeMethod, null, shell,
                new object[] { file, arguments ?? "", workingDirectory ?? "", "open", 1 });
            return true;
        }
        catch (Exception ex)
        {
            Log("Explorer-shell launch failed: " + ex.Message);
            return false;
        }
        finally
        {
            if (shell != null)
            {
                try { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); } catch { }
            }
        }
    }

    private static void DirectOpen(string file, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = file;
        psi.Arguments = arguments ?? "";
        psi.WorkingDirectory = workingDirectory;
        psi.UseShellExecute = true;
        psi.WindowStyle = ProcessWindowStyle.Normal;
        Process.Start(psi);
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 3) return 2;
            int parentPid;
            if (!Int32.TryParse(args[0], out parentPid)) return 2;
            string installRoot = args[1];
            _logPath = args[2];

            Log("started; waiting for parent pid " + parentPid);
            for (int i = 0; i < 600; i++)
            {
                try
                {
                    using (Process parent = Process.GetProcessById(parentPid))
                    {
                        if (parent.HasExited) break;
                    }
                }
                catch { break; }
                Thread.Sleep(50);
            }

            if (ControllerRunning(installRoot))
            {
                Log("controller already running after parent exit");
                return 0;
            }

            string appDir = Path.Combine(installRoot, "app");
            string controller = Path.Combine(appDir, "YomiControllerWpf.exe");
            string launcher = Path.Combine(appDir, "YomiLauncher.exe");

            if (File.Exists(controller))
            {
                Log("launching controller through Explorer shell");
                if (!ShellOpen(controller, "", appDir))
                {
                    Log("Explorer shell unavailable; using normal shell launch");
                    DirectOpen(controller, "", appDir);
                }
                Thread.Sleep(1200);
            }

            if (!ControllerRunning(installRoot) && File.Exists(launcher))
            {
                Log("controller did not stay up; launcher fallback");
                if (!ShellOpen(launcher, "controller", appDir))
                    DirectOpen(launcher, "controller", appDir);
                Thread.Sleep(1600);
            }

            if (ControllerRunning(installRoot))
            {
                Log("restart verified");
                return 0;
            }

            Log("restart failed");
            return 1;
        }
        catch (Exception ex)
        {
            Log("fatal: " + ex);
            return 1;
        }
    }
}
