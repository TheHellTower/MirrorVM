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

    internal static class NumericArithmetic
    {
        public static VirtualMachineValue NormalizeStackValue(object value)
        {
            if (value == null)
            {
                return VirtualMachineValue.FromObject(null);
            }

            if (value.GetType().IsEnum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return NormalizeNumber(value);
                default:
                    if (value is IntPtr) return VirtualMachineValue.FromNativeInt((IntPtr)value);
                    return value is UIntPtr
                        ? VirtualMachineValue.FromNativeInt(ToNativeInt((UIntPtr)value))
                        : VirtualMachineValue.FromObject(value);
            }
        }

        public static VirtualMachineValue NormalizeNumber(object value)
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
                case TypeCode.Boolean: return VirtualMachineValue.FromInt32((bool)value ? 1 : 0);
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char: return VirtualMachineValue.FromInt32(Convert.ToInt32(value));
                case TypeCode.Int32: return VirtualMachineValue.FromInt32((int)value);
                case TypeCode.UInt32: return VirtualMachineValue.FromInt32(unchecked((int)(uint)value));
                case TypeCode.Int64: return VirtualMachineValue.FromInt64((long)value);
                case TypeCode.UInt64: return VirtualMachineValue.FromInt64(unchecked((long)(ulong)value));
                case TypeCode.Single: return VirtualMachineValue.FromSingle((float)value);
                case TypeCode.Double: return VirtualMachineValue.FromDouble((double)value);
                default:
                    if (value is IntPtr) return VirtualMachineValue.FromNativeInt((IntPtr)value);
                    if (value is UIntPtr) return VirtualMachineValue.FromNativeInt(ToNativeInt((UIntPtr)value));
                    throw new ArgumentException("The value is not a supported numeric value kind.", "value");
            }
        }

        private static VirtualMachineValue NormalizeNumber(VirtualMachineValue value)
        {
            return value.Kind == VirtualMachineValueKind.Object
                ? NormalizeNumber(value.AsObject())
                : value;
        }

        public static VirtualMachineValue Apply(
            ArithmeticOperation operation,
            VirtualMachineValue left,
            VirtualMachineValue right)
        {
            left = NormalizeNumber(left);
            right = NormalizeNumber(right);
            VirtualMachineValueKind leftKind = left.Kind;
            VirtualMachineValueKind rightKind = right.Kind;

            if (leftKind != rightKind)
            {
                if (leftKind == VirtualMachineValueKind.NativeInt && rightKind == VirtualMachineValueKind.Int32)
                {
                    right = VirtualMachineValue.FromNativeInt(
                        ConvertInt32ToNative(right.AsInt32(), ZeroExtendInt32ForNative(operation)));
                    rightKind = VirtualMachineValueKind.NativeInt;
                }
                else if (leftKind == VirtualMachineValueKind.Int32 && rightKind == VirtualMachineValueKind.NativeInt)
                {
                    left = VirtualMachineValue.FromNativeInt(
                        ConvertInt32ToNative(left.AsInt32(), ZeroExtendInt32ForNative(operation)));
                    leftKind = VirtualMachineValueKind.NativeInt;
                }
                else if (IsFloatingPoint(leftKind) && IsFloatingPoint(rightKind))
                {
                    left = VirtualMachineValue.FromDouble(ToDouble(left));
                    right = VirtualMachineValue.FromDouble(ToDouble(right));
                    leftKind = VirtualMachineValueKind.Double;
                }
                else
                {
                    throw new InvalidOperationException("Arithmetic operands must use compatible numeric kinds.");
                }
            }

            switch (leftKind)
            {
                case VirtualMachineValueKind.Int32:
                    return VirtualMachineValue.FromInt32(ApplyInt32(left.AsInt32(), right.AsInt32(), operation));
                case VirtualMachineValueKind.Int64:
                    return VirtualMachineValue.FromInt64(ApplyInt64(left.AsInt64(), right.AsInt64(), operation));
                case VirtualMachineValueKind.NativeInt:
                    return VirtualMachineValue.FromNativeInt(
                        ApplyNativeInt(left.AsNativeInt(), right.AsNativeInt(), operation));
                case VirtualMachineValueKind.Single:
                    return VirtualMachineValue.FromSingle(ApplySingle(left.AsSingle(), right.AsSingle(), operation));
                case VirtualMachineValueKind.Double:
                    return VirtualMachineValue.FromDouble(ApplyDouble(left.AsDouble(), right.AsDouble(), operation));
                default:
                    throw new InvalidOperationException("The numeric kind is not supported.");
            }
        }

        public static VirtualMachineValue Negate(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.Int32:
                    return VirtualMachineValue.FromInt32(unchecked(-value.AsInt32()));
                case VirtualMachineValueKind.Int64:
                    return VirtualMachineValue.FromInt64(unchecked(-value.AsInt64()));
                case VirtualMachineValueKind.NativeInt:
                    IntPtr native = value.AsNativeInt();
                    return VirtualMachineValue.FromNativeInt(IntPtr.Size == 4
                        ? new IntPtr(unchecked(-native.ToInt32()))
                        : new IntPtr(unchecked(-native.ToInt64())));
                case VirtualMachineValueKind.Single:
                    return VirtualMachineValue.FromSingle(-value.AsSingle());
                case VirtualMachineValueKind.Double:
                    return VirtualMachineValue.FromDouble(-value.AsDouble());
                default:
                    throw new InvalidOperationException("The numeric kind is not supported.");
            }
        }

        public static VirtualMachineValue Bitwise(
            VirtualOpCode opcode,
            VirtualMachineValue left,
            VirtualMachineValue right)
        {
            left = NormalizeNumber(left);
            right = NormalizeNumber(right);
            VirtualMachineValueKind kind = left.Kind;

            if (kind == VirtualMachineValueKind.NativeInt && right.Kind == VirtualMachineValueKind.Int32)
            {
                right = VirtualMachineValue.FromNativeInt(ConvertInt32ToNative(right.AsInt32(), false));
            }
            else if (kind == VirtualMachineValueKind.Int32 && right.Kind == VirtualMachineValueKind.NativeInt)
            {
                left = VirtualMachineValue.FromNativeInt(ConvertInt32ToNative(left.AsInt32(), false));
                kind = VirtualMachineValueKind.NativeInt;
            }
            else if (kind != right.Kind)
            {
                throw new InvalidOperationException("Bitwise operands must use compatible integer kinds.");
            }

            switch (kind)
            {
                case VirtualMachineValueKind.Int32:
                    return VirtualMachineValue.FromInt32(ApplyBitwise(left.AsInt32(), right.AsInt32(), opcode));
                case VirtualMachineValueKind.Int64:
                    return VirtualMachineValue.FromInt64(ApplyBitwise(left.AsInt64(), right.AsInt64(), opcode));
                case VirtualMachineValueKind.NativeInt:
                    IntPtr nativeLeft = left.AsNativeInt();
                    IntPtr nativeRight = right.AsNativeInt();
                    return VirtualMachineValue.FromNativeInt(IntPtr.Size == 4
                        ? new IntPtr(ApplyBitwise(nativeLeft.ToInt32(), nativeRight.ToInt32(), opcode))
                        : new IntPtr(ApplyBitwise(nativeLeft.ToInt64(), nativeRight.ToInt64(), opcode)));
                default:
                    throw new InvalidOperationException("Bitwise operations require integer values.");
            }
        }

        public static VirtualMachineValue BitwiseNot(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.Int32:
                    return VirtualMachineValue.FromInt32(~value.AsInt32());
                case VirtualMachineValueKind.Int64:
                    return VirtualMachineValue.FromInt64(~value.AsInt64());
                case VirtualMachineValueKind.NativeInt:
                    IntPtr native = value.AsNativeInt();
                    return VirtualMachineValue.FromNativeInt(IntPtr.Size == 4
                        ? new IntPtr(~native.ToInt32())
                        : new IntPtr(~native.ToInt64()));
                default:
                    throw new InvalidOperationException("Bitwise not requires an integer value.");
            }
        }

        public static VirtualMachineValue Shift(
            VirtualOpCode opcode,
            VirtualMachineValue value,
            VirtualMachineValue count)
        {
            value = NormalizeNumber(value);
            count = NormalizeNumber(count);
            if (count.Kind != VirtualMachineValueKind.Int32)
            {
                throw new InvalidCastException();
            }

            int shift = count.AsInt32();
            bool right = opcode == VirtualOpCode.Shr || opcode == VirtualOpCode.ShrUn;
            bool unsigned = opcode == VirtualOpCode.ShrUn;
            switch (value.Kind)
            {
                case VirtualMachineValueKind.Int32:
                    int intValue = value.AsInt32();
                    return VirtualMachineValue.FromInt32(right
                        ? (unsigned ? unchecked((int)((uint)intValue >> shift)) : intValue >> shift)
                        : intValue << shift);
                case VirtualMachineValueKind.Int64:
                    long longValue = value.AsInt64();
                    return VirtualMachineValue.FromInt64(right
                        ? (unsigned ? unchecked((long)((ulong)longValue >> shift)) : longValue >> shift)
                        : longValue << shift);
                case VirtualMachineValueKind.NativeInt:
                    IntPtr native = value.AsNativeInt();
                    if (IntPtr.Size == 4)
                    {
                        int native32 = native.ToInt32();
                        return VirtualMachineValue.FromNativeInt(new IntPtr(right
                            ? (unsigned ? unchecked((int)((uint)native32 >> shift)) : native32 >> shift)
                            : native32 << shift));
                    }

                    long native64 = native.ToInt64();
                    return VirtualMachineValue.FromNativeInt(new IntPtr(right
                        ? (unsigned ? unchecked((long)((ulong)native64 >> shift)) : native64 >> shift)
                        : native64 << shift));
                default:
                    throw new InvalidOperationException("Shift operations require integer values.");
            }
        }

        public static VirtualMachineValue Compare(
            VirtualOpCode opcode,
            VirtualMachineValue left,
            VirtualMachineValue right)
        {
            left = NormalizeNumber(left);
            right = NormalizeNumber(right);
            VirtualMachineValueKind leftKind = left.Kind;
            VirtualMachineValueKind rightKind = right.Kind;
            bool unsigned = opcode == VirtualOpCode.CgtUn || opcode == VirtualOpCode.CltUn;

            if (leftKind == VirtualMachineValueKind.NativeInt && rightKind == VirtualMachineValueKind.Int32)
            {
                right = VirtualMachineValue.FromNativeInt(ConvertInt32ToNative(right.AsInt32(), unsigned));
                rightKind = VirtualMachineValueKind.NativeInt;
            }
            else if (leftKind == VirtualMachineValueKind.Int32 && rightKind == VirtualMachineValueKind.NativeInt)
            {
                left = VirtualMachineValue.FromNativeInt(ConvertInt32ToNative(left.AsInt32(), unsigned));
                leftKind = VirtualMachineValueKind.NativeInt;
            }
            else if (leftKind != rightKind && IsFloatingPoint(leftKind) && IsFloatingPoint(rightKind))
            {
                left = VirtualMachineValue.FromDouble(ToDouble(left));
                right = VirtualMachineValue.FromDouble(ToDouble(right));
                leftKind = VirtualMachineValueKind.Double;
                rightKind = VirtualMachineValueKind.Double;
            }

            bool equal;
            bool greater;
            bool less;
            bool unordered = false;
            if (IsFloatingPoint(leftKind))
            {
                if (!IsFloatingPoint(rightKind)) throw new InvalidCastException();
                double a = ToDouble(left);
                double b = ToDouble(right);
                unordered = double.IsNaN(a) || double.IsNaN(b);
                equal = !unordered && a == b;
                greater = !unordered && a > b;
                less = !unordered && a < b;
            }
            else if (leftKind == VirtualMachineValueKind.Int32)
            {
                if (rightKind != leftKind) throw new InvalidCastException();
                int a = left.AsInt32();
                int b = right.AsInt32();
                equal = a == b;
                greater = unsigned ? (uint)a > (uint)b : a > b;
                less = unsigned ? (uint)a < (uint)b : a < b;
            }
            else if (leftKind == VirtualMachineValueKind.Int64)
            {
                if (rightKind != leftKind) throw new InvalidCastException();
                long a = left.AsInt64();
                long b = right.AsInt64();
                equal = a == b;
                greater = unsigned ? (ulong)a > (ulong)b : a > b;
                less = unsigned ? (ulong)a < (ulong)b : a < b;
            }
            else if (leftKind == VirtualMachineValueKind.NativeInt)
            {
                if (rightKind != leftKind) throw new InvalidCastException();
                IntPtr a = left.AsNativeInt();
                IntPtr b = right.AsNativeInt();
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
                case VirtualOpCode.Ceq: return VirtualMachineValue.FromInt32(equal ? 1 : 0);
                case VirtualOpCode.Cgt: return VirtualMachineValue.FromInt32(greater ? 1 : 0);
                case VirtualOpCode.CgtUn: return VirtualMachineValue.FromInt32(greater || unordered ? 1 : 0);
                case VirtualOpCode.Clt: return VirtualMachineValue.FromInt32(less ? 1 : 0);
                case VirtualOpCode.CltUn: return VirtualMachineValue.FromInt32(less || unordered ? 1 : 0);
                default: throw new InvalidOperationException("The comparison opcode is not supported.");
            }
        }

        public static VirtualMachineValue ConvertValue(VirtualOpCode opcode, VirtualMachineValue value)
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

        public static VirtualMachineValue CheckFinite(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            if (!IsFloatingPoint(value.Kind))
            {
                throw new InvalidOperationException("Ckfinite requires a floating-point value.");
            }

            double number = ToDouble(value);
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw new OverflowException("The value is not finite.");
            }

            return value;
        }

        private static VirtualMachineValue ConvertInteger(
            VirtualMachineValue value,
            int bits,
            bool unsignedTarget,
            bool checkedConversion,
            bool unsignedSource,
            bool nativeTarget)
        {
            if (IsFloatingPoint(value.Kind))
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

        private static VirtualMachineValue ConvertCheckedFloating(
            VirtualMachineValue value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            double number = ToDouble(value);
            if (bits == 8) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((byte)number) : checked((sbyte)number));
            if (bits == 16) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((ushort)number) : checked((short)number));
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)number)) : checked((int)number);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value32)) : VirtualMachineValue.FromInt32(value32);
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)checked((ulong)number)) : checked((long)number);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value64)) : VirtualMachineValue.FromInt64(value64);
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static VirtualMachineValue ConvertUncheckedFloating(
            VirtualMachineValue value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            double number = ToDouble(value);
            if (bits == 8) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)unchecked((byte)number) : unchecked((sbyte)number));
            if (bits == 16) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)unchecked((ushort)number) : unchecked((short)number));
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)(uint)number) : unchecked((int)number);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value32)) : VirtualMachineValue.FromInt32(value32);
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)(ulong)number) : unchecked((long)number);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value64)) : VirtualMachineValue.FromInt64(value64);
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static VirtualMachineValue ConvertCheckedSigned(long value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits == 8) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((byte)value) : checked((sbyte)value));
            if (bits == 16) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((ushort)value) : checked((short)value));
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)value)) : checked((int)value);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value32)) : VirtualMachineValue.FromInt32(value32);
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)checked((ulong)value)) : value;
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value64)) : VirtualMachineValue.FromInt64(value64);
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static VirtualMachineValue ConvertCheckedUnsigned(ulong value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits == 8) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((byte)value) : checked((sbyte)value));
            if (bits == 16) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)checked((ushort)value) : checked((short)value));
            if (bits == 32)
            {
                int value32 = unsignedTarget ? unchecked((int)checked((uint)value)) : checked((int)value);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value32)) : VirtualMachineValue.FromInt32(value32);
            }
            if (bits == 64)
            {
                long value64 = unsignedTarget ? unchecked((long)value) : checked((long)value);
                return nativeTarget ? VirtualMachineValue.FromNativeInt(new IntPtr(value64)) : VirtualMachineValue.FromInt64(value64);
            }

            throw new InvalidOperationException("The numeric conversion width is not supported.");
        }

        private static VirtualMachineValue ConvertUncheckedIntegral(
            VirtualMachineValue value, int bits, bool unsignedTarget, bool nativeTarget)
        {
            if (bits <= 32)
            {
                int result = unsignedTarget
                    ? unchecked((int)(uint)ToUInt64(value))
                    : unchecked((int)ToInt64(value));
                if (nativeTarget) return VirtualMachineValue.FromNativeInt(new IntPtr(result));
                if (bits == 8) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)(byte)result : (int)(sbyte)result);
                if (bits == 16) return VirtualMachineValue.FromInt32(unsignedTarget ? (int)(ushort)result : (int)(short)result);
                return VirtualMachineValue.FromInt32(result);
            }

            long result64 = unsignedTarget
                ? unchecked((long)ToUInt64(value))
                : ToInt64(value);
            return nativeTarget
                ? VirtualMachineValue.FromNativeInt(new IntPtr(result64))
                : VirtualMachineValue.FromInt64(result64);
        }

        private static VirtualMachineValue ConvertFloating(VirtualMachineValue value, bool single, bool unsignedSource)
        {
            if (single) return VirtualMachineValue.FromSingle(ToSingle(value, unsignedSource));
            return VirtualMachineValue.FromDouble(unsignedSource && !IsFloatingPoint(value.Kind)
                ? (double)ToUInt64(value)
                : ToDouble(value));
        }

        private static float ToSingle(VirtualMachineValue value, bool unsignedSource)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.Single: return value.AsSingle();
                case VirtualMachineValueKind.Double: return (float)value.AsDouble();
                case VirtualMachineValueKind.NativeInt:
                    return unsignedSource ? (float)ToUInt64(value) : (float)value.AsNativeInt().ToInt64();
                case VirtualMachineValueKind.Int32:
                    return unsignedSource ? (float)ToUInt64(value) : (float)value.AsInt32();
                case VirtualMachineValueKind.Int64:
                    return unsignedSource ? (float)ToUInt64(value) : (float)value.AsInt64();
                default: throw new InvalidOperationException("The value cannot be converted to floating point.");
            }
        }

        private static long ToInt64(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.NativeInt: return value.AsNativeInt().ToInt64();
                case VirtualMachineValueKind.Int32: return value.AsInt32();
                case VirtualMachineValueKind.Int64: return value.AsInt64();
                default: throw new InvalidOperationException("The value is not an integer stack value.");
            }
        }

        private static ulong ToUInt64(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.NativeInt:
                    IntPtr native = value.AsNativeInt();
                    return IntPtr.Size == 4
                        ? unchecked((uint)native.ToInt32())
                        : unchecked((ulong)native.ToInt64());
                case VirtualMachineValueKind.Int32: return unchecked((uint)value.AsInt32());
                case VirtualMachineValueKind.Int64: return unchecked((ulong)value.AsInt64());
                default: throw new InvalidOperationException("The value is not an integer stack value.");
            }
        }

        private static double ToDouble(VirtualMachineValue value)
        {
            value = NormalizeNumber(value);
            switch (value.Kind)
            {
                case VirtualMachineValueKind.NativeInt: return value.AsNativeInt().ToInt64();
                case VirtualMachineValueKind.Int32: return value.AsInt32();
                case VirtualMachineValueKind.Int64: return value.AsInt64();
                case VirtualMachineValueKind.Single: return value.AsSingle();
                case VirtualMachineValueKind.Double: return value.AsDouble();
                default: throw new InvalidOperationException("The value cannot be converted to floating point.");
            }
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

        private static bool IsFloatingPoint(VirtualMachineValueKind kind)
        {
            return kind == VirtualMachineValueKind.Single || kind == VirtualMachineValueKind.Double;
        }

        private static int ApplyInt32(int left, int right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return unchecked(left + right);
                case ArithmeticOperation.AddChecked: return checked(left + right);
                case ArithmeticOperation.AddCheckedUnsigned: return unchecked((int)checked(unchecked((uint)left) + unchecked((uint)right)));
                case ArithmeticOperation.Subtract: return unchecked(left - right);
                case ArithmeticOperation.SubtractChecked: return checked(left - right);
                case ArithmeticOperation.SubtractCheckedUnsigned: return unchecked((int)checked(unchecked((uint)left) - unchecked((uint)right)));
                case ArithmeticOperation.Multiply: return unchecked(left * right);
                case ArithmeticOperation.MultiplyChecked: return checked(left * right);
                case ArithmeticOperation.MultiplyCheckedUnsigned: return unchecked((int)checked(unchecked((uint)left) * unchecked((uint)right)));
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.DivideUnsigned: return unchecked((int)((uint)left / (uint)right));
                case ArithmeticOperation.Remainder: return left % right;
                case ArithmeticOperation.RemainderUnsigned: return unchecked((int)((uint)left % (uint)right));
                default: throw new InvalidOperationException("The arithmetic operation is not supported for Int32 values.");
            }
        }

        private static long ApplyInt64(long left, long right, ArithmeticOperation operation)
        {
            switch (operation)
            {
                case ArithmeticOperation.Add: return unchecked(left + right);
                case ArithmeticOperation.AddChecked: return checked(left + right);
                case ArithmeticOperation.AddCheckedUnsigned: return unchecked((long)checked(unchecked((ulong)left) + unchecked((ulong)right)));
                case ArithmeticOperation.Subtract: return unchecked(left - right);
                case ArithmeticOperation.SubtractChecked: return checked(left - right);
                case ArithmeticOperation.SubtractCheckedUnsigned: return unchecked((long)checked(unchecked((ulong)left) - unchecked((ulong)right)));
                case ArithmeticOperation.Multiply: return unchecked(left * right);
                case ArithmeticOperation.MultiplyChecked: return checked(left * right);
                case ArithmeticOperation.MultiplyCheckedUnsigned: return unchecked((long)checked(unchecked((ulong)left) * unchecked((ulong)right)));
                case ArithmeticOperation.Divide: return left / right;
                case ArithmeticOperation.DivideUnsigned: return unchecked((long)((ulong)left / (ulong)right));
                case ArithmeticOperation.Remainder: return left % right;
                case ArithmeticOperation.RemainderUnsigned: return unchecked((long)((ulong)left % (ulong)right));
                default: throw new InvalidOperationException("The arithmetic operation is not supported for Int64 values.");
            }
        }

        private static IntPtr ApplyNativeInt(IntPtr left, IntPtr right, ArithmeticOperation operation)
        {
            return IntPtr.Size == 4
                ? new IntPtr(ApplyInt32(left.ToInt32(), right.ToInt32(), operation))
                : new IntPtr(ApplyInt64(left.ToInt64(), right.ToInt64(), operation));
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
            return IntPtr.Size == 4
                ? new IntPtr(unchecked((int)value.ToUInt32()))
                : new IntPtr(unchecked((long)value.ToUInt64()));
        }

        private static bool ZeroExtendInt32ForNative(ArithmeticOperation operation)
        {
            return operation == ArithmeticOperation.AddCheckedUnsigned ||
                operation == ArithmeticOperation.SubtractCheckedUnsigned ||
                operation == ArithmeticOperation.MultiplyCheckedUnsigned ||
                (Environment.Version.Major == 2 &&
                    (operation == ArithmeticOperation.DivideUnsigned ||
                     operation == ArithmeticOperation.RemainderUnsigned));
        }

        private static IntPtr ConvertInt32ToNative(int value, bool unsigned)
        {
            return IntPtr.Size == 4
                ? new IntPtr(value)
                : new IntPtr(unsigned ? (long)(uint)value : value);
        }
    }
}
