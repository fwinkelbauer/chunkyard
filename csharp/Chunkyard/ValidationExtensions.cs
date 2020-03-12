﻿using System;
using System.Diagnostics;

namespace Chunkyard
{
    [DebuggerStepThrough]
    internal static class ValidationExtensions
    {
        public static T EnsureNotNull<T>(this T value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            return value;
        }
    }
}
