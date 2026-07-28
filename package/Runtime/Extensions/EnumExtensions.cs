using System;
using System.Runtime.CompilerServices;

namespace Soso.Net.Extensions
{
    public static class EnumExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToValue<TEnum>(this TEnum value)
            where TEnum : unmanaged, Enum
        {
            ulong result = Unsafe.As<TEnum, ulong>(ref value);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum ToEnum<TEnum>(this ulong value)
            where TEnum : unmanaged, Enum
        {
            TEnum result = Unsafe.As<ulong, TEnum>(ref value);
            return result;
        }
    }
}