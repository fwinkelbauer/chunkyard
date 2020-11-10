using System;
using System.Diagnostics;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to validate parameters.
    /// </summary>
    [DebuggerStepThrough]
    public static class ValidationExtensions
    {
        public static T EnsureNotNull<T>(this T value, string paramName)
        {
            return value ?? throw new ArgumentNullException(paramName);
        }

        public static string EnsureNotNullOrEmpty(
            this string? value,
            string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(paramName);
            }

            return value;
        }

        public static int EnsureBetween(
            this int value,
            int min,
            int max,
            string paramName)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    $"Value must be between {min} and {max}");
            }

            return value;
        }
    }
}
