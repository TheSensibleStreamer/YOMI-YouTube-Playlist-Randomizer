using System;
using System.Diagnostics;
using System.IO;

public static class YomiLauncher
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string mode = (args.Length > 0 ? args[0] : "controller").ToLowerInvariant();
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string scriptName = mode == "settings" ? "settings.ps1" : "controller.ps1";
            string script = Path.Combine(appDir, scriptName);

            if (!File.Exists(script))
                return 2;

            string powershell = Path.Combine(
                Environment.SystemDirectory,
                @"WindowsPowerShell\v1.0\powershell.exe"
            );

            var psi = new ProcessStartInfo();
            psi.FileName = powershell;
            psi.Arguments =
                "-NoProfile -ExecutionPolicy Bypass -STA -File \"" +
                script.Replace("\"", "\\\"") + "\"";
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
