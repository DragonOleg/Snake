using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Snake.Core
{
    public static class StringExtensions
    {
        public static bool IsBase64String(this string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }

        public static IList<string> Parse(this string data, string delimiter)
        {
            if (data == null) return null;

            if (!delimiter.EndsWith("=")) delimiter = delimiter + "=";

            if (!data.Contains(delimiter)) return null;

            var result = new List<string>();
            int start = data.IndexOf(delimiter) + 3;
            int length = data.IndexOf(',', start) - start;
            if (length == 0) return null;
            if (length > 0)
            {
                result.Add(data.Substring(start, length));

                var rec = Parse(data.Substring(start + length), delimiter);
                if (rec != null) result.AddRange(rec); //can't pass null into AddRange() :(
            }
            else
            {
                result.Add(data.Substring(start));
            }

            return result;
        }

        public static string CreateMD5(this string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return ToHex(hashBytes).ToLower();
            }
        }

        public static bool ToBool(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return Convert.ToBoolean(input);
        }

        public static byte[] ToByteArray(this string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder();

            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString();
        }

        public static byte[] FromHex(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
