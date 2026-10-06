// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.Runtime.InteropServices;

namespace MirrorVM
{
    public struct VirtualMachineValue
    {
        private readonly object _reference;
        private readonly long _bits;
        private readonly VirtualMachineValueKind _kind;

        private VirtualMachineValue(VirtualMachineValueKind kind, long bits, object reference)
        {
            _kind = kind;
            _bits = bits;
            _reference = reference;
        }

        internal VirtualMachineValueKind Kind { get { return _kind; } }

        public static VirtualMachineValue FromObject(object value)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.Object, 0, value);
        }

        public object ToObject()
        {
            switch (_kind)
            {
                case VirtualMachineValueKind.Int32: return unchecked((int)_bits);
                case VirtualMachineValueKind.Int64: return _bits;
                case VirtualMachineValueKind.NativeInt: return AsNativeInt();
                case VirtualMachineValueKind.Single: return AsSingle();
                case VirtualMachineValueKind.Double: return AsDouble();
                default: return _reference;
            }
        }

        internal static VirtualMachineValue FromInt32(int value)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.Int32, value, null);
        }

        internal static VirtualMachineValue FromInt64(long value)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.Int64, value, null);
        }

        internal static VirtualMachineValue FromNativeInt(IntPtr value)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.NativeInt, value.ToInt64(), null);
        }

        internal static VirtualMachineValue FromSingle(float value)
        {
            SingleBits bits = new SingleBits();
            bits.Value = value;
            return new VirtualMachineValue(VirtualMachineValueKind.Single, bits.Bits, null);
        }

        internal static VirtualMachineValue FromDouble(double value)
        {
            DoubleBits bits = new DoubleBits();
            bits.Value = value;
            return new VirtualMachineValue(VirtualMachineValueKind.Double, bits.Bits, null);
        }

        internal static VirtualMachineValue FromSingleBits(int bits)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.Single, bits, null);
        }

        internal static VirtualMachineValue FromDoubleBits(long bits)
        {
            return new VirtualMachineValue(VirtualMachineValueKind.Double, bits, null);
        }

        internal int AsInt32() { return unchecked((int)_bits); }
        internal long AsInt64() { return _bits; }
        internal IntPtr AsNativeInt()
        {
            return IntPtr.Size == 4
                ? new IntPtr(unchecked((int)_bits))
                : new IntPtr(_bits);
        }

        internal float AsSingle()
        {
            SingleBits bits = new SingleBits();
            bits.Bits = unchecked((int)_bits);
            return bits.Value;
        }

        internal double AsDouble()
        {
            DoubleBits bits = new DoubleBits();
            bits.Bits = _bits;
            return bits.Value;
        }

        internal object AsObject() { return _reference; }

        public static implicit operator VirtualMachineValue(int value) { return FromInt32(value); }
        public static implicit operator VirtualMachineValue(long value) { return FromInt64(value); }
        public static implicit operator VirtualMachineValue(float value) { return FromSingle(value); }
        public static implicit operator VirtualMachineValue(double value) { return FromDouble(value); }
        public static implicit operator VirtualMachineValue(IntPtr value) { return FromNativeInt(value); }
        public static implicit operator VirtualMachineValue(string value) { return FromObject(value); }

        [StructLayout(LayoutKind.Explicit)]
        private struct SingleBits
        {
            [FieldOffset(0)] public int Bits;
            [FieldOffset(0)] public float Value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct DoubleBits
        {
            [FieldOffset(0)] public long Bits;
            [FieldOffset(0)] public double Value;
        }
    }

    internal enum VirtualMachineValueKind : byte
    {
        Object,
        Int32,
        Int64,
        NativeInt,
        Single,
        Double
    }
}
