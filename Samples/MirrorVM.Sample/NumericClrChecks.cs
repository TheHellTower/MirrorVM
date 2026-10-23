// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.Reflection;
using System.Reflection.Emit;
using MirrorVM;
using ClrOpCodes = System.Reflection.Emit.OpCodes;

namespace MirrorVM.Sample
{
    internal static class NumericClrChecks
    {
        private static readonly VirtualOpCode[] Conversions =
        {
            VirtualOpCode.ConvI1, VirtualOpCode.ConvU1, VirtualOpCode.ConvI2, VirtualOpCode.ConvU2,
            VirtualOpCode.ConvI4, VirtualOpCode.ConvU4, VirtualOpCode.ConvI8, VirtualOpCode.ConvU8,
            VirtualOpCode.ConvI, VirtualOpCode.ConvU, VirtualOpCode.ConvR4, VirtualOpCode.ConvR8,
            VirtualOpCode.ConvRUn, VirtualOpCode.ConvOvfI1, VirtualOpCode.ConvOvfU1,
            VirtualOpCode.ConvOvfI2, VirtualOpCode.ConvOvfU2, VirtualOpCode.ConvOvfI4,
            VirtualOpCode.ConvOvfU4, VirtualOpCode.ConvOvfI8, VirtualOpCode.ConvOvfU8,
            VirtualOpCode.ConvOvfI, VirtualOpCode.ConvOvfU, VirtualOpCode.ConvOvfI1Un,
            VirtualOpCode.ConvOvfU1Un, VirtualOpCode.ConvOvfI2Un, VirtualOpCode.ConvOvfU2Un,
            VirtualOpCode.ConvOvfI4Un, VirtualOpCode.ConvOvfU4Un, VirtualOpCode.ConvOvfI8Un,
            VirtualOpCode.ConvOvfU8Un, VirtualOpCode.ConvOvfIUn, VirtualOpCode.ConvOvfUUn
        };

        private static readonly VirtualOpCode[] Arithmetic =
        {
            VirtualOpCode.Add, VirtualOpCode.AddOvf, VirtualOpCode.AddOvfUn,
            VirtualOpCode.Sub, VirtualOpCode.SubOvf, VirtualOpCode.SubOvfUn,
            VirtualOpCode.Mul, VirtualOpCode.MulOvf, VirtualOpCode.MulOvfUn,
            VirtualOpCode.Div, VirtualOpCode.DivUn, VirtualOpCode.Rem, VirtualOpCode.RemUn
        };

        private static readonly VirtualOpCode[] BitwiseComparisons =
        {
            VirtualOpCode.And, VirtualOpCode.Or, VirtualOpCode.Xor,
            VirtualOpCode.Shl, VirtualOpCode.Shr, VirtualOpCode.ShrUn,
            VirtualOpCode.Ceq, VirtualOpCode.Cgt, VirtualOpCode.CgtUn,
            VirtualOpCode.Clt, VirtualOpCode.CltUn
        };

        public static void Run()
        {
            CheckArithmetic();
            CheckConversions();
            CheckBitwiseAndComparisons();
            CheckUnary();
            CheckSinglePrecisionStack();
            CheckUncheckedArithmeticEdges();
            CheckCheckedArithmeticEdges();
            CheckDivisionAndRemainderEdges();
            CheckShiftCounts();
            CheckFloatingPointEdges();
        }

        private static void CheckArithmetic()
        {
            object[][] values =
            {
                new object[] { typeof(int), -7, 2 },
                new object[] { typeof(uint), uint.MaxValue - 7U, 2U },
                new object[] { typeof(long), -7L, 2L },
                new object[] { typeof(ulong), ulong.MaxValue - 7UL, 2UL },
                new object[] { typeof(IntPtr), new IntPtr(-7), new IntPtr(2) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue - 7U) : new UIntPtr(ulong.MaxValue - 7UL), new UIntPtr(2U) },
                new object[] { typeof(float), 20.0f, 6.0f },
                new object[] { typeof(double), 20.0d, 6.0d }
            };

            foreach (VirtualOpCode opcode in Arithmetic)
            {
                foreach (object[] pair in values)
                {
                    Type type = (Type)pair[0];
                    if ((opcode == VirtualOpCode.AddOvf || opcode == VirtualOpCode.AddOvfUn ||
                         opcode == VirtualOpCode.SubOvf || opcode == VirtualOpCode.SubOvfUn ||
                         opcode == VirtualOpCode.MulOvf || opcode == VirtualOpCode.MulOvfUn ||
                         opcode == VirtualOpCode.DivUn || opcode == VirtualOpCode.RemUn) &&
                        (type == typeof(float) || type == typeof(double)))
                    {
                        continue;
                    }

                    Check(opcode, new Type[] { type, type }, new object[] { pair[1], pair[2] }, type);
                }
            }

            foreach (VirtualOpCode opcode in Arithmetic)
            {
                IntPtr native = new IntPtr(IntPtr.Size == 4 ? 0x10000000L : 0x100000000L);
                Check(opcode, new Type[] { typeof(IntPtr), typeof(int) }, new object[] { native, -1 }, typeof(IntPtr));
                Check(opcode, new Type[] { typeof(int), typeof(IntPtr) }, new object[] { -1, native }, typeof(IntPtr));
            }

            VirtualOpCode[] floating =
            {
                VirtualOpCode.Add, VirtualOpCode.Sub, VirtualOpCode.Mul,
                VirtualOpCode.Div, VirtualOpCode.Rem
            };
            foreach (VirtualOpCode opcode in floating)
            {
                Check(opcode, new[] { typeof(float), typeof(double) },
                    new object[] { 20f, 6d }, typeof(float), true);
                Check(opcode, new[] { typeof(double), typeof(float) },
                    new object[] { 20d, 6f }, typeof(double), true);
            }

            Check(VirtualOpCode.AddOvf, new[] { typeof(int), typeof(int) }, new object[] { int.MaxValue, 1 }, typeof(int));
            Check(VirtualOpCode.AddOvfUn, new[] { typeof(uint), typeof(uint) }, new object[] { uint.MaxValue, 1U }, typeof(uint));
            Check(VirtualOpCode.SubOvf, new[] { typeof(long), typeof(long) }, new object[] { long.MinValue, 1L }, typeof(long));
            Check(VirtualOpCode.SubOvfUn, new[] { typeof(uint), typeof(uint) }, new object[] { 0U, 1U }, typeof(uint));
            Check(VirtualOpCode.MulOvf, new[] { typeof(int), typeof(int) }, new object[] { int.MaxValue, 2 }, typeof(int));
            Check(VirtualOpCode.MulOvfUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { ulong.MaxValue, 2UL }, typeof(ulong));
        }

        private static void CheckCheckedArithmeticEdges()
        {
            object[][] cases =
            {
                new object[] { VirtualOpCode.AddOvf, typeof(int), int.MaxValue, 1, typeof(int) },
                new object[] { VirtualOpCode.AddOvf, typeof(int), int.MinValue, -1, typeof(int) },
                new object[] { VirtualOpCode.AddOvf, typeof(long), long.MaxValue, 1L, typeof(long) },
                new object[] { VirtualOpCode.AddOvf, typeof(long), long.MinValue, -1L, typeof(long) },
                new object[] { VirtualOpCode.AddOvf, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue), new IntPtr(1), typeof(IntPtr) },
                new object[] { VirtualOpCode.AddOvfUn, typeof(uint), uint.MaxValue, 1U, typeof(uint) },
                new object[] { VirtualOpCode.AddOvfUn, typeof(ulong), ulong.MaxValue, 1UL, typeof(ulong) },
                new object[] { VirtualOpCode.AddOvfUn, typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue), new UIntPtr(1U), typeof(UIntPtr) },
                new object[] { VirtualOpCode.AddOvfUn, typeof(IntPtr), new IntPtr(-1), new IntPtr(1), typeof(IntPtr) },
                new object[] { VirtualOpCode.SubOvf, typeof(int), int.MinValue, 1, typeof(int) },
                new object[] { VirtualOpCode.SubOvf, typeof(int), int.MaxValue, -1, typeof(int) },
                new object[] { VirtualOpCode.SubOvf, typeof(long), long.MinValue, 1L, typeof(long) },
                new object[] { VirtualOpCode.SubOvf, typeof(long), long.MaxValue, -1L, typeof(long) },
                new object[] { VirtualOpCode.SubOvf, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue), new IntPtr(1), typeof(IntPtr) },
                new object[] { VirtualOpCode.SubOvfUn, typeof(uint), 0U, 1U, typeof(uint) },
                new object[] { VirtualOpCode.SubOvfUn, typeof(ulong), 0UL, 1UL, typeof(ulong) },
                new object[] { VirtualOpCode.SubOvfUn, typeof(UIntPtr), new UIntPtr(0U), new UIntPtr(1U), typeof(UIntPtr) },
                new object[] { VirtualOpCode.MulOvf, typeof(int), int.MaxValue, 2, typeof(int) },
                new object[] { VirtualOpCode.MulOvf, typeof(int), int.MinValue, -1, typeof(int) },
                new object[] { VirtualOpCode.MulOvf, typeof(long), long.MaxValue, 2L, typeof(long) },
                new object[] { VirtualOpCode.MulOvf, typeof(long), long.MinValue, -1L, typeof(long) },
                new object[] { VirtualOpCode.MulOvf, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue), new IntPtr(-1), typeof(IntPtr) },
                new object[] { VirtualOpCode.MulOvfUn, typeof(uint), uint.MaxValue, 2U, typeof(uint) },
                new object[] { VirtualOpCode.MulOvfUn, typeof(ulong), ulong.MaxValue, 2UL, typeof(ulong) },
                new object[] { VirtualOpCode.MulOvfUn, typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue), new UIntPtr(2U), typeof(UIntPtr) }
            };

            foreach (object[] test in cases)
            {
                Check((VirtualOpCode)test[0], new[] { (Type)test[1], (Type)test[1] },
                    new[] { test[2], test[3] }, (Type)test[4]);
            }
        }

        private static void CheckUncheckedArithmeticEdges()
        {
            object[][] cases =
            {
                new object[] { VirtualOpCode.Add, typeof(int), int.MaxValue, 1, typeof(int) },
                new object[] { VirtualOpCode.Add, typeof(uint), uint.MaxValue, 1U, typeof(uint) },
                new object[] { VirtualOpCode.Add, typeof(long), long.MaxValue, 1L, typeof(long) },
                new object[] { VirtualOpCode.Add, typeof(ulong), ulong.MaxValue, 1UL, typeof(ulong) },
                new object[] { VirtualOpCode.Add, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue), new IntPtr(1), typeof(IntPtr) },
                new object[] { VirtualOpCode.Add, typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue), new UIntPtr(1U), typeof(UIntPtr) },
                new object[] { VirtualOpCode.Sub, typeof(int), int.MinValue, 1, typeof(int) },
                new object[] { VirtualOpCode.Sub, typeof(uint), 0U, 1U, typeof(uint) },
                new object[] { VirtualOpCode.Sub, typeof(long), long.MinValue, 1L, typeof(long) },
                new object[] { VirtualOpCode.Sub, typeof(ulong), 0UL, 1UL, typeof(ulong) },
                new object[] { VirtualOpCode.Sub, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue), new IntPtr(1), typeof(IntPtr) },
                new object[] { VirtualOpCode.Sub, typeof(UIntPtr), new UIntPtr(0U), new UIntPtr(1U), typeof(UIntPtr) },
                new object[] { VirtualOpCode.Mul, typeof(int), int.MaxValue, 2, typeof(int) },
                new object[] { VirtualOpCode.Mul, typeof(uint), uint.MaxValue, 2U, typeof(uint) },
                new object[] { VirtualOpCode.Mul, typeof(long), long.MaxValue, 2L, typeof(long) },
                new object[] { VirtualOpCode.Mul, typeof(ulong), ulong.MaxValue, 2UL, typeof(ulong) },
                new object[] { VirtualOpCode.Mul, typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue), new IntPtr(2), typeof(IntPtr) },
                new object[] { VirtualOpCode.Mul, typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue), new UIntPtr(2U), typeof(UIntPtr) }
            };

            foreach (object[] test in cases)
            {
                Check((VirtualOpCode)test[0], new[] { (Type)test[1], (Type)test[1] },
                    new[] { test[2], test[3] }, (Type)test[4]);
            }
        }

        private static void CheckDivisionAndRemainderEdges()
        {
            object[][] signed32 =
            {
                new object[] { int.MinValue, -1 },
                new object[] { -7, 3 },
                new object[] { 7, -3 },
                new object[] { -7, -3 },
                new object[] { 5, 0 }
            };
            object[][] signed64 =
            {
                new object[] { long.MinValue, -1L },
                new object[] { -7L, 3L },
                new object[] { 7L, -3L },
                new object[] { -7L, -3L },
                new object[] { 5L, 0L }
            };
            VirtualOpCode[] signedOperations = { VirtualOpCode.Div, VirtualOpCode.Rem };
            foreach (VirtualOpCode opcode in signedOperations)
            {
                foreach (object[] pair in signed32)
                    Check(opcode, new[] { typeof(int), typeof(int) }, pair, typeof(int));
                foreach (object[] pair in signed64)
                    Check(opcode, new[] { typeof(long), typeof(long) }, pair, typeof(long));
            }

            Check(VirtualOpCode.DivUn, new[] { typeof(uint), typeof(uint) }, new object[] { uint.MaxValue, 2U }, typeof(uint));
            Check(VirtualOpCode.DivUn, new[] { typeof(uint), typeof(uint) }, new object[] { 1U, 0U }, typeof(uint));
            Check(VirtualOpCode.RemUn, new[] { typeof(uint), typeof(uint) }, new object[] { uint.MaxValue, 10U }, typeof(uint));
            Check(VirtualOpCode.RemUn, new[] { typeof(uint), typeof(uint) }, new object[] { 1U, 0U }, typeof(uint));
            Check(VirtualOpCode.DivUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { ulong.MaxValue, 2UL }, typeof(ulong));
            Check(VirtualOpCode.DivUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { 1UL, 0UL }, typeof(ulong));
            Check(VirtualOpCode.RemUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { ulong.MaxValue, 10UL }, typeof(ulong));
            Check(VirtualOpCode.RemUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { 1UL, 0UL }, typeof(ulong));

            IntPtr nativeMin = IntPtr.Size == 4 ? new IntPtr(int.MinValue) : new IntPtr(long.MinValue);
            Check(VirtualOpCode.Div, new[] { typeof(IntPtr), typeof(int) }, new object[] { nativeMin, -1 }, typeof(IntPtr));
            Check(VirtualOpCode.Rem, new[] { typeof(IntPtr), typeof(int) }, new object[] { nativeMin, -1 }, typeof(IntPtr));
            Check(VirtualOpCode.Rem, new[] { typeof(IntPtr), typeof(int) }, new object[] { new IntPtr(-7), 3 }, typeof(IntPtr));
            Check(VirtualOpCode.Div, new[] { typeof(IntPtr), typeof(int) }, new object[] { new IntPtr(1), 0 }, typeof(IntPtr));
            UIntPtr unsignedNativeMax = IntPtr.Size == 4 ? new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue);
            Check(VirtualOpCode.DivUn, new[] { typeof(UIntPtr), typeof(UIntPtr) }, new object[] { unsignedNativeMax, new UIntPtr(2U) }, typeof(UIntPtr));
            Check(VirtualOpCode.RemUn, new[] { typeof(UIntPtr), typeof(UIntPtr) }, new object[] { unsignedNativeMax, new UIntPtr(10U) }, typeof(UIntPtr));
            Check(VirtualOpCode.DivUn, new[] { typeof(UIntPtr), typeof(UIntPtr) }, new object[] { new UIntPtr(1U), new UIntPtr(0U) }, typeof(UIntPtr));
            Check(VirtualOpCode.RemUn, new[] { typeof(UIntPtr), typeof(UIntPtr) }, new object[] { new UIntPtr(1U), new UIntPtr(0U) }, typeof(UIntPtr));
        }

        private static void CheckShiftCounts()
        {
            int[] counts = { -65, -64, -63, -33, -32, -31, -1, 0, 1, 31, 32, 33, 63, 64, 65, 95, 96, 127, 128, int.MinValue, int.MaxValue };
            object[][] values =
            {
                new object[] { typeof(int), unchecked((int)0x80000001U) },
                new object[] { typeof(uint), 0x80000001U },
                new object[] { typeof(long), unchecked((long)0x8000000000000001UL) },
                new object[] { typeof(ulong), 0x8000000000000001UL },
                new object[] { typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(unchecked((int)0x80000001U)) : new IntPtr(unchecked((long)0x8000000000000001UL)) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(0x80000001U) : new UIntPtr(0x8000000000000001UL) }
            };
            VirtualOpCode[] operations = { VirtualOpCode.Shl, VirtualOpCode.Shr, VirtualOpCode.ShrUn };

            foreach (VirtualOpCode opcode in operations)
            {
                foreach (object[] value in values)
                {
                    foreach (int count in counts)
                    {
                        Check(opcode, new[] { (Type)value[0], typeof(int) },
                            new object[] { value[1], count }, (Type)value[0]);
                    }
                }
            }
        }

        private static void CheckFloatingPointEdges()
        {
            object[][] values =
            {
                new object[] { typeof(float), 0f, -0f },
                new object[] { typeof(float), -0f, 1f },
                new object[] { typeof(float), float.Epsilon, 1f },
                new object[] { typeof(float), float.Epsilon, 0.5f },
                new object[] { typeof(float), 1f, 0f },
                new object[] { typeof(float), -1f, 0f },
                new object[] { typeof(float), float.MaxValue, float.MaxValue },
                new object[] { typeof(float), float.PositiveInfinity, 0f },
                new object[] { typeof(float), float.PositiveInfinity, float.NegativeInfinity },
                new object[] { typeof(float), float.PositiveInfinity, float.PositiveInfinity },
                new object[] { typeof(float), float.NaN, 2f },
                new object[] { typeof(double), 0d, -0d },
                new object[] { typeof(double), -0d, 1d },
                new object[] { typeof(double), double.Epsilon, 1d },
                new object[] { typeof(double), double.Epsilon, 0.5d },
                new object[] { typeof(double), 1d, 0d },
                new object[] { typeof(double), -1d, 0d },
                new object[] { typeof(double), double.MaxValue, double.MaxValue },
                new object[] { typeof(double), double.PositiveInfinity, 0d },
                new object[] { typeof(double), double.PositiveInfinity, double.NegativeInfinity },
                new object[] { typeof(double), double.PositiveInfinity, double.PositiveInfinity },
                new object[] { typeof(double), double.NaN, 2d }
            };
            VirtualOpCode[] operations =
            {
                VirtualOpCode.Add, VirtualOpCode.Sub, VirtualOpCode.Mul,
                VirtualOpCode.Div, VirtualOpCode.Rem
            };

            foreach (VirtualOpCode opcode in operations)
            {
                foreach (object[] pair in values)
                {
                    Type type = (Type)pair[0];
                    Check(opcode, new[] { type, type }, new[] { pair[1], pair[2] }, type);
                }

                Check(opcode, new[] { typeof(float), typeof(double) },
                    new object[] { float.Epsilon, double.Epsilon }, typeof(float), true);
                Check(opcode, new[] { typeof(double), typeof(float) },
                    new object[] { -0d, 1f }, typeof(double), true);
            }
        }

        private static void CheckConversions()
        {
            object[][] sources =
            {
                new object[] { typeof(bool), false },
                new object[] { typeof(bool), true },
                new object[] { typeof(sbyte), sbyte.MinValue },
                new object[] { typeof(sbyte), sbyte.MaxValue },
                new object[] { typeof(byte), (byte)0 },
                new object[] { typeof(byte), byte.MaxValue },
                new object[] { typeof(short), short.MinValue },
                new object[] { typeof(short), short.MaxValue },
                new object[] { typeof(ushort), (ushort)0 },
                new object[] { typeof(ushort), ushort.MaxValue },
                new object[] { typeof(char), '\0' },
                new object[] { typeof(char), '\uD7FF' },
                new object[] { typeof(char), '\uFFFF' },
                new object[] { typeof(int), int.MinValue },
                new object[] { typeof(int), -1 },
                new object[] { typeof(int), 0 },
                new object[] { typeof(int), int.MaxValue },
                new object[] { typeof(uint), 0U },
                new object[] { typeof(uint), uint.MaxValue },
                new object[] { typeof(long), long.MinValue },
                new object[] { typeof(long), -1L },
                new object[] { typeof(long), 0L },
                new object[] { typeof(long), long.MaxValue },
                new object[] { typeof(ulong), 0UL },
                new object[] { typeof(ulong), ulong.MaxValue },
                new object[] { typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue) },
                new object[] { typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(0U) : new UIntPtr(0UL) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue) },
                new object[] { typeof(float), float.Epsilon },
                new object[] { typeof(float), -0.5f },
                new object[] { typeof(float), -128.9f },
                new object[] { typeof(float), 127.9f },
                new object[] { typeof(float), 128f },
                new object[] { typeof(float), 255.9f },
                new object[] { typeof(float), 32767.9f },
                new object[] { typeof(float), 32768f },
                new object[] { typeof(float), 65535.9f },
                new object[] { typeof(float), 65536f },
                new object[] { typeof(float), 2147483520f },
                new object[] { typeof(float), 2147483648f },
                new object[] { typeof(float), 4294967040f },
                new object[] { typeof(float), 4294967296f },
                new object[] { typeof(float), (float)Math.Pow(2d, 63d) },
                new object[] { typeof(float), (float)Math.Pow(2d, 64d) },
                new object[] { typeof(float), 123.75f },
                new object[] { typeof(float), float.MaxValue },
                new object[] { typeof(float), float.PositiveInfinity },
                new object[] { typeof(float), float.NegativeInfinity },
                new object[] { typeof(float), float.NaN },
                new object[] { typeof(double), -123.75d },
                new object[] { typeof(double), -0.5d },
                new object[] { typeof(double), -0.999d },
                new object[] { typeof(double), -128.9d },
                new object[] { typeof(double), 127.9d },
                new object[] { typeof(double), 128d },
                new object[] { typeof(double), 255.9d },
                new object[] { typeof(double), 256d },
                new object[] { typeof(double), -32769d },
                new object[] { typeof(double), 32767.9d },
                new object[] { typeof(double), 32768d },
                new object[] { typeof(double), 65535.9d },
                new object[] { typeof(double), 65536d },
                new object[] { typeof(double), 2147483647d },
                new object[] { typeof(double), 2147483648d },
                new object[] { typeof(double), 4294967295d },
                new object[] { typeof(double), 4294967296d },
                new object[] { typeof(double), -Math.Pow(2d, 63d) },
                new object[] { typeof(double), Math.Pow(2d, 63d) },
                new object[] { typeof(double), Math.Pow(2d, 64d) - 2048d },
                new object[] { typeof(double), Math.Pow(2d, 64d) },
                new object[] { typeof(double), -1d },
                new object[] { typeof(double), 0d },
                new object[] { typeof(double), 0.5d },
                new object[] { typeof(double), 0.999d },
                new object[] { typeof(double), 1d },
                new object[] { typeof(double), double.Epsilon },
                new object[] { typeof(double), double.MaxValue },
                new object[] { typeof(double), double.NaN },
                new object[] { typeof(double), double.PositiveInfinity },
                new object[] { typeof(double), double.NegativeInfinity }
            };

            foreach (VirtualOpCode opcode in Conversions)
            {
                foreach (object[] source in sources)
                {
                    if (opcode == VirtualOpCode.ConvRUn &&
                        ((Type)source[0] == typeof(float) || (Type)source[0] == typeof(double)))
                        continue;

                    Check(opcode, new Type[] { (Type)source[0] }, new object[] { source[1] },
                        ConversionResultType(opcode));
                }
            }
        }

        private static void CheckBitwiseAndComparisons()
        {
            object[][] pairs =
            {
                new object[] { typeof(int), -1, 3 },
                new object[] { typeof(uint), uint.MaxValue, 3U },
                new object[] { typeof(long), -1L, 3L },
                new object[] { typeof(ulong), ulong.MaxValue, 3UL },
                new object[] { typeof(IntPtr), new IntPtr(-1), new IntPtr(3) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue), new UIntPtr(3U) },
                new object[] { typeof(float), float.NaN, 3f },
                new object[] { typeof(double), double.NaN, 3d }
            };

            foreach (VirtualOpCode opcode in BitwiseComparisons)
            {
                foreach (object[] pair in pairs)
                {
                    Type type = (Type)pair[0];
                    bool comparison = opcode == VirtualOpCode.Ceq || opcode == VirtualOpCode.Cgt ||
                        opcode == VirtualOpCode.CgtUn || opcode == VirtualOpCode.Clt || opcode == VirtualOpCode.CltUn;
                    if (!comparison && (type == typeof(float) || type == typeof(double)))
                        continue;

                    Type rightType = type;
                    object right = pair[2];
                    if (opcode == VirtualOpCode.Shl || opcode == VirtualOpCode.Shr || opcode == VirtualOpCode.ShrUn)
                    {
                        rightType = typeof(int);
                        right = 3;
                    }

                    Check(opcode, new Type[] { type, rightType }, new object[] { pair[1], right },
                        comparison ? typeof(int) : type);
                }
            }

            Check(VirtualOpCode.Ceq, new[] { typeof(int), typeof(int) }, new object[] { 42, 42 }, typeof(int));
            Check(VirtualOpCode.CltUn, new[] { typeof(uint), typeof(uint) }, new object[] { 1U, uint.MaxValue }, typeof(int));
            Check(VirtualOpCode.CgtUn, new[] { typeof(double), typeof(double) }, new object[] { double.NaN, 0d }, typeof(int));

            VirtualOpCode[] comparisons =
            {
                VirtualOpCode.Ceq, VirtualOpCode.Cgt, VirtualOpCode.CgtUn,
                VirtualOpCode.Clt, VirtualOpCode.CltUn
            };
            foreach (VirtualOpCode opcode in comparisons)
            {
                Check(opcode, new[] { typeof(float), typeof(double) },
                    new object[] { 1.5f, 1.25d }, typeof(int), true);
                Check(opcode, new[] { typeof(float), typeof(double) },
                    new object[] { float.NaN, 0d }, typeof(int), true);
            }

            object[][] edges =
            {
                new object[] { typeof(int), int.MinValue, int.MaxValue },
                new object[] { typeof(uint), 1U, uint.MaxValue },
                new object[] { typeof(long), long.MinValue, long.MaxValue },
                new object[] { typeof(ulong), 1UL, ulong.MaxValue },
                new object[] { typeof(IntPtr), IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue), new IntPtr(0) },
                new object[] { typeof(UIntPtr), new UIntPtr(1U), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue) },
                new object[] { typeof(float), -0f, 0f },
                new object[] { typeof(float), float.NegativeInfinity, float.PositiveInfinity },
                new object[] { typeof(float), float.NaN, 0f },
                new object[] { typeof(double), -0d, 0d },
                new object[] { typeof(double), double.NegativeInfinity, double.PositiveInfinity },
                new object[] { typeof(double), double.NaN, 0d }
            };
            foreach (VirtualOpCode opcode in comparisons)
            {
                foreach (object[] edge in edges)
                {
                    Type type = (Type)edge[0];
                    Check(opcode, new[] { type, type }, new[] { edge[1], edge[2] }, typeof(int));
                }
            }
        }

        private static void CheckUnary()
        {
            Check(VirtualOpCode.Neg, new[] { typeof(int) }, new object[] { int.MinValue }, typeof(int));
            Check(VirtualOpCode.Neg, new[] { typeof(uint) }, new object[] { uint.MaxValue }, typeof(uint));
            Check(VirtualOpCode.Neg, new[] { typeof(long) }, new object[] { long.MinValue }, typeof(long));
            Check(VirtualOpCode.Neg, new[] { typeof(ulong) }, new object[] { ulong.MaxValue }, typeof(ulong));
            Check(VirtualOpCode.Neg, new[] { typeof(IntPtr) }, new object[] { IntPtr.Size == 4 ? (object)new IntPtr(int.MinValue) : new IntPtr(long.MinValue) }, typeof(IntPtr));
            Check(VirtualOpCode.Neg, new[] { typeof(UIntPtr) },
                new object[] { IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue) },
                typeof(UIntPtr));
            Check(VirtualOpCode.Neg, new[] { typeof(float) }, new object[] { -1.5f }, typeof(float));
            Check(VirtualOpCode.Neg, new[] { typeof(float) }, new object[] { 0f }, typeof(float));
            Check(VirtualOpCode.Neg, new[] { typeof(double) }, new object[] { -1.5d }, typeof(double));
            Check(VirtualOpCode.Neg, new[] { typeof(double) }, new object[] { 0d }, typeof(double));
            Check(VirtualOpCode.Not, new[] { typeof(int) }, new object[] { -7 }, typeof(int));
            Check(VirtualOpCode.Not, new[] { typeof(uint) }, new object[] { uint.MaxValue }, typeof(uint));
            Check(VirtualOpCode.Not, new[] { typeof(long) }, new object[] { -7L }, typeof(long));
            Check(VirtualOpCode.Not, new[] { typeof(ulong) }, new object[] { ulong.MaxValue }, typeof(ulong));
            Check(VirtualOpCode.Not, new[] { typeof(IntPtr) }, new object[] { new IntPtr(-7) }, typeof(IntPtr));
            Check(VirtualOpCode.Not, new[] { typeof(UIntPtr) },
                new object[] { IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue) },
                typeof(UIntPtr));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { 1.25f }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { float.PositiveInfinity }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { float.NegativeInfinity }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { float.NaN }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { 1.25d }, typeof(double));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { double.PositiveInfinity }, typeof(double));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { double.NegativeInfinity }, typeof(double));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { double.NaN }, typeof(double));
        }

        private static void CheckSinglePrecisionStack()
        {
            float first = 16777216f;
            float second = 1f;
            float third = 16777216f;
            DynamicMethod reference = new DynamicMethod("MirrorVMClrSingleStack", typeof(float),
                new[] { typeof(float), typeof(float), typeof(float) }, typeof(NumericClrChecks).Module, true);
            ILGenerator il = reference.GetILGenerator();
            il.Emit(ClrOpCodes.Ldarg_0);
            il.Emit(ClrOpCodes.Ldarg_1);
            il.Emit(ClrOpCodes.Add);
            il.Emit(ClrOpCodes.Ldarg_2);
            il.Emit(ClrOpCodes.Sub);
            il.Emit(ClrOpCodes.Ret);
            double expected = (float)reference.Invoke(null, new object[] { first, second, third });
            byte[] code =
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)VirtualOpCode.LoadArgument, 1, 0,
                (byte)VirtualOpCode.Add,
                (byte)VirtualOpCode.LoadArgument, 2, 0,
                (byte)VirtualOpCode.Sub,
                (byte)VirtualOpCode.Ret
            };
            double actual = (double)VirtualMachine.Execute(
                VirtualMachine.Compile(code), new object[] { first, second, third });
            if (actual != expected)
            {
                throw new InvalidOperationException("Single stack arithmetic on " + Environment.Version +
                    " returned " + actual + "; CLR returned " + expected + ".");
            }
        }

        private static void Check(
            VirtualOpCode opcode, Type[] inputTypes, object[] values, Type resultType, bool requireSuccess = false)
        {
            object expected = null;
            object actual = null;
            string inputDescription = " with input " + values[0] +
                (values.Length > 1 ? " and " + values[1] : "");
            Exception clrError = null;
            Exception vmError = null;

            try
            {
                DynamicMethod reference = new DynamicMethod("MirrorVMClrReference", resultType, inputTypes,
                    typeof(NumericClrChecks).Module, true);
                ILGenerator il = reference.GetILGenerator();
                for (int index = 0; index < inputTypes.Length; index++)
                    il.Emit(index == 0 ? ClrOpCodes.Ldarg_0 : ClrOpCodes.Ldarg_1);
                il.Emit(GetClrOpCode(opcode));
                il.Emit(ClrOpCodes.Ret);
                expected = reference.Invoke(null, values);
            }
            catch (TargetInvocationException exception) { clrError = exception.InnerException; }
            catch (Exception exception) { clrError = exception; }

            try
            {
                byte[] code = inputTypes.Length == 1
                    ? new byte[] { (byte)VirtualOpCode.LoadArgument, 0, 0, (byte)opcode }
                    : new byte[] { (byte)VirtualOpCode.LoadArgument, 0, 0,
                        (byte)VirtualOpCode.LoadArgument, 1, 0, (byte)opcode };
                actual = VirtualMachine.Execute(VirtualMachine.Compile(code), values);
            }
            catch (Exception exception) { vmError = exception; }

            if (requireSuccess && (clrError != null || vmError != null))
                throw new InvalidOperationException(opcode + " on mixed Single/Double values failed: CLR " +
                    clrError + "; MirrorVM " + vmError + ".");

            if ((clrError == null ? null : clrError.GetType()) != (vmError == null ? null : vmError.GetType()))
                throw new InvalidOperationException(opcode + " on " + inputTypes[0].FullName + inputDescription +
                    " on " + Environment.Version + " differed: CLR threw " + clrError +
                    "; MirrorVM threw " + vmError + ".");

            if (clrError == null)
            {
                object normalizedExpected = Normalize(expected);
                object normalizedActual = NormalizeVm(actual, resultType);
                bool equal = resultType == typeof(float) || resultType == typeof(double)
                    ? FloatingResultsEqual(expected, actual, resultType)
                    : object.Equals(normalizedExpected, normalizedActual);
                if (!equal)
                    throw new InvalidOperationException(opcode + " on " + inputTypes[0].FullName + inputDescription +
                        " on " + Environment.Version + " returned " + normalizedActual +
                        "; CLR returned " + normalizedExpected + ".");
            }
        }

        private static Type ConversionResultType(VirtualOpCode opcode)
        {
            switch (opcode)
            {
                case VirtualOpCode.ConvI1: case VirtualOpCode.ConvOvfI1: case VirtualOpCode.ConvOvfI1Un: return typeof(sbyte);
                case VirtualOpCode.ConvU1: case VirtualOpCode.ConvOvfU1: case VirtualOpCode.ConvOvfU1Un: return typeof(byte);
                case VirtualOpCode.ConvI2: case VirtualOpCode.ConvOvfI2: case VirtualOpCode.ConvOvfI2Un: return typeof(short);
                case VirtualOpCode.ConvU2: case VirtualOpCode.ConvOvfU2: case VirtualOpCode.ConvOvfU2Un: return typeof(ushort);
                case VirtualOpCode.ConvI4: case VirtualOpCode.ConvOvfI4: case VirtualOpCode.ConvOvfI4Un: return typeof(int);
                case VirtualOpCode.ConvU4: case VirtualOpCode.ConvOvfU4: case VirtualOpCode.ConvOvfU4Un: return typeof(uint);
                case VirtualOpCode.ConvI8: case VirtualOpCode.ConvOvfI8: case VirtualOpCode.ConvOvfI8Un: return typeof(long);
                case VirtualOpCode.ConvU8: case VirtualOpCode.ConvOvfU8: case VirtualOpCode.ConvOvfU8Un: return typeof(ulong);
                case VirtualOpCode.ConvI: case VirtualOpCode.ConvOvfI: case VirtualOpCode.ConvOvfIUn: return typeof(IntPtr);
                case VirtualOpCode.ConvU: case VirtualOpCode.ConvOvfU: case VirtualOpCode.ConvOvfUUn: return typeof(UIntPtr);
                case VirtualOpCode.ConvR4: return typeof(float);
                default: return typeof(double);
            }
        }

        private static OpCode GetClrOpCode(VirtualOpCode opcode)
        {
            string name = opcode.ToString();
            if (name == "ConvRUn") name = "Conv_R_Un";
            else if (name.StartsWith("ConvOvf", StringComparison.Ordinal))
            {
                string target = name.Substring(7);
                if (target.EndsWith("Un", StringComparison.Ordinal))
                    target = target.Substring(0, target.Length - 2) + "_Un";
                name = "Conv_Ovf_" + target;
            }
            else if (name.StartsWith("Conv", StringComparison.Ordinal))
                name = "Conv_" + name.Substring(4);
            else if (name.EndsWith("OvfUn", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 5) + "_Ovf_Un";
            else if (name.EndsWith("Ovf", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 3) + "_Ovf";
            else if (name.EndsWith("Un", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 2) + "_Un";

            FieldInfo field = typeof(ClrOpCodes).GetField(name, BindingFlags.Public | BindingFlags.Static);
            return (OpCode)field.GetValue(null);
        }

        private static object Normalize(object value)
        {
            if (value is sbyte || value is byte || value is short || value is ushort || value is char)
                return Convert.ToInt32(value);
            if (value is uint) return unchecked((int)(uint)value);
            if (value is ulong) return unchecked((long)(ulong)value);
            if (value is UIntPtr)
            {
                UIntPtr native = (UIntPtr)value;
                return IntPtr.Size == 4 ? (object)new IntPtr(unchecked((int)native.ToUInt32()))
                    : new IntPtr(unchecked((long)native.ToUInt64()));
            }
            if (value is float) return (double)(float)value;
            return value;
        }

        private static object NormalizeVm(object value, Type resultType)
        {
            if (resultType == typeof(float))
                return (double)(float)(double)value;
            return value;
        }

        private static bool FloatingResultsEqual(object expected, object actual, Type resultType)
        {
            if (resultType == typeof(float))
            {
                float expectedValue = (float)expected;
                float actualValue = (float)(double)actual;
                if (float.IsNaN(expectedValue) || float.IsNaN(actualValue))
                    return float.IsNaN(expectedValue) && float.IsNaN(actualValue);
                if (expectedValue == 0f && actualValue == 0f)
                    return BitConverter.ToInt32(BitConverter.GetBytes(expectedValue), 0) ==
                        BitConverter.ToInt32(BitConverter.GetBytes(actualValue), 0);
                return expectedValue == actualValue;
            }

            double expectedDouble = (double)expected;
            double actualDouble = (double)actual;
            if (double.IsNaN(expectedDouble) || double.IsNaN(actualDouble))
                return double.IsNaN(expectedDouble) && double.IsNaN(actualDouble);
            if (expectedDouble == 0d && actualDouble == 0d)
                return BitConverter.ToInt64(BitConverter.GetBytes(expectedDouble), 0) ==
                    BitConverter.ToInt64(BitConverter.GetBytes(actualDouble), 0);
            return expectedDouble == actualDouble;
        }
    }
}
