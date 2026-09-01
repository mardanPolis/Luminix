using System;
using System.Text;

namespace Luminix.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendLong(this StringBuilder sb, long value, int minDigits = 1)
    {
        if (value < 0)
        {
            sb.Append('-');
            value = -value;
        }

        long temp = value;
        int digitsFound = 0;
        do { digitsFound++; temp /= 10; } while (temp > 0);

        for (int i = digitsFound; i < minDigits; i++)
        {
            sb.Append('0');
        }

        Span<char> buffer = stackalloc char[20];
        int pos = 0;

        temp = value;
        do
        {
            buffer[pos++] = (char)('0' + (temp % 10));
            temp /= 10;
        } while (temp > 0);

        for (int i = pos - 1; i >= 0; i--)
        {
            sb.Append(buffer[i]);
        }

        return sb;
    }

    public static StringBuilder AppendDouble(this StringBuilder sb, double value, int decimalPlaces = 4, int minIntDigits = 1)
    {
        if (double.IsNaN(value)) { sb.Append("NaN"); return sb; }
        if (double.IsInfinity(value)) { sb.Append("INF"); return sb; }
        if (value < 0) { sb.Append('-'); value = -value; }

        double integral = Math.Floor(value);
        sb.AppendLong((long)integral, minIntDigits);

        if (decimalPlaces > 0)
        {
            sb.Append('.');
            double remainder = value - integral;
            for (int i = 0; i < decimalPlaces; i++)
            {
                remainder *= 10;
                int digit = (int)remainder;
                sb.Append((char)('0' + digit));
                remainder -= digit;
            }
        }

        return sb;
    }
}