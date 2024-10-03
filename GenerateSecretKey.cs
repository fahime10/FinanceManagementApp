//using System;
//using System.Security.Cryptography;

//public class Program
//{
//    public static void Main()
//    {
//        var key = GenerateSecureKey(32);
//        Console.WriteLine("Generated secret key: " + key);
//    }

//    public static string GenerateSecureKey(int size)
//    {
//        using (var cryptoProvider = new RNGCryptoServiceProvider())
//        {
//            byte[] secretKey = new byte[size];
//            cryptoProvider.GetBytes(secretKey);
//            return Convert.ToBase64String(secretKey);
//        }
//    }
//}