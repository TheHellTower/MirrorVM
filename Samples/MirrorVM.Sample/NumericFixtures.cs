// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.Sample
{
    public enum NumericEnum : ushort { Value = 1 }

    public struct NumericStruct { public int Value; }

    public static class NumericFixtures
    {
        public static bool BooleanIdentity(bool value) { bool local; if (value) local = true; else local = false; return local; }
        public static sbyte SByteIdentity(sbyte value) { sbyte local; if (value == 0) local = 0; else local = value; return local; }
        public static byte ByteIdentity(byte value) { byte local; if (value == 0) local = 0; else local = value; return local; }
        public static short Int16Identity(short value) { short local; if (value == 0) local = 0; else local = value; return local; }
        public static ushort UInt16Identity(ushort value) { ushort local; if (value == 0) local = 0; else local = value; return local; }
        public static char CharIdentity(char value) { char local; if (value == '\0') local = '\0'; else local = value; return local; }
        public static int Int32Identity(int value) { int local; if (value == 0) local = 0; else local = value; return local; }
        public static uint UInt32Identity(uint value) { uint local; if (value == 0U) local = 0U; else local = value; return local; }
        public static long Int64Identity(long value) { long local; if (value == 0L) local = 0L; else local = value; return local; }
        public static ulong UInt64Identity(ulong value) { ulong local; if (value == 0UL) local = 0UL; else local = value; return local; }
        public static IntPtr IntPtrIdentity(IntPtr value) { IntPtr local = value; return local; }
        public static UIntPtr UIntPtrIdentity(UIntPtr value) { UIntPtr local = value; return local; }
        public static float SingleIdentity(float value) { float local; if (value == 0f) local = value; else local = value; return local; }
        public static double DoubleIdentity(double value) { double local; if (value == 0d) local = value; else local = value; return local; }
        public static decimal DecimalIdentity(decimal value) { decimal local = value; return local; }
        public static string StringIdentity(string value) { string local; if (value == null) local = null; else local = value; return local; }
        public static NumericEnum EnumIdentity(NumericEnum value) { return value; }
        public static NumericStruct StructIdentity(NumericStruct value) { return value; }
        public static object ObjectIdentity(object value) { return value; }
        public static int[] ArrayIdentity(int[] value) { return value; }
        public static int ByRefValue(ref int value) { return value; }
        public static T GenericIdentity<T>(T value) { return value; }

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
