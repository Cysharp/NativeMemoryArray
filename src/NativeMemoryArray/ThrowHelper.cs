using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cysharp.Collections
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
#if !NETSTANDARD2_0 && !UNITY_2019_1_OR_NEWER
        [DoesNotReturn]
#endif
        public static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if !NETSTANDARD2_0 && !UNITY_2019_1_OR_NEWER
        [DoesNotReturn]
#endif
        public static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

    }
}
