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
        Double,
        Decimal
    }

    internal static class NumericArithmetic
    {
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
                case TypeCode.Decimal:
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

            // Reject implicit widening except for the native-int/I4 forms used
            // by native pointer arithmetic.
            if (leftKind != rightKind)
            {
                if (leftKind == NumericKind.NativeInt && rightKind == NumericKind.Int32 &&
                    SupportsNativeOffset(operation, true))
                {
                    normalizedRight = ConvertInt32ToNative((int)normalizedRight, IsUnsigned(operation));
                    rightKind = NumericKind.NativeInt;
                }
                else if (leftKind == NumericKind.Int32 && rightKind == NumericKind.NativeInt &&
                    SupportsNativeOffset(operation, false))
                {
                    normalizedLeft = ConvertInt32ToNative((int)normalizedLeft, IsUnsigned(operation));
                    leftKind = NumericKind.NativeInt;
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
                case NumericKind.Decimal:
                    return ApplyDecimal((decimal)normalizedLeft, (decimal)normalizedRight, operation);
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
                case NumericKind.Decimal: return -(decimal)normalized;
                default: throw new InvalidOperationException("The numeric kind is not supported.");
            }
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
                case TypeCode.Decimal: return NumericKind.Decimal;
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

        private static decimal ApplyDecimal(decimal left, decimal right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return left + right;
                case ArithmeticOperation.Subtract: return left - right;
                case ArithmeticOperation.Multiply: return left * right;
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.Remainder: return left % right;
                default: throw new InvalidOperationException("This opcode is not defined for Decimal values.");
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

        private static bool SupportsNativeOffset(ArithmeticOperation operation, bool nativeIsLeft)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add:
                case ArithmeticOperation.AddChecked:
                case ArithmeticOperation.AddCheckedUnsigned:
                    return true;
                case ArithmeticOperation.Subtract:
                case ArithmeticOperation.SubtractChecked:
                case ArithmeticOperation.SubtractCheckedUnsigned:
                    return nativeIsLeft;
                default:
                    return false;
            }
        }

        private static bool IsUnsigned(ArithmeticOperation operation)
        {
            return operation == ArithmeticOperation.AddCheckedUnsigned ||
                operation == ArithmeticOperation.SubtractCheckedUnsigned;
        }

        private static IntPtr ConvertInt32ToNative(int value, bool unsigned)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(value);
            }

            long widened = unsigned ? (long)(uint)value : (long)value;
            return new IntPtr(widened);
        }
    }
}
