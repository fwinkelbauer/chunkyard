using System;
using System.Diagnostics;

namespace Chunkyard.Core
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
    }
}
