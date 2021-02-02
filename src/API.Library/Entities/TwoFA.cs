using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace API
{
    /// <summary>
    /// Two-Factor Authentication
    /// </summary>
    public static class TwoFA
    {
        const long unixEpochTicks = 621355968000000000L;

        const long ticksToSeconds = 10000000L;

        private const int step = 30;

        private const int totpSize = 6;

        private static byte[] key;

        /// <summary>
        /// Generates a random 16 digit string that will be used as the 
        /// shared secret between the user and the application.
        /// The Get2faCode and the phone app will always generate the same 6 digit code for this secret
        /// </summary>
        /// <returns></returns>
        public static string GenerateSharedSecret(int size = 16)
        {
            List<char> cList = new List<char>();
            for (int i = 50; i <= 55; i++)
            {
                cList.Add((char)i);
            }

            for (int i = 65; i <= 90; i++)
            {
                cList.Add((char)i);
            }

            for (int i = 97; i <= 122; i++)
            {
                cList.Add((char)i);
            }
            char[] chars = cList.ToArray();
            byte[] data = new byte[4 * size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }




        /// <summary>
        /// Get a 6 digit code corresponding to the shared secret code plus the time of day
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <returns></returns>
        private static string Get2faCode(string sharedSecret)
        {
            key = ToBytes(sharedSecret);

            return ComputeTotp();

        }

        /// <summary>
        /// Validates the one-time password against the shared secret
        /// </summary>
        /// <param name="Otp"></param>
        /// <param name="sharedSecret"></param>
        /// <returns></returns>
        public static bool Validate2fa(string Otp, string sharedSecret)
        {
            return Get2faCode(sharedSecret) == Otp;
        }

        private static string ComputeTotp()
        {
            var window = CalculateTimeStepFromTimestamp(DateTime.UtcNow);

            var data = GetBigEndianBytes(window);

            var hmac = new HMACSHA1();
            hmac.Key = key;
            var hmacComputedHash = hmac.ComputeHash(data);

            int offset = hmacComputedHash[hmacComputedHash.Length - 1] & 0x0F;
            var otp = (hmacComputedHash[offset] & 0x7f) << 24
                   | (hmacComputedHash[offset + 1] & 0xff) << 16
                   | (hmacComputedHash[offset + 2] & 0xff) << 8
                   | (hmacComputedHash[offset + 3] & 0xff) % 1000000;

            var result = Digits(otp, totpSize);

            return result;
        }

        private static int RemainingSeconds()
        {
            return step - (int)(((DateTime.UtcNow.Ticks - unixEpochTicks) / ticksToSeconds) % step);
        }

        private static byte[] GetBigEndianBytes(long input)
        {
            // Since .net uses little endian numbers, we need to reverse the byte order to get big endian.
            var data = BitConverter.GetBytes(input);
            Array.Reverse(data);
            return data;
        }

        private static long CalculateTimeStepFromTimestamp(DateTime timestamp)
        {
            var unixTimestamp = (timestamp.Ticks - unixEpochTicks) / ticksToSeconds;
            var window = unixTimestamp / (long)step;
            return window;
        }

        private static string Digits(long input, int digitCount)
        {
            var truncatedValue = ((int)input % (int)Math.Pow(10, digitCount));
            return truncatedValue.ToString().PadLeft(digitCount, '0');
        }

        private static byte[] ToBytes(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException("input");
            }

            input = input.TrimEnd('='); //remove padding characters
            int byteCount = input.Length * 5 / 8; //this must be TRUNCATED
            byte[] returnArray = new byte[byteCount];

            byte curByte = 0, bitsRemaining = 8;
            int mask = 0, arrayIndex = 0;

            foreach (char c in input)
            {
                int cValue = CharToValue(c);

                if (bitsRemaining > 5)
                {
                    mask = cValue << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = cValue >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(cValue << (3 + bitsRemaining));
                    bitsRemaining += 3;
                }
            }

            //if we didn't end with a full byte
            if (arrayIndex != byteCount)
            {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }

        private static string ToString(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                throw new ArgumentNullException("input");
            }

            int charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
            char[] returnArray = new char[charCount];

            byte nextChar = 0, bitsRemaining = 5;
            int arrayIndex = 0;

            foreach (byte b in input)
            {
                nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                returnArray[arrayIndex++] = ValueToChar(nextChar);

                if (bitsRemaining < 4)
                {
                    nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                    returnArray[arrayIndex++] = ValueToChar(nextChar);
                    bitsRemaining += 5;
                }

                bitsRemaining -= 3;
                nextChar = (byte)((b << bitsRemaining) & 31);
            }

            //if we didn't end with a full char
            if (arrayIndex != charCount)
            {
                returnArray[arrayIndex++] = ValueToChar(nextChar);
                while (arrayIndex != charCount) returnArray[arrayIndex++] = '='; //padding
            }

            return new string(returnArray);
        }

        private static int CharToValue(char c)
        {
            int value = (int)c;

            //65-90 == uppercase letters
            if (value < 91 && value > 64)
            {
                return value - 65;
            }
            //50-55 == numbers 2-7
            if (value < 56 && value > 49)
            {
                return value - 24;
            }
            //97-122 == lowercase letters
            if (value < 123 && value > 96)
            {
                return value - 97;
            }

            throw new ArgumentException("Character is not a Base32 character.", "c");
        }

        private static char ValueToChar(byte b)
        {
            if (b < 26)
            {
                return (char)(b + 65);
            }

            if (b < 32)
            {
                return (char)(b + 24);
            }

            throw new ArgumentException("Byte is not a value Base32 value.", "b");
        }
    }

}
