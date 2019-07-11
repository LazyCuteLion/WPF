using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class StringExtension
    {
        public static byte[] ToBytes(this string s, Encoding e = null)
        {
            if (e == null)
                e = Encoding.UTF8;
            return e.GetBytes(s);
        }

        public static bool IsHexadecimal(this string s)
        {
            return Regex.IsMatch(s, "[0-9A-Fa-f]") && s.Length % 2 == 0;
        }

        public static byte[] ToHexadecimal(this string s, string split = "")
        {
            if (split.Length > 0)
                s = s.Replace(split, "");
            var data = new byte[s.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            }
            return data;
        }

        public static string ToMD5String(this string s, string split = "")
        {
            return s.ToBytes().ToMD5String(split);
        }

        public static string ToMD5String(this byte[] data, string split = "")
        {
            var md5 = new MD5CryptoServiceProvider();
            var temp = md5.ComputeHash(data);
            md5.Dispose();
            var sb = new StringBuilder();
            foreach (var item in temp)
            {
                sb.AppendFormat("{0}{1:X2}", split, item);
            }
            if (split.Length > 0)
            {
                sb.Remove(0, split.Length);
            }
            return sb.ToString();
        }

        public static string ToDESString(this string s, string key )
        {
            if (key.Length < 8)
                throw new ArgumentException("参数[key]长度必须大于8", "key");

            var dKey = Encoding.Default.GetBytes(key.Substring(0, 8));
            var dIV = Encoding.Default.GetBytes("10100100");
            var des = new DESCryptoServiceProvider();
            var stream = new MemoryStream();
            using (var cs = new CryptoStream(stream, des.CreateEncryptor(dKey, dIV), CryptoStreamMode.Write))
            {
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(s);
                }
            }
            var data = stream.ToArray();
            des.Dispose();
            stream.Close();
            return Convert.ToBase64String(data);
        }

        public static string FromDESString(this string s, string key )
        {
            if (key.Length < 8)
                throw new ArgumentException("参数[key]长度必须大于8", "key");

            var dKey = Encoding.Default.GetBytes(key.Substring(0, 8));
            var dIV = Encoding.Default.GetBytes("10100100");
            var data = Convert.FromBase64String(s);
            var des = new DESCryptoServiceProvider();
            var stream = new MemoryStream(data);
            var r = "";
            using (var cs = new CryptoStream(stream, des.CreateDecryptor(dKey, dIV), CryptoStreamMode.Read))
            {
                using (var sr = new StreamReader(cs))
                {
                    r = sr.ReadToEnd();
                }
            }
            des.Dispose();
            stream.Close();
            return r;
        }
    }