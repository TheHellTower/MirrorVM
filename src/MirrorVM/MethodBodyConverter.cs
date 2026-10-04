// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace MirrorVM.Protector
{
    internal static class MethodBodyConverter
    {
        private static readonly Dictionary<Code, VirtualOpCode> ArithmeticOpCodes =
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
                { Code.Neg, VirtualOpCode.Neg }
            };

        private enum StackValueKind
        {
            Int32,
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
                reason = "requires an Int32, UInt32, or String return type";
                return false;
            }

            if (method.MethodSig.Params.Count > 256)
            {
                reason = "has more than 256 parameters";
                return false;
            }

            foreach (TypeSig parameterType in method.MethodSig.Params)
            {
                StackValueKind parameterKind;
                if (!TryGetValueKind(parameterType, out parameterKind))
                {
                    reason = "requires only Int32, UInt32, or String parameters";
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
                if (TryGetArgumentIndex(method, instruction, out argumentIndex))
                {
                    if (argumentIndex < 0 || argumentIndex >= method.MethodSig.Params.Count ||
                        argumentIndex > byte.MaxValue)
                    {
                        reason = "references an argument outside the bytecode range";
                        return false;
                    }

                    output.Add((byte)VirtualOpCode.LoadArgument);
                    output.Add((byte)argumentIndex);
                    StackValueKind argumentKind;
                    if (!TryGetValueKind(method.MethodSig.Params[argumentIndex], out argumentKind))
                    {
                        reason = "loads an unsupported method argument";
                        return false;
                    }

                    stack.Add(argumentKind);
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

                VirtualOpCode virtualOpCode;
                if (ArithmeticOpCodes.TryGetValue(code, out virtualOpCode))
                {
                    bool unary = virtualOpCode == VirtualOpCode.Neg;
                    int requiredDepth = unary ? 1 : 2;
                    if (stack.Count < requiredDepth)
                    {
                        reason = "has an arithmetic stack underflow";
                        return false;
                    }

                    if (stack[stack.Count - 1] != StackValueKind.Int32 ||
                        (!unary && stack[stack.Count - 2] != StackValueKind.Int32))
                    {
                        reason = "uses arithmetic with a non-Int32 value";
                        return false;
                    }

                    output.Add((byte)virtualOpCode);
                    if (!unary)
                    {
                        stack.RemoveAt(stack.Count - 1);
                        stack.RemoveAt(stack.Count - 1);
                        stack.Add(StackValueKind.Int32);
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

                    if (stack.Count != 1 || stack[0] != returnKind)
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
            if (type != null &&
                (type.FullName == "System.Int32" || type.FullName == "System.UInt32"))
            {
                kind = StackValueKind.Int32;
                return true;
            }

            if (type != null && type.FullName == "System.String")
            {
                kind = StackValueKind.String;
                return true;
            }

            kind = default(StackValueKind);
            return false;
        }

        private static bool TryGetArgumentIndex(MethodDef method, Instruction instruction, out int index)
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
    }
}
