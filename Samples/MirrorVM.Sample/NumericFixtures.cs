// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.Sample
{
    public static class NumericFixtures
    {
        public static bool BooleanIdentity(bool value) { return value; }
        public static sbyte SByteIdentity(sbyte value) { return value; }
        public static byte ByteIdentity(byte value) { return value; }
        public static short Int16Identity(short value) { return value; }
        public static ushort UInt16Identity(ushort value) { return value; }
        public static char CharIdentity(char value) { return value; }
        public static int Int32Identity(int value) { return value; }
        public static uint UInt32Identity(uint value) { return value; }
        public static long Int64Identity(long value) { return value; }
        public static ulong UInt64Identity(ulong value) { return value; }
        public static IntPtr IntPtrIdentity(IntPtr value) { return value; }
        public static UIntPtr UIntPtrIdentity(UIntPtr value) { return value; }
        public static float SingleIdentity(float value) { return value; }
        public static double DoubleIdentity(double value) { return value; }
        public static decimal DecimalIdentity(decimal value) { return value; }

        public static long Int64Constant() { return 0x1020304050607080L; }
        public static float SingleConstant() { return 1.25f; }
        public static double DoubleConstant() { return 1.25; }

        public static long AddInt64(long left, long right) { return left + right; }
        public static ulong AddUInt64Checked(ulong left, ulong right) { return checked(left + right); }
        public static ulong DivideUInt64(ulong left, ulong right) { return left / right; }
        public static float AddSingle(float left, float right) { return left + right; }
        public static float SinglePrecision(float first, float second, float third)
        {
            return first + second - third;
        }
        public static double AddDouble(double left, double right) { return left + right; }

        public static IntPtr NativeAdd(IntPtr left, IntPtr right) { return AddNativeReference(left, right.ToInt32()); }
        public static IntPtr NativeAddInt32(IntPtr left, int right) { return AddNativeReference(left, right); }
        public static IntPtr Int32AddNative(int left, IntPtr right) { return AddNativeReference(right, left); }

        public static decimal DecimalAdd(decimal left, decimal right) { return left + right; }

        public static sbyte ConvertSByte(int value) { return unchecked((sbyte)value); }
        public static uint CheckedUInt32(long value) { return checked((uint)value); }
        public static ulong ConvertUInt64(uint value) { return value; }
        public static float ConvertSingle(long value) { return (float)value; }
        public static double ConvertUInt64ToDouble(ulong value) { return value; }
        public static int And(int left, int right) { return left & right; }
        public static int Or(int left, int right) { return left | right; }
        public static int Xor(int left, int right) { return left ^ right; }
        public static int Not(int value) { return ~value; }
        public static int ShiftLeft(int value, int count) { return value << count; }
        public static int ShiftRight(int value, int count) { return value >> count; }
        public static uint ShiftRightUnsigned(uint value, int count) { return value >> count; }
        public static bool Equal(int left, int right) { return left == right; }
        public static bool GreaterThan(int left, int right) { return left > right; }
        public static bool GreaterThanUnsigned(uint left, uint right) { return left > right; }
        public static bool LessThan(int left, int right) { return left < right; }
        public static bool LessThanUnsigned(uint left, uint right) { return left < right; }

        private static IntPtr AddNativeReference(IntPtr value, int offset)
        {
            return IntPtr.Size == 4
                ? new IntPtr(unchecked(value.ToInt32() + offset))
                : new IntPtr(unchecked(value.ToInt64() + offset));
        }
    }
}
