using System;
using System.IO;
using System.Security.Cryptography;

namespace MyDMVpro.Services
{
    public class AesEncryption
    {
        public static string EncryptNumber(int number, byte[] key, byte[] iv)
        {
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(number);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (CryptographicException ex)
            {
                // Handle cryptography-specific exceptions
                Console.WriteLine($"Cryptographic error: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                // Handle IO-specific exceptions
                Console.WriteLine($"IO error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle all other exceptions
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        public static int DecryptNumber(string encryptedNumber, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedNumber)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            string decryptedText = srDecrypt.ReadToEnd();
                            return int.Parse(decryptedText);
                        }
                    }
                }
            }
        }
    }
}
