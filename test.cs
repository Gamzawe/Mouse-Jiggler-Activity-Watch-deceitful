using System;
using System.Text;

class P {
    static void Main() {
        byte[] Key = Encoding.UTF8.GetBytes("MacroKey2025!");
        byte[] textBytes = Encoding.UTF8.GetBytes("Keep-Alive");
        byte[] result = new byte[textBytes.Length];
        for (int i = 0; i < textBytes.Length; i++) result[i] = (byte)(textBytes[i] ^ Key[i % Key.Length]);
        Console.WriteLine(Convert.ToBase64String(result));
    }
}
