// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;
using System;
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
            String,
            Null
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

            bool returnsVoid = method.MethodSig.RetType.ElementType == ElementType.Void;
            StackValueKind returnKind = default(StackValueKind);
            if (!returnsVoid && !TryGetValueKind(method.MethodSig.RetType, out returnKind))
            {
                reason = "has an unsupported return type";
                return false;
            }

            int parameterCount = method.MethodSig.Params.Count;
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
            if (body.ExceptionHandlers.Count != 0)
            {
                reason = "uses exception handlers";
                return false;
            }

            StackValueKind[] localKinds = new StackValueKind[body.Variables.Count];
            for (int index = 0; index < localKinds.Length; index++)
            {
                if (!TryGetValueKind(body.Variables[index].Type, out localKinds[index]))
                {
                    reason = "uses an unsupported local type";
                    return false;
                }
            }

            Dictionary<Instruction, int> instructionIndices = new Dictionary<Instruction, int>();
            for (int index = 0; index < body.Instructions.Count; index++)
            {
                instructionIndices.Add(body.Instructions[index], index);
            }

            List<byte> output = new List<byte>();

            for (int index = 0; index < body.Instructions.Count; index++)
            {
                Instruction instruction = body.Instructions[index];
                Code code = instruction.OpCode.Code;

                if (code == Code.Nop)
                {
                    output.Add((byte)VirtualOpCode.Nop);
                    continue;
                }

                if (code == Code.Ldnull)
                {
                    output.Add((byte)VirtualOpCode.Ldnull);
                    continue;
                }

                VirtualOpCode branchOpcode;
                if (TryGetBranchOpcode(code, out branchOpcode))
                {
                    output.Add((byte)branchOpcode);
                    Instruction target = instruction.Operand as Instruction;
                    int targetIndex;
                    if (target == null || !instructionIndices.TryGetValue(target, out targetIndex))
                    {
                        reason = "contains a branch target outside the method";
                        return false;
                    }

                    AddInt32(output, targetIndex);
                    continue;
                }

                if (code == Code.Switch)
                {
                    Instruction[] targets = instruction.Operand as Instruction[];
                    if (targets == null)
                    {
                        reason = "contains an invalid switch target table";
                        return false;
                    }

                    output.Add((byte)VirtualOpCode.Switch);
                    AddInt32(output, targets.Length);
                    for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                    {
                        int mappedIndex;
                        if (targets[targetIndex] == null ||
                            !instructionIndices.TryGetValue(targets[targetIndex], out mappedIndex))
                        {
                            reason = "contains a switch target outside the method";
                            return false;
                        }

                        AddInt32(output, mappedIndex);
                    }

                    continue;
                }

                bool storeLocal;
                int localIndex;
                if (TryGetLocalIndex(instruction, out storeLocal, out localIndex))
                {
                    if (localIndex < 0 || localIndex >= localKinds.Length || localIndex > ushort.MaxValue)
                    {
                        reason = "references a local outside the bytecode range";
                        return false;
                    }

                    output.Add((byte)(storeLocal ? VirtualOpCode.Stloc : VirtualOpCode.Ldloc));
                    AddUInt16(output, localIndex);
                    output.Add((byte)localKinds[localIndex]);
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
                    AddUInt16(output, argumentIndex);
                    continue;
                }

                int constant;
                if (TryGetInt32Constant(instruction, out constant))
                {
                    output.Add((byte)VirtualOpCode.LoadInt32);
                    AddInt32(output, constant);
                    continue;
                }

                long longConstant;
                if (TryGetInt64Constant(instruction, out longConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadInt64);
                    AddInt64(output, longConstant);
                    continue;
                }

                float singleConstant;
                if (TryGetSingleConstant(instruction, out singleConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadSingle);
                    AddSingle(output, singleConstant);
                    continue;
                }

                double doubleConstant;
                if (TryGetDoubleConstant(instruction, out doubleConstant))
                {
                    output.Add((byte)VirtualOpCode.LoadDouble);
                    AddDouble(output, doubleConstant);
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

                    output.Add((byte)VirtualOpCode.Ldstr);
                    AddInt32(output, value.Length * 2);
                    for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
                    {
                        char character = value[characterIndex];
                        output.Add((byte)character);
                        output.Add((byte)(character >> 8));
                    }
                    continue;
                }

                if (code == Code.Ckfinite)
                {
                    output.Add((byte)VirtualOpCode.Ckfinite);
                    continue;
                }

                VirtualOpCode conversion;
                if (ConversionOpCodes.TryGetValue(code, out conversion))
                {
                    output.Add((byte)conversion);
                    continue;
                }

                VirtualOpCode virtualOpCode;
                if (NumericOpCodes.TryGetValue(code, out virtualOpCode))
                {
                    output.Add((byte)virtualOpCode);
                    continue;
                }

                if (code == Code.Ret)
                {
                    output.Add((byte)VirtualOpCode.Ret);
                    continue;
                }

                reason = "contains unsupported CIL opcode '" + instruction.OpCode.Name + "'";
                return false;
            }

            if (!ValidateControlFlow(body, parameterKinds, localKinds, returnKind, returnsVoid, instructionIndices, out reason))
            {
                return false;
            }

            byteCode = output.ToArray();
            return true;
        }

        private static bool ValidateControlFlow(
            CilBody body,
            StackValueKind[] parameterKinds,
            StackValueKind[] localKinds,
            StackValueKind returnKind,
            bool returnsVoid,
            Dictionary<Instruction, int> instructionIndices,
            out string reason)
        {
            reason = null;
            int instructionCount = body.Instructions.Count;
            if (instructionCount == 0)
            {
                reason = "has an empty method body";
                return false;
            }

            List<StackValueKind>[] stacks = new List<StackValueKind>[instructionCount];
            bool[][] assignedLocals = new bool[instructionCount][];
            Queue<int> pending = new Queue<int>();
            stacks[0] = new List<StackValueKind>();
            assignedLocals[0] = new bool[localKinds.Length];
            if (body.InitLocals)
            {
                for (int index = 0; index < localKinds.Length; index++)
                {
                    assignedLocals[0][index] = true;
                }
            }

            pending.Enqueue(0);
            int maximumDepth = 0;
            while (pending.Count != 0)
            {
                int index = pending.Dequeue();
                Instruction instruction = body.Instructions[index];
                Code code = instruction.OpCode.Code;
                List<StackValueKind> stack = new List<StackValueKind>(stacks[index]);
                bool[] assigned = (bool[])assignedLocals[index].Clone();
                if (!ApplyStackEffect(
                    instruction,
                    parameterKinds,
                    localKinds,
                    returnKind,
                    returnsVoid,
                    stack,
                    assigned,
                    body.InitLocals,
                    out reason))
                {
                    return false;
                }

                maximumDepth = Math.Max(maximumDepth, stack.Count);
                VirtualOpCode branchOpcode;
                bool isBranch = TryGetBranchOpcode(code, out branchOpcode);
                if (code == Code.Ret)
                {
                    continue;
                }

                if (isBranch)
                {
                    int target = instructionIndices[(Instruction)instruction.Operand];
                    if (!MergeFlowState(target, stack, assigned, stacks, assignedLocals, pending, out reason))
                    {
                        return false;
                    }

                    if (branchOpcode == VirtualOpCode.Br)
                    {
                        continue;
                    }
                }
                else if (code == Code.Switch)
                {
                    Instruction[] targets = (Instruction[])instruction.Operand;
                    for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                    {
                        if (!MergeFlowState(
                            instructionIndices[targets[targetIndex]],
                            stack,
                            assigned,
                            stacks,
                            assignedLocals,
                            pending,
                            out reason))
                        {
                            return false;
                        }
                    }
                }

                if (index + 1 == instructionCount)
                {
                    reason = "has a reachable path that falls through the end of the method";
                    return false;
                }

                if (!MergeFlowState(index + 1, stack, assigned, stacks, assignedLocals, pending, out reason))
                {
                    return false;
                }
            }

            if (maximumDepth > body.MaxStack)
            {
                reason = "exceeds its declared CIL maxstack";
                return false;
            }

            return true;
        }

        private static bool ApplyStackEffect(
            Instruction instruction,
            StackValueKind[] parameterKinds,
            StackValueKind[] localKinds,
            StackValueKind returnKind,
            bool returnsVoid,
            List<StackValueKind> stack,
            bool[] assigned,
            bool initLocals,
            out string reason)
        {
            reason = null;
            int argumentIndex;
            if (TryGetArgumentIndex(instruction, out argumentIndex))
            {
                if (argumentIndex < 0 || argumentIndex >= parameterKinds.Length)
                {
                    reason = "references an argument outside the bytecode range";
                    return false;
                }

                stack.Add(parameterKinds[argumentIndex]);
                return true;
            }

            int constant;
            if (TryGetInt32Constant(instruction, out constant))
            {
                stack.Add(StackValueKind.Int32);
                return true;
            }

            long longConstant;
            if (TryGetInt64Constant(instruction, out longConstant))
            {
                stack.Add(StackValueKind.Int64);
                return true;
            }

            float singleConstant;
            if (TryGetSingleConstant(instruction, out singleConstant))
            {
                stack.Add(StackValueKind.Single);
                return true;
            }

            double doubleConstant;
            if (TryGetDoubleConstant(instruction, out doubleConstant))
            {
                stack.Add(StackValueKind.Double);
                return true;
            }

            Code code = instruction.OpCode.Code;
            if (code == Code.Ldstr)
            {
                stack.Add(StackValueKind.String);
                return true;
            }

            if (code == Code.Ldnull)
            {
                stack.Add(StackValueKind.Null);
                return true;
            }

            if (code == Code.Nop)
            {
                return true;
            }

            bool storeLocal;
            int localIndex;
            if (TryGetLocalIndex(instruction, out storeLocal, out localIndex))
            {
                if (storeLocal)
                {
                    if (stack.Count == 0 || !AreStackKindsCompatible(stack[stack.Count - 1], localKinds[localIndex]))
                    {
                        reason = "stores a value with an incompatible local kind";
                        return false;
                    }

                    stack.RemoveAt(stack.Count - 1);
                    assigned[localIndex] = true;
                    return true;
                }

                if (!initLocals && !assigned[localIndex])
                {
                    reason = "reads a local before it is assigned";
                    return false;
                }

                stack.Add(localKinds[localIndex]);
                return true;
            }

            if (code == Code.Ckfinite)
            {
                if (stack.Count == 0 || !IsFloatingPoint(stack[stack.Count - 1]))
                {
                    reason = "uses ckfinite with a non-floating stack value";
                    return false;
                }

                return true;
            }

            VirtualOpCode conversion;
            if (ConversionOpCodes.TryGetValue(code, out conversion))
            {
                if (stack.Count == 0 || !IsNumeric(stack[stack.Count - 1]))
                {
                    reason = "uses a numeric conversion with a non-numeric stack value";
                    return false;
                }

                stack[stack.Count - 1] = GetConversionResultKind(conversion);
                return true;
            }

            VirtualOpCode virtualOpCode;
            if (NumericOpCodes.TryGetValue(code, out virtualOpCode))
            {
                bool unary = virtualOpCode == VirtualOpCode.Neg || virtualOpCode == VirtualOpCode.Not;
                int requiredDepth = unary ? 1 : 2;
                if (stack.Count < requiredDepth)
                {
                    reason = "has an evaluation stack underflow";
                    return false;
                }

                StackValueKind resultKind;
                if (!TryGetNumericResultKind(
                    virtualOpCode,
                    stack[stack.Count - 1],
                    unary ? stack[stack.Count - 1] : stack[stack.Count - 2],
                    out resultKind))
                {
                    reason = "uses an opcode with incompatible CIL stack kinds";
                    return false;
                }

                if (unary)
                {
                    stack[stack.Count - 1] = resultKind;
                }
                else
                {
                    stack.RemoveAt(stack.Count - 1);
                    stack.RemoveAt(stack.Count - 1);
                    stack.Add(resultKind);
                }

                return true;
            }

            VirtualOpCode branch;
            if (TryGetBranchOpcode(code, out branch))
            {
                if (branch == VirtualOpCode.Br)
                {
                    return true;
                }

                int requiredDepth = branch == VirtualOpCode.Brtrue || branch == VirtualOpCode.Brfalse ? 1 : 2;
                if (stack.Count < requiredDepth)
                {
                    reason = "has a branch evaluation stack underflow";
                    return false;
                }

                if (requiredDepth == 1)
                {
                    StackValueKind value = stack[stack.Count - 1];
                    if (!IsInteger(value) && value != StackValueKind.String && value != StackValueKind.Null)
                    {
                        reason = "branches on an unsupported CIL stack kind";
                        return false;
                    }

                    stack.RemoveAt(stack.Count - 1);
                    return true;
                }

                StackValueKind right = stack[stack.Count - 1];
                StackValueKind left = stack[stack.Count - 2];
                bool referenceEquality = (branch == VirtualOpCode.Beq || branch == VirtualOpCode.BneUn) &&
                    IsReference(left) && IsReference(right);
                bool compatible = branch == VirtualOpCode.Beq || branch == VirtualOpCode.BneUn
                    ? referenceEquality || AreCompatibleNumericKinds(left, right)
                    : AreCompatibleNumericKinds(left, right);
                if (!compatible)
                {
                    reason = "compares incompatible CIL stack kinds in a branch";
                    return false;
                }

                stack.RemoveAt(stack.Count - 1);
                stack.RemoveAt(stack.Count - 1);
                return true;
            }

            if (code == Code.Switch)
            {
                if (stack.Count == 0 || stack[stack.Count - 1] != StackValueKind.Int32)
                {
                    reason = "uses switch with a non-Int32 selector";
                    return false;
                }

                stack.RemoveAt(stack.Count - 1);
                return true;
            }

            if (code == Code.Ret)
            {
                if (returnsVoid ? stack.Count != 0 :
                    stack.Count != 1 || !AreStackKindsCompatible(stack[0], returnKind))
                {
                    reason = returnsVoid
                        ? "returns with a non-empty evaluation stack"
                        : "does not return one value matching its declared type";
                    return false;
                }

                return true;
            }

            reason = "contains an unsupported CIL opcode '" + instruction.OpCode.Name + "'";
            return false;
        }

        private static bool MergeFlowState(
            int target,
            List<StackValueKind> incomingStack,
            bool[] incomingLocals,
            List<StackValueKind>[] stacks,
            bool[][] assignedLocals,
            Queue<int> pending,
            out string reason)
        {
            reason = null;
            if (stacks[target] == null)
            {
                stacks[target] = new List<StackValueKind>(incomingStack);
                assignedLocals[target] = (bool[])incomingLocals.Clone();
                pending.Enqueue(target);
                return true;
            }

            List<StackValueKind> current = stacks[target];
            if (current.Count != incomingStack.Count)
            {
                reason = "has incompatible stack heights at a control-flow merge";
                return false;
            }

            bool changed = false;
            for (int index = 0; index < current.Count; index++)
            {
                if (current[index] == incomingStack[index])
                {
                    continue;
                }

                if (IsFloatingPoint(current[index]) && IsFloatingPoint(incomingStack[index]))
                {
                    if (current[index] != StackValueKind.Double)
                    {
                        current[index] = StackValueKind.Double;
                        changed = true;
                    }
                }
                else if (IsReference(current[index]) && IsReference(incomingStack[index]))
                {
                    if (current[index] == StackValueKind.Null)
                    {
                        current[index] = incomingStack[index];
                        changed = true;
                    }
                }
                else
                {
                    reason = "has incompatible stack kinds at a control-flow merge";
                    return false;
                }
            }

            for (int index = 0; index < incomingLocals.Length; index++)
            {
                if (assignedLocals[target][index] && !incomingLocals[index])
                {
                    assignedLocals[target][index] = false;
                    changed = true;
                }
            }

            if (changed)
            {
                pending.Enqueue(target);
            }

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
            if (opcode == VirtualOpCode.Ceq && IsReference(left) && IsReference(right))
            {
                result = StackValueKind.Int32;
                return true;
            }

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

        private static bool IsReference(StackValueKind kind)
        {
            return kind == StackValueKind.String || kind == StackValueKind.Null;
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
            return actual == expected || (IsFloatingPoint(actual) && IsFloatingPoint(expected)) ||
                actual == StackValueKind.Null && expected == StackValueKind.String;
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

        private static bool TryGetLocalIndex(Instruction instruction, out bool store, out int index)
        {
            store = false;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldloc_0: index = 0; return true;
                case Code.Ldloc_1: index = 1; return true;
                case Code.Ldloc_2: index = 2; return true;
                case Code.Ldloc_3: index = 3; return true;
                case Code.Ldloc:
                case Code.Ldloc_S:
                    Local load = instruction.Operand as Local;
                    if (load != null) { index = load.Index; return true; }
                    break;
                case Code.Stloc_0: store = true; index = 0; return true;
                case Code.Stloc_1: store = true; index = 1; return true;
                case Code.Stloc_2: store = true; index = 2; return true;
                case Code.Stloc_3: store = true; index = 3; return true;
                case Code.Stloc:
                case Code.Stloc_S:
                    Local stored = instruction.Operand as Local;
                    if (stored != null) { store = true; index = stored.Index; return true; }
                    break;
            }

            index = -1;
            return false;
        }

        private static bool TryGetBranchOpcode(Code code, out VirtualOpCode opcode)
        {
            switch (code)
            {
                case Code.Br: case Code.Br_S: opcode = VirtualOpCode.Br; return true;
                case Code.Brfalse: case Code.Brfalse_S: opcode = VirtualOpCode.Brfalse; return true;
                case Code.Brtrue: case Code.Brtrue_S: opcode = VirtualOpCode.Brtrue; return true;
                case Code.Beq: case Code.Beq_S: opcode = VirtualOpCode.Beq; return true;
                case Code.Bge: case Code.Bge_S: opcode = VirtualOpCode.Bge; return true;
                case Code.Bge_Un: case Code.Bge_Un_S: opcode = VirtualOpCode.BgeUn; return true;
                case Code.Bgt: case Code.Bgt_S: opcode = VirtualOpCode.Bgt; return true;
                case Code.Bgt_Un: case Code.Bgt_Un_S: opcode = VirtualOpCode.BgtUn; return true;
                case Code.Ble: case Code.Ble_S: opcode = VirtualOpCode.Ble; return true;
                case Code.Ble_Un: case Code.Ble_Un_S: opcode = VirtualOpCode.BleUn; return true;
                case Code.Blt: case Code.Blt_S: opcode = VirtualOpCode.Blt; return true;
                case Code.Blt_Un: case Code.Blt_Un_S: opcode = VirtualOpCode.BltUn; return true;
                case Code.Bne_Un: case Code.Bne_Un_S: opcode = VirtualOpCode.BneUn; return true;
                default: opcode = default(VirtualOpCode); return false;
            }
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

        private static void AddUInt16(List<byte> output, int value)
        {
            output.Add((byte)value);
            output.Add((byte)(value >> 8));
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
