using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

public static class PriorityRun
{
    private static string Quote(string arg)
    {
        if (arg == null) return "\"\"";
        if (arg.Length == 0) return "\"\"";
        bool need = false;
        foreach (char c in arg)
        {
            if (char.IsWhiteSpace(c) || c == '"') { need = true; break; }
        }
        if (!need) return arg;

        var sb = new StringBuilder();
        sb.Append('"');
        int slashes = 0;
        foreach (char c in arg)
        {
            if (c == '\\')
            {
                slashes++;
                continue;
            }
            if (c == '"')
            {
                sb.Append('\\', slashes * 2 + 1);
                sb.Append('"');
                slashes = 0;
                continue;
            }
            sb.Append('\\', slashes);
            slashes = 0;
            sb.Append(c);
        }
        sb.Append('\\', slashes * 2);
        sb.Append('"');
        return sb.ToString();
    }

    private static ProcessPriorityClass ParseClass(string value)
    {
        switch ((value ?? "").ToLowerInvariant())
        {
            case "idle": return ProcessPriorityClass.Idle;
            case "below": return ProcessPriorityClass.BelowNormal;
            case "normal": return ProcessPriorityClass.Normal;
            default: return ProcessPriorityClass.BelowNormal;
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: PriorityRun.exe idle|below|normal program [args...]");
            return 2;
        }

        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; } catch { }

        string exe = args[1];
        var command = new StringBuilder();
        for (int i = 2; i < args.Length; i++)
        {
            if (command.Length > 0) command.Append(' ');
            command.Append(Quote(args[i]));
        }

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = command.ToString(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (var p = new Process())
        {
            p.StartInfo = psi;
            if (!p.Start()) return 3;

            try { p.PriorityClass = ParseClass(args[0]); } catch { }
            try { p.PriorityBoostEnabled = false; } catch { }

            Task<string> outTask = p.StandardOutput.ReadToEndAsync();
            Task<string> errTask = p.StandardError.ReadToEndAsync();
            p.WaitForExit();
            Task.WaitAll(outTask, errTask);

            if (!String.IsNullOrEmpty(outTask.Result)) Console.Out.Write(outTask.Result);
            if (!String.IsNullOrEmpty(errTask.Result)) Console.Error.Write(errTask.Result);

            return p.ExitCode;
        }
    }
}
