using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedKernel.Utility;

public static class HelperExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrEmpty(value);
    }


    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static string IfNullOrWhiteSpace(this string value, object defaultValue)
    {
        return value.IsNullOrWhiteSpace() ? defaultValue.ToString() : value;
    }

    public static dynamic ToDynamic<T>(this T obj) where T : class
    {
        IDictionary<string, object?> expando = new ExpandoObject();

        var objType = typeof(T);

        // If generic type is not specified, try to infer it from object's type
        if (objType == typeof(object)) objType = obj.GetType();

        foreach (var property in objType.GetProperties())
            expando.Add(property.Name, property.GetValue(obj));

        return (ExpandoObject)expando;
    }

    public static DateTime TruncateToSeconds(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
    }
    public static T? ConvertTo<T>(this string value)
    {
        var convertedValue = (T?)value.ConvertTo(typeof(T));
        return convertedValue == null ? default : convertedValue;
    }
    public static object? ConvertTo(this string value, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(value);
        else
            throw new NotSupportedException($"Cannot convert from {typeof(string)} to {type}.");
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
            throw new NotSupportedException($"{listType} is not a list type.");
        }

        if (!listType.IsGenericType)
        {
            //"Not a generic type"
            throw new NotSupportedException($"{listType} is not a generic type.");
        }

        if (listType.GenericTypeArguments.Length != 1)
        {
            //"Too many or too few type arguments"
            throw new NotSupportedException($"{listType} must have exactly one type argument.");
        }

        //var constructedListType = listType.MakeGenericType(listType.GenericTypeArguments[0]);
        var instance = Activator.CreateInstance(listType);

        if (instance == null) throw new NotSupportedException($"Cannot instantiate {listType}");

        var list = (IList)instance;

        var convertedList = data
          .Select(v => v.ConvertTo(listType.GenericTypeArguments[0]));

        foreach (var convertedValue in convertedList)
        {
            list.Add(convertedValue);
        }

        return list;
    }

    public static object? ConvertTo(this object value, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(value);
        else
            throw new NotSupportedException($"Cannot convert from {value.GetType()} to {type}.");
    }

    private static IEnumerable<char> ReadNext(string str, int currentPosition, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (currentPosition + i >= str.Length)
            {
                yield break;
            }
            else
            {
                yield return str[currentPosition + i];
            }
        }
    }

    public static IEnumerable<string> QuotedSplit(this string s, string delim)
    {
        const char quote = '\'';

        var sb = new StringBuilder(s.Length);
        var counter = 0;
        while (counter < s.Length)
        {
            // if starts with delmiter if so read ahead to see if matches
            if (delim[0] == s[counter] &&
                delim.SequenceEqual(ReadNext(s, counter, delim.Length)))
            {
                yield return sb.ToString();
                sb.Clear();
                counter = counter + delim.Length; // Move the counter past the delimiter 
            }
            // if we hit a quote read until we hit another quote or end of string
            else if (s[counter] == quote)
            {
                sb.Append(s[counter++]);
                while (counter < s.Length && s[counter] != quote)
                {
                    sb.Append(s[counter++]);
                }
                // if not end of string then we hit a quote add the quote
                if (counter < s.Length)
                {
                    sb.Append(s[counter++]);
                }
            }
            else
            {
                sb.Append(s[counter++]);
            }
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }

    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                var domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static IEnumerable<LinkedListNode<T>> GetNodes<T>(this LinkedList<T> list)
    {
        for (var node = list.First; node != null; node = node.Next)
            yield return node;
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? list)
    {
        if (list == null)
        {
            return true;
        }

        if (!list.Any())
        {
            return true;
        }

        return false;
    }

    public static bool HasDuplicates<T, TProp>(this IEnumerable<T> list, Func<T, TProp> selector)
    {
        HashSet<TProp> hashSet = new HashSet<TProp>();
        foreach (var item in list)
        {
            if (!hashSet.Add(selector(item)))
            {
                return true;
            }
        }

        return false;
    }
}
