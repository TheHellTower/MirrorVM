// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM
{
    internal enum ArithmeticOperation
    {
        Add,
        AddChecked,
        AddCheckedUnsigned,
        Subtract,
        SubtractChecked,
        SubtractCheckedUnsigned,
        Multiply,
        MultiplyChecked,
        MultiplyCheckedUnsigned,
        Divide,
        DivideUnsigned,
        Remainder,
        RemainderUnsigned
    }

    internal enum NumericKind
    {
        Int32,
        Int64,
        NativeInt,
        Single,
        Double
    }

    internal static class NumericArithmetic
    {
        public static object NormalizeStackValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value.GetType().IsEnum)
            {
                return NormalizeNumber(value);
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return NormalizeNumber(value);
                default:
                    if (value is UIntPtr)
                    {
                        return ToNativeInt((UIntPtr)value);
                    }

                    return value;
            }
        }

        // Small integers share I4 and unsigned integers keep their bits. The
        // selected opcode, rather than the original boxed type, sets signedness.
        public static object NormalizeNumber(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.GetType().IsEnum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    return (bool)value ? 1 : 0;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char:
                    return Convert.ToInt32(value);
                case TypeCode.Int32:
                    return value;
                case TypeCode.UInt32:
                    return unchecked((int)(uint)value);
                case TypeCode.Int64:
                    return value;
                case TypeCode.UInt64:
                    return unchecked((long)(ulong)value);
                case TypeCode.Single:
                case TypeCode.Double:
                    return value;
                default:
                    if (value is IntPtr)
                    {
                        return value;
                    }

                    if (value is UIntPtr)
                    {
                        return ToNativeInt((UIntPtr)value);
                    }

                    throw new ArgumentException("The value is not a supported numeric value kind.", "value");
            }
        }

        public static object Apply(ArithmeticOperation operation, object left, object right)
        {
            object normalizedLeft = NormalizeNumber(left);
            object normalizedRight = NormalizeNumber(right);
            NumericKind leftKind = GetKind(normalizedLeft);
            NumericKind rightKind = GetKind(normalizedRight);

            // The CLI uses the native width for mixed I4/native-int arithmetic.
            if (leftKind != rightKind)
            {
                if (leftKind == NumericKind.NativeInt && rightKind == NumericKind.Int32)
                {
                    normalizedRight = ConvertInt32ToNative(
                        (int)normalizedRight, ZeroExtendInt32ForNative(operation));
                    rightKind = NumericKind.NativeInt;
                }
                else if (leftKind == NumericKind.Int32 && rightKind == NumericKind.NativeInt)
                {
                    normalizedLeft = ConvertInt32ToNative(
                        (int)normalizedLeft, ZeroExtendInt32ForNative(operation));
                    leftKind = NumericKind.NativeInt;
                }
                else if ((leftKind == NumericKind.Single || leftKind == NumericKind.Double) &&
                    (rightKind == NumericKind.Single || rightKind == NumericKind.Double))
                {
                    normalizedLeft = leftKind == NumericKind.Single
                        ? (double)(float)normalizedLeft
                        : (double)normalizedLeft;
                    normalizedRight = rightKind == NumericKind.Single
                        ? (double)(float)normalizedRight
                        : (double)normalizedRight;
                    leftKind = NumericKind.Double;
                    rightKind = NumericKind.Double;
                }
                else
                {
                    throw new InvalidOperationException("Arithmetic operands must use compatible numeric kinds.");
                }
            }

            switch (leftKind)
            {
                case NumericKind.Int32:
                    return ApplyInt32((int)normalizedLeft, (int)normalizedRight, operation);
                case NumericKind.Int64:
                    return ApplyInt64((long)normalizedLeft, (long)normalizedRight, operation);
                case NumericKind.NativeInt:
                    return ApplyNativeInt((IntPtr)normalizedLeft, (IntPtr)normalizedRight, operation);
                case NumericKind.Single:
                    return ApplySingle((float)normalizedLeft, (float)normalizedRight, operation);
                case NumericKind.Double:
                    return ApplyDouble((double)normalizedLeft, (double)normalizedRight, operation);
                default:
                    throw new InvalidOperationException("The numeric kind is not supported.");
            }
        }

        public static object Negate(object value)
        {
            object normalized = NormalizeNumber(value);

            switch (GetKind(normalized))
            {
                case NumericKind.Int32: return unchecked(-(int)normalized);
                case NumericKind.Int64: return unchecked(-(long)normalized);
                case NumericKind.NativeInt:
                    IntPtr native = (IntPtr)normalized;
                    return IntPtr.Size == 4
                        ? (object)new IntPtr(unchecked(-native.ToInt32()))
                        : new IntPtr(unchecked(-native.ToInt64()));
                case NumericKind.Single: return -(float)normalized;
                case NumericKind.Double: return -(double)normalized;
                default: throw new InvalidOperationException("The numeric kind is not supported.");
            }
        }

        public static object Bitwise(VirtualOpCode opcode, object left, object right)
        {
            object normalizedLeft = NormalizeNumber(left);
            object normalizedRight = NormalizeNumber(right);
            NumericKind kind = GetKind(normalizedLeft);
            NumericKind rightKind = GetKind(normalizedRight);

            if (kind == NumericKind.NativeInt && rightKind == NumericKind.Int32)
            {
                normalizedRight = ConvertInt32ToNative((int)normalizedRight, false);
            }
            else if (kind == NumericKind.Int32 && rightKind == NumericKind.NativeInt)
            {
                normalizedLeft = ConvertInt32ToNative((int)normalizedLeft, false);
                kind = NumericKind.NativeInt;
            }
            else if (kind != rightKind)
            {
                throw new InvalidOperationException("Bitwise operands must use compatible integer kinds.");
            }

            switch (kind)
            {
                case NumericKind.Int32:
                    return ApplyBitwise((int)normalizedLeft, (int)normalizedRight, opcode);
                case NumericKind.Int64:
                    return ApplyBitwise((long)normalizedLeft, (long)normalizedRight, opcode);
                case NumericKind.NativeInt:
                    IntPtr nativeLeft = (IntPtr)normalizedLeft;
                    IntPtr nativeRight = (IntPtr)normalizedRight;
                    return IntPtr.Size == 4
                        ? (object)new IntPtr(ApplyBitwise(nativeLeft.ToInt32(), nativeRight.ToInt32(), opcode))
                        : new IntPtr(ApplyBitwise(nativeLeft.ToInt64(), nativeRight.ToInt64(), opcode));
                default:
                    throw new InvalidOperationException("Bitwise operations require integer values.");
            }
        }

        public static object BitwiseNot(object value)
        {
            object normalized = NormalizeNumber(value);
            switch (GetKind(normalized))
            {
                case NumericKind.Int32: return ~(int)normalized;
                case NumericKind.Int64: return ~(long)normalized;
                case NumericKind.NativeInt:
                    IntPtr native = (IntPtr)normalized;
                    return IntPtr.Size == 4
                        ? (object)new IntPtr(~native.ToInt32())
                        : new IntPtr(~native.ToInt64());
                default: throw new InvalidOperationException("Bitwise not requires an integer value.");
            }
        }

        public static object Shift(VirtualOpCode opcode, object value, object count)
        {
            object normalized = NormalizeNumber(value);
            int shift = (int)NormalizeNumber(count);
            bool right = opcode == VirtualOpCode.Shr || opcode == VirtualOpCode.ShrUn;
            bool unsigned = opcode == VirtualOpCode.ShrUn;

            switch (GetKind(normalized))
            {
                case NumericKind.Int32:
                    return right
                        ? (unsigned ? (object)unchecked((int)((uint)(int)normalized >> shift)) : (int)normalized >> shift)
                        : (int)normalized << shift;
                case NumericKind.Int64:
                    return right
                        ? (unsigned ? (object)unchecked((long)((ulong)(long)normalized >> shift)) : (long)normalized >> shift)
                        : (long)normalized << shift;
                case NumericKind.NativeInt:
                    IntPtr native = (IntPtr)normalized;
                    if (IntPtr.Size == 4)
                    {
                        int native32 = native.ToInt32();
                        return new IntPtr(right
                            ? (unsigned ? unchecked((int)((uint)native32 >> shift)) : native32 >> shift)
                            : native32 << shift);
                    }

                    long native64 = native.ToInt64();
                    return new IntPtr(right
                        ? (unsigned ? unchecked((long)((ulong)native64 >> shift)) : native64 >> shift)
                        : native64 << shift);
                default:
                    throw new InvalidOperationException("Shift operations require integer values.");
            }
        }

        public static int Compare(VirtualOpCode opcode, object left, object right)
        {
            object normalizedLeft = NormalizeNumber(left);
            object normalizedRight = NormalizeNumber(right);
            NumericKind leftKind = GetKind(normalizedLeft);
            NumericKind rightKind = GetKind(normalizedRight);
            bool unsigned = opcode == VirtualOpCode.CgtUn || opcode == VirtualOpCode.CltUn;

            if (leftKind == NumericKind.NativeInt && rightKind == NumericKind.Int32)
            {
                normalizedRight = ConvertInt32ToNative((int)normalizedRight, unsigned);
                rightKind = NumericKind.NativeInt;
            }
            else if (leftKind == NumericKind.Int32 && rightKind == NumericKind.NativeInt)
            {
                normalizedLeft = ConvertInt32ToNative((int)normalizedLeft, unsigned);
                leftKind = NumericKind.NativeInt;
            }
            else if (leftKind != rightKind &&
                (leftKind == NumericKind.Single || leftKind == NumericKind.Double) &&
                (rightKind == NumericKind.Single || rightKind == NumericKind.Double))
            {
                normalizedLeft = ToDouble(normalizedLeft);
                normalizedRight = ToDouble(normalizedRight);
                leftKind = NumericKind.Double;
                rightKind = NumericKind.Double;
            }

            bool equal;
            bool greater;
            bool less;
            bool unordered = false;
            if (leftKind == NumericKind.Single || leftKind == NumericKind.Double)
            {
                double a = leftKind == NumericKind.Single ? (float)normalizedLeft : (double)normalizedLeft;
                double b = rightKind == NumericKind.Single ? (float)normalizedRight : (double)normalizedRight;
                unordered = double.IsNaN(a) || double.IsNaN(b);
                equal = !unordered && a == b;
                greater = !unordered && a > b;
                less = !unordered && a < b;
            }
            else if (leftKind == NumericKind.Int32)
            {
                int a = (int)normalizedLeft;
                int b = (int)normalizedRight;
                equal = a == b;
                greater = unsigned ? (uint)a > (uint)b : a > b;
                less = unsigned ? (uint)a < (uint)b : a < b;
            }
            else if (leftKind == NumericKind.Int64)
            {
                long a = (long)normalizedLeft;
                long b = (long)normalizedRight;
                equal = a == b;
                greater = unsigned ? (ulong)a > (ulong)b : a > b;
                less = unsigned ? (ulong)a < (ulong)b : a < b;
            }
            else if (leftKind == NumericKind.NativeInt)
            {
                IntPtr a = (IntPtr)normalizedLeft;
                IntPtr b = (IntPtr)normalizedRight;
                if (IntPtr.Size == 4)
                {
                    equal = a.ToInt32() == b.ToInt32();
                    greater = unsigned
                        ? unchecked((uint)a.ToInt32()) > unchecked((uint)b.ToInt32())
                        : a.ToInt32() > b.ToInt32();
                    less = unsigned
                        ? unchecked((uint)a.ToInt32()) < unchecked((uint)b.ToInt32())
                        : a.ToInt32() < b.ToInt32();
                }
                else
                {
                    equal = a.ToInt64() == b.ToInt64();
                    greater = unsigned
                        ? unchecked((ulong)a.ToInt64()) > unchecked((ulong)b.ToInt64())
                        : a.ToInt64() > b.ToInt64();
                    less = unsigned
                        ? unchecked((ulong)a.ToInt64()) < unchecked((ulong)b.ToInt64())
                        : a.ToInt64() < b.ToInt64();
                }
            }
            else
            {
                throw new InvalidOperationException("Comparison requires compatible numeric kinds.");
            }

            switch (opcode)
            {
                case VirtualOpCode.Ceq: return equal ? 1 : 0;
                case VirtualOpCode.Cgt: return greater ? 1 : 0;
                case VirtualOpCode.CgtUn: return (greater || unordered) ? 1 : 0;
                case VirtualOpCode.Clt: return less ? 1 : 0;
                case VirtualOpCode.CltUn: return (less || unordered) ? 1 : 0;
                default: throw new InvalidOperationException("The comparison opcode is not supported.");
            }
        }

        public static object ConvertValue(VirtualOpCode opcode, object value)
        {
            int bits = 0;
            bool unsignedTarget = false;
            bool checkedConversion = false;
            bool unsignedSource = false;

            switch (opcode)
            {
                case VirtualOpCode.ConvI1: bits = 8; break;
                case VirtualOpCode.ConvU1: bits = 8; unsignedTarget = true; break;
                case VirtualOpCode.ConvI2: bits = 16; break;
                case VirtualOpCode.ConvU2: bits = 16; unsignedTarget = true; break;
                case VirtualOpCode.ConvI4: bits = 32; break;
                case VirtualOpCode.ConvU4: bits = 32; unsignedTarget = true; break;
                case VirtualOpCode.ConvI8: bits = 64; break;
                case VirtualOpCode.ConvU8: bits = 64; unsignedTarget = true; break;
                case VirtualOpCode.ConvI: break;
                case VirtualOpCode.ConvU: unsignedTarget = true; break;
                case VirtualOpCode.ConvOvfI1: bits = 8; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfU1: bits = 8; unsignedTarget = true; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfI2: bits = 16; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfU2: bits = 16; unsignedTarget = true; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfI4: bits = 32; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfU4: bits = 32; unsignedTarget = true; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfI8: bits = 64; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfU8: bits = 64; unsignedTarget = true; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfI: checkedConversion = true; break;
                case VirtualOpCode.ConvOvfU: unsignedTarget = true; checkedConversion = true; break;
                case VirtualOpCode.ConvOvfI1Un: bits = 8; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfU1Un: bits = 8; unsignedTarget = true; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfI2Un: bits = 16; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfU2Un: bits = 16; unsignedTarget = true; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfI4Un: bits = 32; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfU4Un: bits = 32; unsignedTarget = true; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfI8Un: bits = 64; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfU8Un: bits = 64; unsignedTarget = true; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfIUn: checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvOvfUUn: unsignedTarget = true; checkedConversion = true; unsignedSource = true; break;
                case VirtualOpCode.ConvR4: return ConvertFloating(value, true, false);
                case VirtualOpCode.ConvR8: return ConvertFloating(value, false, false);
                case VirtualOpCode.ConvRUn: return ConvertFloating(value, false, true);
                default: throw new InvalidOperationException("The conversion opcode is not supported.");
            }

            return ConvertInteger(value, bits == 0 ? IntPtr.Size * 8 : bits,
                unsignedTarget, checkedConversion, unsignedSource, bits == 0);
        }

        public static object CheckFinite(object value)
        {
            object normalized = NormalizeNumber(value);
            if (!(normalized is float) && !(normalized is double))
            {
                throw new InvalidOperationException("Ckfinite requires a floating-point value.");
            }

            double number = ToDouble(normalized);
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw new OverflowException("The value is not finite.");
            }

            return normalized;
        }

        private static object ConvertInteger(
            object value,
            int bits,
            bool unsignedTarget,
            bool checkedConversion,
            bool unsignedSource,
            bool nativeTarget)
        {
            if (value is float || value is double)
            {
                if (checkedConversion)
                {
                    if (unsignedSource && (bits == 8 || bits == 16) && IntPtr.Size == 4 &&
                        Environment.Version.Major < 6 && ToDouble(value) < 0d)
                    {
                        throw new OverflowException();
                    }

                    return ConvertCheckedFloating(value, bits, unsignedTarget, nativeTarget);
                }

                return ConvertUncheckedFloating(value, bits, unsignedTarget, nativeTarget);
            }

            if (checkedConversion)
            {
                return unsignedSource
                    ? ConvertCheckedUnsigned(ToUInt64(value), bits, unsignedTarget, nativeTarget)
                    : ConvertCheckedSigned(ToInt64(value), bits, unsignedTarget, nativeTarget);
            }

            return ConvertUncheckedIntegral(value, bits, unsignedTarget, nativeTarget);
        }

        private static object ConvertCheckedFloating(object value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            double number = ToDouble(value);
            if (bits == 8) return unsignedTarget ? (object)(int)checked((byte)number) : (int)checked((sbyte)number);
            if (bits == 16) return unsignedTarget ? (object)(int)checked((ushort)number) : (int)checked((short)number);
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)number)) : checked((int)number);
                return nativeTarget ? (object)new IntPtr(value32) : value32;
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)checked((ulong)number)) : checked((long)number);
                return nativeTarget ? (object)new IntPtr(value64) : value64;
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static object ConvertUncheckedFloating(object value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            double number = ToDouble(value);
            if (bits == 8) return unsignedTarget ? (object)(int)unchecked((byte)number) : (int)unchecked((sbyte)number);
            if (bits == 16) return unsignedTarget ? (object)(int)unchecked((ushort)number) : (int)unchecked((short)number);
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)(uint)number) : unchecked((int)number);
                return nativeTarget ? (object)new IntPtr(value32) : value32;
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)(ulong)number) : unchecked((long)number);
                return nativeTarget ? (object)new IntPtr(value64) : value64;
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static object ConvertCheckedSigned(long value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits == 8) return unsignedTarget ? (object)(int)checked((byte)value) : (int)checked((sbyte)value);
            if (bits == 16) return unsignedTarget ? (object)(int)checked((ushort)value) : (int)checked((short)value);
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)value)) : checked((int)value);
                return nativeTarget ? (object)new IntPtr(value32) : value32;
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)checked((ulong)value)) : value;
                return nativeTarget ? (object)new IntPtr(value64) : value64;
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static object ConvertCheckedUnsigned(ulong value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits == 8) return unsignedTarget ? (object)(int)checked((byte)value) : (int)checked((sbyte)value);
            if (bits == 16) return unsignedTarget ? (object)(int)checked((ushort)value) : (int)checked((short)value);
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)value)) : checked((int)value);
                return nativeTarget ? (object)new IntPtr(value32) : value32;
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)value) : checked((long)value);
                return nativeTarget ? (object)new IntPtr(value64) : value64;
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static object ConvertUncheckedIntegral(object value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits <= 32)
            {
                int result = unsignedTarget
                    ? unchecked((int)(uint)ToUInt64(value))
                    : unchecked((int)ToInt64(value));
                if (nativeTarget) return new IntPtr(result);
                if (bits == 8) return unsignedTarget ? (object)(int)(byte)result : (int)(sbyte)result;
                if (bits == 16) return unsignedTarget ? (object)(int)(ushort)result : (int)(short)result;
                return result;
            }

            long result64 = unsignedTarget
                ? unchecked((long)ToUInt64(value))
                : ToInt64(value);
            return nativeTarget ? (object)new IntPtr(result64) : result64;
        }

        private static object ConvertFloating(object value, bool single, bool unsignedSource)
        {
            if (single) return ToSingle(value, unsignedSource);
            return unsignedSource && !(value is float) && !(value is double)
                ? (object)(double)ToUInt64(value)
                : ToDouble(value);
        }

        private static float ToSingle(object value, bool unsignedSource)
        {
            object normalized = NormalizeNumber(value);
            if (normalized is float) return (float)normalized;
            if (normalized is double) return (float)(double)normalized;
            if (unsignedSource) return (float)ToUInt64(normalized);
            if (normalized is IntPtr) return (float)((IntPtr)normalized).ToInt64();
            if (normalized is int) return (float)(int)normalized;
            return (float)(long)normalized;
        }

        private static long ToInt64(object value)
        {
            object normalized = NormalizeNumber(value);
            if (normalized is IntPtr) return ((IntPtr)normalized).ToInt64();
            if (normalized is int) return (int)normalized;
            if (normalized is long) return (long)normalized;
            throw new InvalidOperationException("The value is not an integer stack value.");
        }

        private static ulong ToUInt64(object value)
        {
            object normalized = NormalizeNumber(value);
            if (normalized is IntPtr)
            {
                IntPtr native = (IntPtr)normalized;
                return IntPtr.Size == 4
                    ? unchecked((uint)native.ToInt32())
                    : unchecked((ulong)native.ToInt64());
            }

            if (normalized is int) return unchecked((uint)(int)normalized);
            if (normalized is long) return unchecked((ulong)(long)normalized);
            throw new InvalidOperationException("The value is not an integer stack value.");
        }

        private static double ToDouble(object value)
        {
            object normalized = NormalizeNumber(value);
            if (normalized is IntPtr) return ((IntPtr)normalized).ToInt64();
            if (normalized is int) return (int)normalized;
            if (normalized is long) return (long)normalized;
            if (normalized is float) return (float)normalized;
            if (normalized is double) return (double)normalized;
            throw new InvalidOperationException("The value cannot be converted to floating point.");
        }

        private static int ApplyBitwise(int left, int right, VirtualOpCode opcode)
        {
            switch (opcode)
            {
                case VirtualOpCode.And: return left & right;
                case VirtualOpCode.Or: return left | right;
                case VirtualOpCode.Xor: return left ^ right;
                default: throw new InvalidOperationException("The bitwise opcode is not supported.");
            }
        }

        private static long ApplyBitwise(long left, long right, VirtualOpCode opcode)
        {
            switch (opcode)
            {
                case VirtualOpCode.And: return left & right;
                case VirtualOpCode.Or: return left | right;
                case VirtualOpCode.Xor: return left ^ right;
                default: throw new InvalidOperationException("The bitwise opcode is not supported.");
            }
        }

        private static bool IsFloatingPoint(object value)
        {
            return value is float || value is double;
        }

        private static NumericKind GetKind(object value)
        {
            if (value is IntPtr)
            {
                return NumericKind.NativeInt;
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Int32: return NumericKind.Int32;
                case TypeCode.Int64: return NumericKind.Int64;
                case TypeCode.Single: return NumericKind.Single;
                case TypeCode.Double: return NumericKind.Double;
                default: throw new InvalidOperationException("The numeric kind is not supported.");
            }
        }

        private static int ApplyInt32(int left, int right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return unchecked(left + right);
                case ArithmeticOperation.AddChecked: return checked(left + right);
                case ArithmeticOperation.AddCheckedUnsigned:
                    return unchecked((int)checked(unchecked((uint)left) + unchecked((uint)right)));
                case ArithmeticOperation.Subtract: return unchecked(left - right);
                case ArithmeticOperation.SubtractChecked: return checked(left - right);
                case ArithmeticOperation.SubtractCheckedUnsigned:
                    return unchecked((int)checked(unchecked((uint)left) - unchecked((uint)right)));
                case ArithmeticOperation.Multiply: return unchecked(left * right);
                case ArithmeticOperation.MultiplyChecked: return checked(left * right);
                case ArithmeticOperation.MultiplyCheckedUnsigned:
                    return unchecked((int)checked(unchecked((uint)left) * unchecked((uint)right)));
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.DivideUnsigned:
                    return unchecked((int)((uint)left / (uint)right));
                case ArithmeticOperation.Remainder: return left % right;
                case ArithmeticOperation.RemainderUnsigned:
                    return unchecked((int)((uint)left % (uint)right));
                default: throw new InvalidOperationException("The arithmetic operation is not supported for Int32 values.");
            }
        }

        private static long ApplyInt64(long left, long right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return unchecked(left + right);
                case ArithmeticOperation.AddChecked: return checked(left + right);
                case ArithmeticOperation.AddCheckedUnsigned:
                    return unchecked((long)checked(unchecked((ulong)left) + unchecked((ulong)right)));
                case ArithmeticOperation.Subtract: return unchecked(left - right);
                case ArithmeticOperation.SubtractChecked: return checked(left - right);
                case ArithmeticOperation.SubtractCheckedUnsigned:
                    return unchecked((long)checked(unchecked((ulong)left) - unchecked((ulong)right)));
                case ArithmeticOperation.Multiply: return unchecked(left * right);
                case ArithmeticOperation.MultiplyChecked: return checked(left * right);
                case ArithmeticOperation.MultiplyCheckedUnsigned:
                    return unchecked((long)checked(unchecked((ulong)left) * unchecked((ulong)right)));
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.DivideUnsigned:
                    return unchecked((long)((ulong)left / (ulong)right));
                case ArithmeticOperation.Remainder: return left % right;
                case ArithmeticOperation.RemainderUnsigned:
                    return unchecked((long)((ulong)left % (ulong)right));
                default: throw new InvalidOperationException("The arithmetic operation is not supported for Int64 values.");
            }
        }

        private static IntPtr ApplyNativeInt(IntPtr left, IntPtr right, ArithmeticOperation operation)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(ApplyInt32(left.ToInt32(), right.ToInt32(), operation));
            }

            return new IntPtr(ApplyInt64(left.ToInt64(), right.ToInt64(), operation));
        }

        private static float ApplySingle(float left, float right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return left + right;
                case ArithmeticOperation.Subtract: return left - right;
                case ArithmeticOperation.Multiply: return left * right;
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.Remainder: return left % right;
                default: throw new InvalidOperationException("This opcode requires an integral numeric kind.");
            }
        }

        private static double ApplyDouble(double left, double right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return left + right;
                case ArithmeticOperation.Subtract: return left - right;
                case ArithmeticOperation.Multiply: return left * right;
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.Remainder: return left % right;
                default: throw new InvalidOperationException("This opcode requires an integral numeric kind.");
            }
        }

        private static IntPtr ToNativeInt(UIntPtr value)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(unchecked((int)value.ToUInt32()));
            }

            return new IntPtr(unchecked((long)value.ToUInt64()));
        }

        private static bool ZeroExtendInt32ForNative(ArithmeticOperation operation)
        {
            // CLR 2.x also zero-extends I4 for mixed native div.un/rem.un.
            return operation == ArithmeticOperation.AddCheckedUnsigned ||
                operation == ArithmeticOperation.SubtractCheckedUnsigned ||
                operation == ArithmeticOperation.MultiplyCheckedUnsigned ||
                (Environment.Version.Major == 2 &&
                    (operation == ArithmeticOperation.DivideUnsigned ||
                     operation == ArithmeticOperation.RemainderUnsigned));
        }

        private static IntPtr ConvertInt32ToNative(int value, bool unsigned)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(value);
            }

            return new IntPtr(unsigned ? (long)(uint)value : (long)value);
        }
    }
}
