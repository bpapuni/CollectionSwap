using System;
using System.Security.Cryptography;
using System.Text;

namespace CollectionSwap.Helpers
{
    public class HashGenerator
    {
        public static string Create(string input)
        {
            string output = string.Empty;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                output = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 10);
            }
            return output;
        }
    }
}