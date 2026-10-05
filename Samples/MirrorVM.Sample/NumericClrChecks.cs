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
            Check(VirtualOpCode.MulOvfUn, new[] { typeof(ulong), typeof(ulong) }, new object[] { ulong.MaxValue, 2UL }, typeof(ulong));
        }

        private static void CheckConversions()
        {
            object[][] sources =
            {
                new object[] { typeof(sbyte), (sbyte)-123 },
                new object[] { typeof(byte), (byte)251 },
                new object[] { typeof(short), (short)-32000 },
                new object[] { typeof(ushort), (ushort)65000 },
                new object[] { typeof(char), '\uD7FF' },
                new object[] { typeof(int), -1 },
                new object[] { typeof(uint), uint.MaxValue },
                new object[] { typeof(long), -1L },
                new object[] { typeof(ulong), ulong.MaxValue },
                new object[] { typeof(IntPtr), new IntPtr(-1) },
                new object[] { typeof(UIntPtr), IntPtr.Size == 4 ? (object)new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue) },
                new object[] { typeof(float), 123.75f },
                new object[] { typeof(double), -123.75d },
                new object[] { typeof(double), double.MaxValue },
                new object[] { typeof(double), double.NaN },
                new object[] { typeof(double), double.PositiveInfinity }
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
            Check(VirtualOpCode.Neg, new[] { typeof(double) }, new object[] { -1.5d }, typeof(double));
            Check(VirtualOpCode.Not, new[] { typeof(int) }, new object[] { -7 }, typeof(int));
            Check(VirtualOpCode.Not, new[] { typeof(long) }, new object[] { -7L }, typeof(long));
            Check(VirtualOpCode.Not, new[] { typeof(IntPtr) }, new object[] { new IntPtr(-7) }, typeof(IntPtr));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { 1.25f }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { float.PositiveInfinity }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(float) }, new object[] { float.NaN }, typeof(float));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { double.PositiveInfinity }, typeof(double));
            Check(VirtualOpCode.Ckfinite, new[] { typeof(double) }, new object[] { double.NaN }, typeof(double));
        }

        private static void Check(
            VirtualOpCode opcode, Type[] inputTypes, object[] values, Type resultType, bool requireSuccess = false)
        {
            object expected = null;
            object actual = null;
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
                    ? new byte[] { (byte)VirtualOpCode.LoadArgument, 0, (byte)opcode }
                    : new byte[] { (byte)VirtualOpCode.LoadArgument, 0,
                        (byte)VirtualOpCode.LoadArgument, 1, (byte)opcode };
                actual = VirtualMachine.Execute(code, values);
            }
            catch (Exception exception) { vmError = exception; }

            if (requireSuccess && (clrError != null || vmError != null))
                throw new InvalidOperationException(opcode + " on mixed Single/Double values failed: CLR " +
                    clrError + "; MirrorVM " + vmError + ".");

            if ((clrError == null ? null : clrError.GetType()) != (vmError == null ? null : vmError.GetType()))
                throw new InvalidOperationException(opcode + " on " + inputTypes[0].FullName +
                    " on " + Environment.Version + " differed: CLR threw " + clrError +
                    "; MirrorVM threw " + vmError + ".");

            if (clrError == null)
            {
                object normalizedExpected = Normalize(expected);
                object normalizedActual = NormalizeVm(actual, resultType);
                if (!object.Equals(normalizedExpected, normalizedActual))
                    throw new InvalidOperationException(opcode + " on " + inputTypes[0].FullName +
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
    }
}
