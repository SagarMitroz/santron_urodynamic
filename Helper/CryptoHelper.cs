using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SantronWinApp.Helper
{
    public static class CryptoHelper
    {
        private static readonly string keyString = "MySuperSecretKey123";
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("MyInitVector12345".PadRight(16).Substring(0, 16));

        public static byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] cipherData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (MemoryStream ms = new MemoryStream(cipherData))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }

}
