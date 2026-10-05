// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;
using System;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace MirrorVM.Protector
{
    internal static class MethodBodyConverter
    {
        private static readonly Dictionary<Code, VirtualOpCode> NumericOpCodes =
            new Dictionary<Code, VirtualOpCode>
            {
                { Code.Add, VirtualOpCode.Add },
                { Code.Add_Ovf, VirtualOpCode.AddOvf },
                { Code.Add_Ovf_Un, VirtualOpCode.AddOvfUn },
                { Code.Sub, VirtualOpCode.Sub },
                { Code.Sub_Ovf, VirtualOpCode.SubOvf },
                { Code.Sub_Ovf_Un, VirtualOpCode.SubOvfUn },
                { Code.Mul, VirtualOpCode.Mul },
                { Code.Mul_Ovf, VirtualOpCode.MulOvf },
                { Code.Mul_Ovf_Un, VirtualOpCode.MulOvfUn },
                { Code.Div, VirtualOpCode.Div },
                { Code.Div_Un, VirtualOpCode.DivUn },
                { Code.Rem, VirtualOpCode.Rem },
                { Code.Rem_Un, VirtualOpCode.RemUn },
                { Code.Neg, VirtualOpCode.Neg },
                { Code.Not, VirtualOpCode.Not },
                { Code.And, VirtualOpCode.And },
                { Code.Or, VirtualOpCode.Or },
                { Code.Xor, VirtualOpCode.Xor },
                { Code.Shl, VirtualOpCode.Shl },
                { Code.Shr, VirtualOpCode.Shr },
                { Code.Shr_Un, VirtualOpCode.ShrUn },
                { Code.Ceq, VirtualOpCode.Ceq },
                { Code.Cgt, VirtualOpCode.Cgt },
                { Code.Cgt_Un, VirtualOpCode.CgtUn },
                { Code.Clt, VirtualOpCode.Clt },
                { Code.Clt_Un, VirtualOpCode.CltUn }
            };

        private static readonly Dictionary<Code, VirtualOpCode> ConversionOpCodes =
            new Dictionary<Code, VirtualOpCode>
            {
                { Code.Conv_I1, VirtualOpCode.ConvI1 }, { Code.Conv_U1, VirtualOpCode.ConvU1 },
                { Code.Conv_I2, VirtualOpCode.ConvI2 }, { Code.Conv_U2, VirtualOpCode.ConvU2 },
                { Code.Conv_I4, VirtualOpCode.ConvI4 }, { Code.Conv_U4, VirtualOpCode.ConvU4 },
                { Code.Conv_I8, VirtualOpCode.ConvI8 }, { Code.Conv_U8, VirtualOpCode.ConvU8 },
                { Code.Conv_I, VirtualOpCode.ConvI }, { Code.Conv_U, VirtualOpCode.ConvU },
                { Code.Conv_R4, VirtualOpCode.ConvR4 }, { Code.Conv_R8, VirtualOpCode.ConvR8 },
                { Code.Conv_R_Un, VirtualOpCode.ConvRUn },
                { Code.Conv_Ovf_I1, VirtualOpCode.ConvOvfI1 }, { Code.Conv_Ovf_U1, VirtualOpCode.ConvOvfU1 },
                { Code.Conv_Ovf_I2, VirtualOpCode.ConvOvfI2 }, { Code.Conv_Ovf_U2, VirtualOpCode.ConvOvfU2 },
                { Code.Conv_Ovf_I4, VirtualOpCode.ConvOvfI4 }, { Code.Conv_Ovf_U4, VirtualOpCode.ConvOvfU4 },
                { Code.Conv_Ovf_I8, VirtualOpCode.ConvOvfI8 }, { Code.Conv_Ovf_U8, VirtualOpCode.ConvOvfU8 },
                { Code.Conv_Ovf_I, VirtualOpCode.ConvOvfI }, { Code.Conv_Ovf_U, VirtualOpCode.ConvOvfU },
                { Code.Conv_Ovf_I1_Un, VirtualOpCode.ConvOvfI1Un }, { Code.Conv_Ovf_U1_Un, VirtualOpCode.ConvOvfU1Un },
                { Code.Conv_Ovf_I2_Un, VirtualOpCode.ConvOvfI2Un }, { Code.Conv_Ovf_U2_Un, VirtualOpCode.ConvOvfU2Un },
                { Code.Conv_Ovf_I4_Un, VirtualOpCode.ConvOvfI4Un }, { Code.Conv_Ovf_U4_Un, VirtualOpCode.ConvOvfU4Un },
                { Code.Conv_Ovf_I8_Un, VirtualOpCode.ConvOvfI8Un }, { Code.Conv_Ovf_U8_Un, VirtualOpCode.ConvOvfU8Un },
                { Code.Conv_Ovf_I_Un, VirtualOpCode.ConvOvfIUn }, { Code.Conv_Ovf_U_Un, VirtualOpCode.ConvOvfUUn }
            };

        private enum StackValueKind
        {
            Int32,
            Int64,
            NativeInt,
            Single,
            Double,
            Decimal,
            String
        }

        public static bool TryConvert(MethodDef method, out byte[] byteCode, out string reason)
        {
            byteCode = null;
            reason = null;

            if (!method.IsStatic || method.HasGenericParameters || !method.HasBody || method.MethodSig == null)
            {
                reason = "requires a non-generic static method body";
                return false;
            }

            StackValueKind returnKind;
            if (!TryGetValueKind(method.MethodSig.RetType, out returnKind))
            {
                reason = "has an unsupported return type";
                return false;
            }

            int parameterCount = method.MethodSig.Params.Count;
            if (parameterCount > 256)
            {
                reason = "has more than 256 parameters";
                return false;
            }

            StackValueKind[] parameterKinds = new StackValueKind[parameterCount];
            for (int index = 0; index < parameterCount; index++)
            {
                if (!TryGetValueKind(method.MethodSig.Params[index], out parameterKinds[index]))
                {
                    reason = "has an unsupported parameter type";
                    return false;
                }
            }

            CilBody body = method.Body;
            if (body.ExceptionHandlers.Count != 0 || body.Variables.Count != 0)
            {
                reason = "uses locals or exception handlers";
                return false;
            }

            List<byte> output = new List<byte>();
            List<StackValueKind> stack = new List<StackValueKind>();
            bool returned = false;

            for (int index = 0; index < body.Instructions.Count; index++)
            {
                Instruction instruction = body.Instructions[index];
                Code code = instruction.OpCode.Code;

                if (code == Code.Nop)
                {
                    continue;
                }

                int argumentIndex;
                if (TryGetArgumentIndex(instruction, out argumentIndex))
                {
                    if (argumentIndex < 0 || argumentIndex >= parameterKinds.Length)
                    {
                        reason = "references an argument outside the bytecode range";
                        return false;
                    }

                    output.Add((byte)VirtualOpCode.LoadArgument);
                    output.Add((byte)argumentIndex);
                    stack.Add(parameterKinds[argumentIndex]);
                    continue;
                }

                int constant;
                if (TryGetInt32Constant(instruction, out constant))
                {
                    output.Add((byte)VirtualOpCode.LoadInt32);
                    AddInt32(output, constant);
                    stack.Add(StackValueKind.Int32);
                    continue;
                }

                long longConstant;
                if (TryGetInt64Constant(instruction, out longConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadInt64);
                    AddInt64(output, longConstant);
                    stack.Add(StackValueKind.Int64);
                    continue;
                }

                float singleConstant;
                if (TryGetSingleConstant(instruction, out singleConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadSingle);
                    AddSingle(output, singleConstant);
                    stack.Add(StackValueKind.Single);
                    continue;
                }

                double doubleConstant;
                if (TryGetDoubleConstant(instruction, out doubleConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadDouble);
                    AddDouble(output, doubleConstant);
                    stack.Add(StackValueKind.Double);
                    continue;
                }

                if (code == Code.Ldstr)
                {
                    string value = instruction.Operand as string;
                    if (value == null)
                    {
                        reason = "contains an invalid ldstr operand";
                        return false;
                    }

                    byte[] utf8 = Encoding.UTF8.GetBytes(value);
                    output.Add((byte)VirtualOpCode.Ldstr);
                    AddInt32(output, utf8.Length);
                    output.AddRange(utf8);
                    stack.Add(StackValueKind.String);
                    continue;
                }

                if (code == Code.Ckfinite)
                {
                    if (stack.Count == 0 || !IsFloatingPoint(stack[stack.Count - 1]))
                    {
                        reason = "uses ckfinite with a non-floating stack value";
                        return false;
                    }

                    output.Add((byte)VirtualOpCode.Ckfinite);
                    continue;
                }

                VirtualOpCode conversion;
                if (ConversionOpCodes.TryGetValue(code, out conversion))
                {
                    if (stack.Count == 0 || !IsNumeric(stack[stack.Count - 1]))
                    {
                        reason = "uses a numeric conversion with a non-numeric stack value";
                        return false;
                    }

                    output.Add((byte)conversion);
                    stack[stack.Count - 1] = GetConversionResultKind(conversion);
                    continue;
                }

                VirtualOpCode virtualOpCode;
                if (NumericOpCodes.TryGetValue(code, out virtualOpCode))
                {
                    bool unary = virtualOpCode == VirtualOpCode.Neg || virtualOpCode == VirtualOpCode.Not;
                    int requiredDepth = unary ? 1 : 2;
                    if (stack.Count < requiredDepth)
                    {
                        reason = "has an arithmetic stack underflow";
                        return false;
                    }

                    StackValueKind resultKind;
                    if (!TryGetNumericResultKind(
                        virtualOpCode,
                        stack[stack.Count - 1],
                        unary ? stack[stack.Count - 1] : stack[stack.Count - 2],
                        out resultKind))
                    {
                        reason = "uses arithmetic with incompatible CIL stack kinds";
                        return false;
                    }

                    output.Add((byte)virtualOpCode);
                    if (!unary)
                    {
                        stack.RemoveAt(stack.Count - 1);
                        stack.RemoveAt(stack.Count - 1);
                        stack.Add(resultKind);
                    }
                    else
                    {
                        stack[stack.Count - 1] = resultKind;
                    }

                    continue;
                }

                if (code == Code.Ret)
                {
                    if (index != body.Instructions.Count - 1)
                    {
                        reason = "has instructions after its return";
                        return false;
                    }

                    if (stack.Count != 1 || !AreStackKindsCompatible(stack[0], returnKind))
                    {
                        reason = "does not return one value matching its declared type";
                        return false;
                    }

                    returned = true;
                    continue;
                }

                reason = "contains unsupported CIL opcode '" + instruction.OpCode.Name + "'";
                return false;
            }

            if (!returned)
            {
                reason = "has no final return instruction";
                return false;
            }

            byteCode = output.ToArray();
            return true;
        }

        private static bool TryGetValueKind(TypeSig type, out StackValueKind kind)
        {
            if (type == null)
            {
                kind = default(StackValueKind);
                return false;
            }

            string fullName = type.FullName;
            if (fullName == "System.Boolean" || fullName == "System.SByte" ||
                fullName == "System.Byte" || fullName == "System.Int16" ||
                fullName == "System.UInt16" || fullName == "System.Char" ||
                fullName == "System.Int32" || fullName == "System.UInt32")
            {
                kind = StackValueKind.Int32;
                return true;
            }

            if (fullName == "System.Int64" || fullName == "System.UInt64")
            {
                kind = StackValueKind.Int64;
                return true;
            }

            if (fullName == "System.IntPtr" || fullName == "System.UIntPtr")
            {
                kind = StackValueKind.NativeInt;
                return true;
            }

            if (fullName == "System.Single")
            {
                kind = StackValueKind.Single;
                return true;
            }

            if (fullName == "System.Double")
            {
                kind = StackValueKind.Double;
                return true;
            }

            if (fullName == "System.Decimal")
            {
                kind = StackValueKind.Decimal;
                return true;
            }

            if (fullName == "System.String")
            {
                kind = StackValueKind.String;
                return true;
            }

            kind = default(StackValueKind);
            return false;
        }

        private static bool TryGetNumericResultKind(
            VirtualOpCode opcode,
            StackValueKind left,
            StackValueKind right,
            out StackValueKind result)
        {
            if (opcode == VirtualOpCode.Neg || opcode == VirtualOpCode.Not)
            {
                result = left;
                return opcode == VirtualOpCode.Not ? IsInteger(left) : IsNumeric(left);
            }

            if (opcode == VirtualOpCode.Shl || opcode == VirtualOpCode.Shr || opcode == VirtualOpCode.ShrUn)
            {
                result = left;
                return IsInteger(left) && right == StackValueKind.Int32;
            }

            bool comparison = opcode == VirtualOpCode.Ceq || opcode == VirtualOpCode.Cgt ||
                opcode == VirtualOpCode.CgtUn || opcode == VirtualOpCode.Clt || opcode == VirtualOpCode.CltUn;
            if (comparison && AreCompatibleNumericKinds(left, right))
            {
                result = StackValueKind.Int32;
                return true;
            }

            if (IsFloatingPoint(left) || IsFloatingPoint(right))
            {
                if (!IsFloatingPoint(left) || !IsFloatingPoint(right) ||
                    (opcode != VirtualOpCode.Add && opcode != VirtualOpCode.Sub &&
                     opcode != VirtualOpCode.Mul && opcode != VirtualOpCode.Div &&
                     opcode != VirtualOpCode.Rem))
                {
                    result = default(StackValueKind);
                    return false;
                }

                result = left == StackValueKind.Double || right == StackValueKind.Double
                    ? StackValueKind.Double
                    : StackValueKind.Single;
                return true;
            }

            if (left == right && IsInteger(left))
            {
                result = left;
                return true;
            }

            if ((left == StackValueKind.NativeInt && right == StackValueKind.Int32) ||
                (left == StackValueKind.Int32 && right == StackValueKind.NativeInt))
            {
                result = comparison ? StackValueKind.Int32 : StackValueKind.NativeInt;
                return true;
            }

            result = default(StackValueKind);
            return false;
        }

        private static bool AreCompatibleNumericKinds(StackValueKind left, StackValueKind right)
        {
            if (IsInteger(left) && IsInteger(right))
            {
                return left == right || left == StackValueKind.NativeInt && right == StackValueKind.Int32 ||
                    left == StackValueKind.Int32 && right == StackValueKind.NativeInt;
            }

            return IsFloatingPoint(left) && IsFloatingPoint(right);
        }

        private static StackValueKind GetConversionResultKind(VirtualOpCode opcode)
        {
            switch (opcode)
            {
                case VirtualOpCode.ConvI1: case VirtualOpCode.ConvU1:
                case VirtualOpCode.ConvI2: case VirtualOpCode.ConvU2:
                case VirtualOpCode.ConvI4: case VirtualOpCode.ConvU4:
                case VirtualOpCode.ConvOvfI1: case VirtualOpCode.ConvOvfU1:
                case VirtualOpCode.ConvOvfI2: case VirtualOpCode.ConvOvfU2:
                case VirtualOpCode.ConvOvfI4: case VirtualOpCode.ConvOvfU4:
                case VirtualOpCode.ConvOvfI1Un: case VirtualOpCode.ConvOvfU1Un:
                case VirtualOpCode.ConvOvfI2Un: case VirtualOpCode.ConvOvfU2Un:
                case VirtualOpCode.ConvOvfI4Un: case VirtualOpCode.ConvOvfU4Un:
                    return StackValueKind.Int32;
                case VirtualOpCode.ConvI8: case VirtualOpCode.ConvU8:
                case VirtualOpCode.ConvOvfI8: case VirtualOpCode.ConvOvfU8:
                case VirtualOpCode.ConvOvfI8Un: case VirtualOpCode.ConvOvfU8Un:
                    return StackValueKind.Int64;
                case VirtualOpCode.ConvI: case VirtualOpCode.ConvU:
                case VirtualOpCode.ConvOvfI: case VirtualOpCode.ConvOvfU:
                case VirtualOpCode.ConvOvfIUn: case VirtualOpCode.ConvOvfUUn:
                    return StackValueKind.NativeInt;
                case VirtualOpCode.ConvR4:
                    return StackValueKind.Single;
                case VirtualOpCode.ConvR8: case VirtualOpCode.ConvRUn:
                    return StackValueKind.Double;
                default:
                    return default(StackValueKind);
            }
        }

        private static bool IsFloatingPoint(StackValueKind kind)
        {
            return kind == StackValueKind.Single || kind == StackValueKind.Double;
        }

        private static bool IsNumeric(StackValueKind kind)
        {
            return IsInteger(kind) || IsFloatingPoint(kind);
        }

        private static bool IsInteger(StackValueKind kind)
        {
            return kind == StackValueKind.Int32 || kind == StackValueKind.Int64 ||
                kind == StackValueKind.NativeInt;
        }

        private static bool AreStackKindsCompatible(StackValueKind actual, StackValueKind expected)
        {
            return actual == expected || (IsFloatingPoint(actual) && IsFloatingPoint(expected));
        }

        private static bool TryGetArgumentIndex(Instruction instruction, out int index)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldarg_0: index = 0; return true;
                case Code.Ldarg_1: index = 1; return true;
                case Code.Ldarg_2: index = 2; return true;
                case Code.Ldarg_3: index = 3; return true;
                case Code.Ldarg:
                case Code.Ldarg_S:
                    Parameter parameter = instruction.Operand as Parameter;
                    if (parameter != null && parameter.IsNormalMethodParameter)
                    {
                        index = parameter.Index;
                        return true;
                    }
                    break;
            }

            index = -1;
            return false;
        }

        private static bool TryGetInt32Constant(Instruction instruction, out int value)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldc_I4_M1: value = -1; return true;
                case Code.Ldc_I4_0: value = 0; return true;
                case Code.Ldc_I4_1: value = 1; return true;
                case Code.Ldc_I4_2: value = 2; return true;
                case Code.Ldc_I4_3: value = 3; return true;
                case Code.Ldc_I4_4: value = 4; return true;
                case Code.Ldc_I4_5: value = 5; return true;
                case Code.Ldc_I4_6: value = 6; return true;
                case Code.Ldc_I4_7: value = 7; return true;
                case Code.Ldc_I4_8: value = 8; return true;
                case Code.Ldc_I4:
                    if (instruction.Operand is int)
                    {
                        value = (int)instruction.Operand;
                        return true;
                    }
                    break;
                case Code.Ldc_I4_S:
                    if (instruction.Operand is sbyte)
                    {
                        value = (sbyte)instruction.Operand;
                        return true;
                    }
                    break;
            }

            value = 0;
            return false;
        }

        private static bool TryGetInt64Constant(Instruction instruction, out long value)
        {
            if (instruction.OpCode.Code == Code.Ldc_I8 && instruction.Operand is long)
            {
                value = (long)instruction.Operand;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool TryGetSingleConstant(Instruction instruction, out float value)
        {
            if (instruction.OpCode.Code == Code.Ldc_R4 && instruction.Operand is float)
            {
                value = (float)instruction.Operand;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool TryGetDoubleConstant(Instruction instruction, out double value)
        {
            if (instruction.OpCode.Code == Code.Ldc_R8 && instruction.Operand is double)
            {
                value = (double)instruction.Operand;
                return true;
            }

            value = 0;
            return false;
        }

        private static void AddInt32(List<byte> output, int value)
        {
            unchecked
            {
                output.Add((byte)value);
                output.Add((byte)(value >> 8));
                output.Add((byte)(value >> 16));
                output.Add((byte)(value >> 24));
            }
        }

        private static void AddInt64(List<byte> output, long value)
        {
            AddInt32(output, unchecked((int)value));
            AddInt32(output, unchecked((int)(value >> 32)));
        }

        private static void AddSingle(List<byte> output, float value)
        {
            AddInt32(output, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        private static void AddDouble(List<byte> output, double value)
        {
            AddInt64(output, BitConverter.ToInt64(BitConverter.GetBytes(value), 0));
        }
    }
}
