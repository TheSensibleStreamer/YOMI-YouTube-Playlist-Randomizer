using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

public static class PriorityRun
{
    private const uint CREATE_SUSPENDED = 0x00000004;
    private const uint CREATE_NO_WINDOW = 0x08000000;
    private const uint STARTF_USESTDHANDLES = 0x00000100;
    private const uint HANDLE_FLAG_INHERIT = 0x00000001;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint IDLE_PRIORITY_CLASS = 0x00000040;
    private const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
    private const uint NORMAL_PRIORITY_CLASS = 0x00000020;
    private const uint INFINITE = 0xFFFFFFFF;
    private const int STD_INPUT_HANDLE = -10;

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(IntPtr hJob, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, uint cbInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcess(
        string lpApplicationName,
        StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr hThread);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessPriorityBoost(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] bool disablePriorityBoost);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private static string Quote(string arg)
    {
        if (arg == null || arg.Length == 0) return "\"\"";
        bool need = false;
        foreach (char c in arg)
        {
            if (Char.IsWhiteSpace(c) || c == '"') { need = true; break; }
        }
        if (!need) return arg;

        var sb = new StringBuilder();
        sb.Append('"');
        int slashes = 0;
        foreach (char c in arg)
        {
            if (c == '\\') { slashes++; continue; }
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

    private static uint ParseClass(string value)
    {
        switch ((value ?? "").ToLowerInvariant())
        {
            case "idle": return IDLE_PRIORITY_CLASS;
            case "below": return BELOW_NORMAL_PRIORITY_CLASS;
            case "normal": return NORMAL_PRIORITY_CLASS;
            default: return BELOW_NORMAL_PRIORITY_CLASS;
        }
    }

    private static void ThrowLastWin32(string operation)
    {
        throw new Win32Exception(Marshal.GetLastWin32Error(), operation);
    }

    private static void SafeClose(ref IntPtr handle)
    {
        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
        CloseHandle(handle);
        handle = IntPtr.Zero;
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
        command.Append(Quote(exe));
        for (int i = 2; i < args.Length; i++)
        {
            command.Append(' ');
            command.Append(Quote(args[i]));
        }

        IntPtr job = IntPtr.Zero;
        IntPtr stdoutRead = IntPtr.Zero, stdoutWrite = IntPtr.Zero;
        IntPtr stderrRead = IntPtr.Zero, stderrWrite = IntPtr.Zero;
        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
        StreamReader stdout = null, stderr = null;
        try
        {
            job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) ThrowLastWin32("CreateJobObject failed");
            var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref limits, (uint)Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION))))
                ThrowLastWin32("SetInformationJobObject failed");

            var sa = new SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)), bInheritHandle = true, lpSecurityDescriptor = IntPtr.Zero };
            if (!CreatePipe(out stdoutRead, out stdoutWrite, ref sa, 0)) ThrowLastWin32("CreatePipe stdout failed");
            if (!SetHandleInformation(stdoutRead, HANDLE_FLAG_INHERIT, 0)) ThrowLastWin32("SetHandleInformation stdout failed");
            if (!CreatePipe(out stderrRead, out stderrWrite, ref sa, 0)) ThrowLastWin32("CreatePipe stderr failed");
            if (!SetHandleInformation(stderrRead, HANDLE_FLAG_INHERIT, 0)) ThrowLastWin32("SetHandleInformation stderr failed");

            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            si.dwFlags = (int)STARTF_USESTDHANDLES;
            si.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
            si.hStdOutput = stdoutWrite;
            si.hStdError = stderrWrite;

            if (!CreateProcess(exe, command, IntPtr.Zero, IntPtr.Zero, true, CREATE_SUSPENDED | CREATE_NO_WINDOW, IntPtr.Zero, null, ref si, out pi))
                ThrowLastWin32("CreateProcess failed");

            if (!AssignProcessToJobObject(job, pi.hProcess))
            {
                TerminateProcess(pi.hProcess, 3);
                ThrowLastWin32("AssignProcessToJobObject failed");
            }
            if (!SetPriorityClass(pi.hProcess, ParseClass(args[0])))
                Console.Error.WriteLine("PriorityRun warning: SetPriorityClass failed: " + new Win32Exception(Marshal.GetLastWin32Error()).Message);
            SetProcessPriorityBoost(pi.hProcess, true);

            SafeClose(ref stdoutWrite);
            SafeClose(ref stderrWrite);
            if (ResumeThread(pi.hThread) == 0xFFFFFFFF) ThrowLastWin32("ResumeThread failed");
            SafeClose(ref pi.hThread);

            stdout = new StreamReader(new FileStream(new SafeFileHandle(stdoutRead, true), FileAccess.Read, 4096, false), Encoding.UTF8, true);
            stdoutRead = IntPtr.Zero;
            stderr = new StreamReader(new FileStream(new SafeFileHandle(stderrRead, true), FileAccess.Read, 4096, false), Encoding.UTF8, true);
            stderrRead = IntPtr.Zero;
            Task<string> outTask = stdout.ReadToEndAsync();
            Task<string> errTask = stderr.ReadToEndAsync();

            WaitForSingleObject(pi.hProcess, INFINITE);
            uint exitCode;
            if (!GetExitCodeProcess(pi.hProcess, out exitCode)) ThrowLastWin32("GetExitCodeProcess failed");
            Task.WaitAll(outTask, errTask);
            if (!String.IsNullOrEmpty(outTask.Result)) Console.Out.Write(outTask.Result);
            if (!String.IsNullOrEmpty(errTask.Result)) Console.Error.Write(errTask.Result);
            return unchecked((int)exitCode);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PriorityRun failed: " + ex.Message);
            return 3;
        }
        finally
        {
            if (stdout != null) stdout.Dispose();
            if (stderr != null) stderr.Dispose();
            SafeClose(ref stdoutRead);
            SafeClose(ref stdoutWrite);
            SafeClose(ref stderrRead);
            SafeClose(ref stderrWrite);
            SafeClose(ref pi.hThread);
            SafeClose(ref pi.hProcess);
            // Closing the job is the lifetime authority. If this wrapper is aborted by mpv,
            // Windows closes the handle and kills any still-running descendant automatically.
            SafeClose(ref job);
        }
    }
}
