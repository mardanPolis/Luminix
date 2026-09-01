
namespace Luminix.Extensions;

public static class StringExtensions
{
    public static bool HasWhitespace(this string value)
    {
        if(string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
                return true;
        }
        return false;
    }
}