﻿using System;
using System.Diagnostics;

namespace Chunkyard
{
    [DebuggerStepThrough]
    public static class ValidationExtensions
    {
        public static T EnsureNotNull<T>(this T value, string paramName)
        {
            return value ?? throw new ArgumentNullException(paramName);
        }

        public static string EnsureNotNullOrEmpty(
            this string value,
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

        public static uint EnsureBetween(
            this uint value,
            uint min,
            uint max,
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