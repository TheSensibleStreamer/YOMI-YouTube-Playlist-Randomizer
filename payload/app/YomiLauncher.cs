using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public static class YomiLauncher
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string mode = (args.Length > 0 ? args[0] : "controller").ToLowerInvariant();
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            bool updateMode = mode == "update" || mode == "update-auto";
            string scriptName = updateMode ? "update.ps1" : (mode == "settings" ? "settings.ps1" : "controller.ps1");
            string script = Path.Combine(appDir, scriptName);

            if (!File.Exists(script))
                return 2;

            // Settings is a single working surface. A second click should
            // foreground its existing window, not create a stale editor.
            if (mode == "settings")
            {
                try
                {
                    using (EventWaitHandle activate = EventWaitHandle.OpenExisting("Local\\YOMI_SETTINGS_ACTIVATE_V4"))
                    {
                        activate.Set();
                        return 0;
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Settings is not running yet; launch the first instance below.
                }
            }

            string powershell = Path.Combine(
                Environment.SystemDirectory,
                @"WindowsPowerShell\v1.0\powershell.exe"
            );

            var psi = new ProcessStartInfo();
            psi.FileName = powershell;
            psi.Arguments =
                "-NoProfile -ExecutionPolicy Bypass -STA -File \"" +
                script.Replace("\"", "\\\"") + "\"" +
                (mode == "update" ? " -Manual" : "");
            psi.WorkingDirectory = appDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Normal;

            Process.Start(psi);
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
