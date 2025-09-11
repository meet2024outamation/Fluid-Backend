using SharedKernel.Models;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SharedKernel
{
    public static class Extensions
    {
        #region string
        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        {
            return String.IsNullOrWhiteSpace(value);
        }
        public static bool IsNullOrEmpty(this string? value)
        {
            return String.IsNullOrEmpty(value);
        }
        public static string? ToTitleCase(this string? str)
        {
            return str.IsNullOrWhiteSpace() ? str : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
        public static T? ConvertTo<T>(this string value)
        {
            T convertedValue = (T)value.ConvertTo(typeof(T));
            return convertedValue == null ? default : convertedValue;
        }
        public static object? ConvertTo(this string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.CanConvertFrom(value.GetType()) ?
                converter.ConvertFrom(value) : null;
        }
        public static object ConvertToList(this string data, string delimiter, Type listType)
        {
            var splittedValues = data
                .Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            return splittedValues.ToStronglyTypedList(listType);
        }

        public static object ToStronglyTypedList(this IEnumerable<object> data, Type listType)
        {
            if (!typeof(IList).IsAssignableFrom(listType))
            {
                //"Not a list type"
                return null;
            }

            if (!listType.IsGenericType)
            {
                //"Not a generic type"
                return null;
            }

            if (listType.GenericTypeArguments.Length != 1)
            {
                //"Too many or too few type arguments"
                return null;
            }

            //var constructedListType = listType.MakeGenericType(listType.GenericTypeArguments[0]);
            var instance = Activator.CreateInstance(listType);
            var list = (IList)instance;

            var convertedList = data
                .Select(v => v.ConvertTo(listType.GenericTypeArguments[0]));

            foreach (var convertedValue in convertedList)
            {
                list.Add(convertedValue);
            }

            return list;
        }

        public static object ConvertTo(this object value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.CanConvertFrom(value.GetType()) ? converter.ConvertFrom(value) : null;
        }

        public static string ConvertToLowerCaseWithHyphens(string input)
        {
            StringBuilder result = new StringBuilder();

            if (!input.Contains("-"))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char currentChar = input[i];

                    // Insert a hyphen before each uppercase letter (except the first one)
                    if (char.IsUpper(currentChar))
                    {
                        if (i > 0)
                        {
                            result.Append('-');
                        }

                        // Convert the uppercase letter to lowercase
                        result.Append(char.ToLower(currentChar));
                    }
                    else
                    {
                        result.Append(currentChar);
                    }
                }
            }
            else
            {
                result.Append(input.ToLower());
            }

            return result.ToString();
        }

        public static string ToUtf8FromBase64(this string? encodedString)
        {
            if (encodedString.IsNullOrEmpty()) return string.Empty;
            return Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
        }

        public static string FromPlainTextToBase64(string plainString)
        {
            if (plainString.IsNullOrEmpty()) return string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainString));
        }

        #endregion string


        #region Datetime
        public static DateTime TruncateToSeconds(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
        }
        public static string FormatDateTime(DateTime dateTime, string format)
        {
            return dateTime.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture);
        }
        #endregion Datetime

        #region Hashing
        public static string ToHex(byte[] data, bool lowercase)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                stringBuilder.Append(data[i].ToString(lowercase ? "x2" : "X2", CultureInfo.InvariantCulture));
            }

            return stringBuilder.ToString();
        }

        public static byte[] ComposeSigningKey(string secretAccesskey, string region, string date, string service, byte[] terminatorBytes)
        {
            char[]? array = null;
            try
            {
                array = secretAccesskey.ToCharArray();
                byte[] key = ComputeKeyedHash(SigningAlgorithm.HmacSHA256, Encoding.UTF8.GetBytes(array), Encoding.UTF8.GetBytes(date));
                byte[] key2 = ComputeKeyedHash(SigningAlgorithm.HmacSHA256, key, Encoding.UTF8.GetBytes(region));
                byte[] key3 = ComputeKeyedHash(SigningAlgorithm.HmacSHA256, key2, Encoding.UTF8.GetBytes(service));
                return ComputeKeyedHash(SigningAlgorithm.HmacSHA256, key3, terminatorBytes);
            }
            finally
            {
                if (array != null)
                {
                    Array.Clear(array, 0, array.Length);
                }
            }
        }

        public static byte[] ComputeKeyedHash(SigningAlgorithm algorithm, byte[] key, byte[] data)
        {
            return HMACSignBinary(data, key, algorithm);
        }

        public static byte[] ComputeHash(string data)
        {
            KeyedHashAlgorithm keyedHashAlgorithm = CreateKeyedHashAlgorithm(SigningAlgorithm.HmacSHA256);
            byte[] bytesData = Encoding.UTF8.GetBytes(data);
            return keyedHashAlgorithm.ComputeHash(bytesData);
        }


        public static byte[] HMACSignBinary(byte[] data, byte[] key, SigningAlgorithm algorithmName)
        {
            if (key == null || key.Length == 0)
            {
                throw new ArgumentNullException("key", "Please specify a Secret Signing Key.");
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentNullException(nameof(data), "Please specify data to sign.");
            }

            KeyedHashAlgorithm keyedHashAlgorithm = CreateKeyedHashAlgorithm(algorithmName);
            if (keyedHashAlgorithm == null)
            {
                throw new InvalidOperationException("Please specify a KeyedHashAlgorithm to use.");
            }

            try
            {
                keyedHashAlgorithm.Key = key;
                return keyedHashAlgorithm.ComputeHash(data);
            }
            finally
            {
                keyedHashAlgorithm.Dispose();
            }
        }

        public static KeyedHashAlgorithm CreateKeyedHashAlgorithm(SigningAlgorithm algorithmName)
        {
            return algorithmName switch
            {
                SigningAlgorithm.HmacSHA256 => new HMACSHA256(),
                SigningAlgorithm.HmacSHA1 => new HMACSHA1(),
                _ => throw new Exception($"KeyedHashAlgorithm {algorithmName} was not found."),
            };
        }

        #endregion Hashing
    }
}
