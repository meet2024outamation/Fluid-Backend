namespace Fluid.API.Constants;

/// <summary>
/// Constants for schema field data types
/// </summary>
public static class SchemaFieldDataTypes
{
    public const string String = "string";
    public const string Number = "number";
    public const string DateTime = "dateTime";
    public const string Date = "date";
    public const string Boolean = "bool";

    /// <summary>
    /// Gets all supported data types
    /// </summary>
    public static readonly string[] All = { String, Number, DateTime, Date, Boolean };

    /// <summary>
    /// Checks if a data type is supported
    /// </summary>
    /// <param name="dataType">The data type to check</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsSupported(string dataType)
    {
        return All.Contains(dataType, StringComparer.OrdinalIgnoreCase);
    }
}