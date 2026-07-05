using System;
using System.Text;

namespace LogiOptions
{
    public static class StringObfuscator
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("MacroKey2025!");

        public static string Encrypt(string plainText)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(XorBytes(textBytes));
        }

        public static string Decrypt(string cipherText)
        {
            byte[] textBytes = Convert.FromBase64String(cipherText);
            return Encoding.UTF8.GetString(XorBytes(textBytes));
        }

        public static byte[] XorBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ Key[i % Key.Length]);
            }
            return result;
        }
    }
}
