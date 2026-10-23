// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.Collections.Generic;
using MirrorVM.OpCodes;

namespace MirrorVM
{
    public sealed class VirtualMachine
    {
        private static readonly IOpCode[] ByteCodeOpCodes = CreateByteCodeOpCodes();

        public static VirtualMachineProgram Compile(byte[] byteCode)
        {
            if (byteCode == null)
            {
                throw new ArgumentNullException("byteCode");
            }

            ByteCodeReader reader = new ByteCodeReader(byteCode);
            List<VirtualMachineInstruction> instructions = new List<VirtualMachineInstruction>();
            List<VirtualMachineLocalKind> localKinds = new List<VirtualMachineLocalKind>();
            List<bool> definedLocals = new List<bool>();
            while (reader.HasRemaining)
            {
                byte opcode = reader.ReadByte();
                if (ByteCodeOpCodes[opcode] == null)
                {
                    throw new InvalidProgramException("Unknown MirrorVM opcode 0x" + opcode.ToString("X2") + ".");
                }

                VirtualMachineValue operand = default(VirtualMachineValue);
                switch ((VirtualOpCode)opcode)
                {
                    case VirtualOpCode.LoadArgument:
                        operand = VirtualMachineValue.FromInt32(reader.ReadUInt16());
                        break;
                    case VirtualOpCode.LoadInt32:
                        operand = VirtualMachineValue.FromInt32(reader.ReadInt32());
                        break;
                    case VirtualOpCode.LoadInt64:
                        operand = VirtualMachineValue.FromInt64(reader.ReadInt64());
                        break;
                    case VirtualOpCode.LoadSingle:
                        operand = VirtualMachineValue.FromSingleBits(reader.ReadInt32());
                        break;
                    case VirtualOpCode.LoadDouble:
                        operand = VirtualMachineValue.FromDoubleBits(reader.ReadInt64());
                        break;
                    case VirtualOpCode.Ldstr:
                        operand = VirtualMachineValue.FromObject(string.Intern(reader.ReadString()));
                        break;
                    case VirtualOpCode.Ldloc:
                    case VirtualOpCode.Stloc:
                        int localIndex = reader.ReadUInt16();
                        byte localKindValue = reader.ReadByte();
                        if (localKindValue > (byte)VirtualMachineLocalKind.String)
                        {
                            throw new InvalidProgramException("The bytecode contains an invalid local kind.");
                        }

                        while (localKinds.Count <= localIndex)
                        {
                            localKinds.Add(VirtualMachineLocalKind.Int32);
                            definedLocals.Add(false);
                        }

                        VirtualMachineLocalKind localKind = (VirtualMachineLocalKind)localKindValue;
                        if (definedLocals[localIndex] && localKinds[localIndex] != localKind)
                        {
                            throw new InvalidProgramException("The bytecode assigns incompatible kinds to one local.");
                        }

                        localKinds[localIndex] = localKind;
                        definedLocals[localIndex] = true;
                        operand = VirtualMachineValue.FromInt32(localIndex | (localKindValue << 16));
                        break;
                    case VirtualOpCode.Br:
                    case VirtualOpCode.Brfalse:
                    case VirtualOpCode.Brtrue:
                    case VirtualOpCode.Beq:
                    case VirtualOpCode.Bge:
                    case VirtualOpCode.BgeUn:
                    case VirtualOpCode.Bgt:
                    case VirtualOpCode.BgtUn:
                    case VirtualOpCode.Ble:
                    case VirtualOpCode.BleUn:
                    case VirtualOpCode.Blt:
                    case VirtualOpCode.BltUn:
                    case VirtualOpCode.BneUn:
                        operand = VirtualMachineValue.FromInt32(reader.ReadInt32());
                        break;
                    case VirtualOpCode.Switch:
                        int targetCount = reader.ReadInt32();
                        if (targetCount < 0 || targetCount > reader.Remaining / 4)
                        {
                            throw new InvalidProgramException("The switch target table is truncated or has an invalid length.");
                        }

                        int[] targets = new int[targetCount];
                        for (int index = 0; index < targetCount; index++)
                        {
                            targets[index] = reader.ReadInt32();
                        }

                        operand = VirtualMachineValue.FromObject(targets);
                        break;
                }

                instructions.Add(new VirtualMachineInstruction(opcode, operand));
            }

            ValidateBranchTargets(instructions);
            VirtualMachineValue[] localDefaults = new VirtualMachineValue[localKinds.Count];
            for (int index = 0; index < localDefaults.Length; index++)
            {
                if (!definedLocals[index])
                {
                    throw new InvalidProgramException("The bytecode references an undefined local.");
                }

                localDefaults[index] = GetLocalDefault(localKinds[index]);
            }

            return new VirtualMachineProgram(instructions.ToArray(), localDefaults);
        }

        public static object Execute(VirtualMachineProgram program, object[] arguments)
        {
            if (program == null)
            {
                throw new ArgumentNullException("program");
            }

            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            VirtualMachineState state = new VirtualMachineState(arguments, program.LocalDefaults);
            VirtualMachineInstruction[] instructions = program.Instructions;
            int instructionPointer = 0;
            while (instructionPointer < instructions.Length)
            {
                VirtualMachineInstruction instruction = instructions[instructionPointer];
                state.InstructionPointer = instructionPointer + 1;
                state.SetInstructionOperand(instruction.Operand);
                ByteCodeOpCodes[instruction.OpCode].Execute(state);
                if (state.Returned)
                {
                    break;
                }

                instructionPointer = state.InstructionPointer;
            }

            if (state.StackCount > 1 || state.StackCount == 0 && !state.Returned)
            {
                throw new InvalidProgramException("A virtual method must return with zero or one value on the stack.");
            }

            if (state.StackCount == 0)
            {
                return null;
            }

            VirtualMachineValue result = state.Pop();
            return result.Kind == VirtualMachineValueKind.Single
                ? (object)(double)result.AsSingle()
                : result.ToObject();
        }

        private static IOpCode[] CreateByteCodeOpCodes()
        {
            IOpCode[] handlers = new IOpCode[256];
            handlers[(byte)VirtualOpCode.LoadArgument] = new LoadArgument();
            handlers[(byte)VirtualOpCode.LoadInt32] = new LoadInt32();
            handlers[(byte)VirtualOpCode.Ldstr] = new Ldstr();
            handlers[(byte)VirtualOpCode.LoadInt64] = new LoadInt64();
            handlers[(byte)VirtualOpCode.LoadSingle] = new LoadSingle();
            handlers[(byte)VirtualOpCode.LoadDouble] = new LoadDouble();
            handlers[(byte)VirtualOpCode.Nop] = new OpCodes.Nop();
            handlers[(byte)VirtualOpCode.Ldloc] = new OpCodes.Ldloc();
            handlers[(byte)VirtualOpCode.Stloc] = new OpCodes.Stloc();
            handlers[(byte)VirtualOpCode.Ret] = new OpCodes.Ret();
            handlers[(byte)VirtualOpCode.Ldnull] = new OpCodes.Ldnull();
            handlers[(byte)VirtualOpCode.Add] = new Add();
            handlers[(byte)VirtualOpCode.AddOvf] = new AddOvf();
            handlers[(byte)VirtualOpCode.AddOvfUn] = new AddOvfUn();
            handlers[(byte)VirtualOpCode.Sub] = new Sub();
            handlers[(byte)VirtualOpCode.SubOvf] = new SubOvf();
            handlers[(byte)VirtualOpCode.SubOvfUn] = new SubOvfUn();
            handlers[(byte)VirtualOpCode.Mul] = new Mul();
            handlers[(byte)VirtualOpCode.MulOvf] = new MulOvf();
            handlers[(byte)VirtualOpCode.MulOvfUn] = new MulOvfUn();
            handlers[(byte)VirtualOpCode.Div] = new Div();
            handlers[(byte)VirtualOpCode.DivUn] = new DivUn();
            handlers[(byte)VirtualOpCode.Rem] = new Rem();
            handlers[(byte)VirtualOpCode.RemUn] = new RemUn();
            handlers[(byte)VirtualOpCode.Neg] = new Neg();
            handlers[(byte)VirtualOpCode.Not] = new OpCodes.Not();
            handlers[(byte)VirtualOpCode.And] = new OpCodes.And();
            handlers[(byte)VirtualOpCode.Or] = new OpCodes.Or();
            handlers[(byte)VirtualOpCode.Xor] = new OpCodes.Xor();
            handlers[(byte)VirtualOpCode.Shl] = new OpCodes.Shl();
            handlers[(byte)VirtualOpCode.Shr] = new OpCodes.Shr();
            handlers[(byte)VirtualOpCode.ShrUn] = new OpCodes.ShrUn();
            handlers[(byte)VirtualOpCode.Ceq] = new OpCodes.Ceq();
            handlers[(byte)VirtualOpCode.Cgt] = new OpCodes.Cgt();
            handlers[(byte)VirtualOpCode.CgtUn] = new OpCodes.CgtUn();
            handlers[(byte)VirtualOpCode.Clt] = new OpCodes.Clt();
            handlers[(byte)VirtualOpCode.CltUn] = new OpCodes.CltUn();
            handlers[(byte)VirtualOpCode.Ckfinite] = new OpCodes.Ckfinite();
            handlers[(byte)VirtualOpCode.ConvI1] = new OpCodes.ConvI1();
            handlers[(byte)VirtualOpCode.ConvU1] = new OpCodes.ConvU1();
            handlers[(byte)VirtualOpCode.ConvI2] = new OpCodes.ConvI2();
            handlers[(byte)VirtualOpCode.ConvU2] = new OpCodes.ConvU2();
            handlers[(byte)VirtualOpCode.ConvI4] = new OpCodes.ConvI4();
            handlers[(byte)VirtualOpCode.ConvU4] = new OpCodes.ConvU4();
            handlers[(byte)VirtualOpCode.ConvI8] = new OpCodes.ConvI8();
            handlers[(byte)VirtualOpCode.ConvU8] = new OpCodes.ConvU8();
            handlers[(byte)VirtualOpCode.ConvI] = new OpCodes.ConvI();
            handlers[(byte)VirtualOpCode.ConvU] = new OpCodes.ConvU();
            handlers[(byte)VirtualOpCode.ConvR4] = new OpCodes.ConvR4();
            handlers[(byte)VirtualOpCode.ConvR8] = new OpCodes.ConvR8();
            handlers[(byte)VirtualOpCode.ConvRUn] = new OpCodes.ConvRUn();
            handlers[(byte)VirtualOpCode.ConvOvfI1] = new OpCodes.ConvOvfI1();
            handlers[(byte)VirtualOpCode.ConvOvfU1] = new OpCodes.ConvOvfU1();
            handlers[(byte)VirtualOpCode.ConvOvfI2] = new OpCodes.ConvOvfI2();
            handlers[(byte)VirtualOpCode.ConvOvfU2] = new OpCodes.ConvOvfU2();
            handlers[(byte)VirtualOpCode.ConvOvfI4] = new OpCodes.ConvOvfI4();
            handlers[(byte)VirtualOpCode.ConvOvfU4] = new OpCodes.ConvOvfU4();
            handlers[(byte)VirtualOpCode.ConvOvfI8] = new OpCodes.ConvOvfI8();
            handlers[(byte)VirtualOpCode.ConvOvfU8] = new OpCodes.ConvOvfU8();
            handlers[(byte)VirtualOpCode.ConvOvfI] = new OpCodes.ConvOvfI();
            handlers[(byte)VirtualOpCode.ConvOvfU] = new OpCodes.ConvOvfU();
            handlers[(byte)VirtualOpCode.ConvOvfI1Un] = new OpCodes.ConvOvfI1Un();
            handlers[(byte)VirtualOpCode.ConvOvfU1Un] = new OpCodes.ConvOvfU1Un();
            handlers[(byte)VirtualOpCode.ConvOvfI2Un] = new OpCodes.ConvOvfI2Un();
            handlers[(byte)VirtualOpCode.ConvOvfU2Un] = new OpCodes.ConvOvfU2Un();
            handlers[(byte)VirtualOpCode.ConvOvfI4Un] = new OpCodes.ConvOvfI4Un();
            handlers[(byte)VirtualOpCode.ConvOvfU4Un] = new OpCodes.ConvOvfU4Un();
            handlers[(byte)VirtualOpCode.ConvOvfI8Un] = new OpCodes.ConvOvfI8Un();
            handlers[(byte)VirtualOpCode.ConvOvfU8Un] = new OpCodes.ConvOvfU8Un();
            handlers[(byte)VirtualOpCode.ConvOvfIUn] = new OpCodes.ConvOvfIUn();
            handlers[(byte)VirtualOpCode.ConvOvfUUn] = new OpCodes.ConvOvfUUn();
            handlers[(byte)VirtualOpCode.Br] = new OpCodes.Br();
            handlers[(byte)VirtualOpCode.Brfalse] = new OpCodes.Brfalse();
            handlers[(byte)VirtualOpCode.Brtrue] = new OpCodes.Brtrue();
            handlers[(byte)VirtualOpCode.Beq] = new OpCodes.Beq();
            handlers[(byte)VirtualOpCode.Bge] = new OpCodes.Bge();
            handlers[(byte)VirtualOpCode.BgeUn] = new OpCodes.BgeUn();
            handlers[(byte)VirtualOpCode.Bgt] = new OpCodes.Bgt();
            handlers[(byte)VirtualOpCode.BgtUn] = new OpCodes.BgtUn();
            handlers[(byte)VirtualOpCode.Ble] = new OpCodes.Ble();
            handlers[(byte)VirtualOpCode.BleUn] = new OpCodes.BleUn();
            handlers[(byte)VirtualOpCode.Blt] = new OpCodes.Blt();
            handlers[(byte)VirtualOpCode.BltUn] = new OpCodes.BltUn();
            handlers[(byte)VirtualOpCode.BneUn] = new OpCodes.BneUn();
            handlers[(byte)VirtualOpCode.Switch] = new OpCodes.Switch();
            return handlers;
        }

        private static void ValidateBranchTargets(List<VirtualMachineInstruction> instructions)
        {
            for (int index = 0; index < instructions.Count; index++)
            {
                VirtualMachineInstruction instruction = instructions[index];
                VirtualOpCode opcode = (VirtualOpCode)instruction.OpCode;
                if (IsBranch(opcode))
                {
                    ValidateTarget(instruction.Operand.AsInt32(), instructions.Count);
                }
                else if (opcode == VirtualOpCode.Switch)
                {
                    int[] targets = (int[])instruction.Operand.AsObject();
                    for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                    {
                        ValidateTarget(targets[targetIndex], instructions.Count);
                    }
                }
            }
        }

        private static bool IsBranch(VirtualOpCode opcode)
        {
            return opcode >= VirtualOpCode.Br && opcode <= VirtualOpCode.BneUn;
        }

        private static void ValidateTarget(int target, int instructionCount)
        {
            if ((uint)target >= (uint)instructionCount)
            {
                throw new InvalidProgramException("A branch target is outside the virtual method.");
            }
        }

        private static VirtualMachineValue GetLocalDefault(VirtualMachineLocalKind kind)
        {
            switch (kind)
            {
                case VirtualMachineLocalKind.Int32: return VirtualMachineValue.FromInt32(0);
                case VirtualMachineLocalKind.Int64: return VirtualMachineValue.FromInt64(0L);
                case VirtualMachineLocalKind.NativeInt: return VirtualMachineValue.FromNativeInt(IntPtr.Zero);
                case VirtualMachineLocalKind.Single: return VirtualMachineValue.FromSingle(0f);
                case VirtualMachineLocalKind.Double: return VirtualMachineValue.FromDouble(0d);
                case VirtualMachineLocalKind.Decimal: return VirtualMachineValue.FromObject(default(decimal));
                default: return VirtualMachineValue.FromObject(null);
            }
        }

        private struct ByteCodeReader
        {
            private readonly byte[] _byteCode;
            private int _instructionPointer;

            internal ByteCodeReader(byte[] byteCode)
            {
                _byteCode = byteCode;
                _instructionPointer = 0;
            }

            internal bool HasRemaining { get { return _instructionPointer < _byteCode.Length; } }
            internal int Remaining { get { return _byteCode.Length - _instructionPointer; } }

            internal byte ReadByte()
            {
                if (_instructionPointer >= _byteCode.Length)
                {
                    throw new InvalidProgramException("The virtual bytecode is truncated.");
                }

                return _byteCode[_instructionPointer++];
            }

            internal int ReadInt32()
            {
                if (_instructionPointer > _byteCode.Length - 4)
                {
                    throw new InvalidProgramException("The virtual Int32 operand is truncated.");
                }

                int value = unchecked(
                    _byteCode[_instructionPointer] |
                    (_byteCode[_instructionPointer + 1] << 8) |
                    (_byteCode[_instructionPointer + 2] << 16) |
                    (_byteCode[_instructionPointer + 3] << 24));
                _instructionPointer += 4;
                return value;
            }

            internal int ReadUInt16()
            {
                if (_instructionPointer > _byteCode.Length - 2)
                {
                    throw new InvalidProgramException("The local index is truncated.");
                }

                int value = _byteCode[_instructionPointer] | (_byteCode[_instructionPointer + 1] << 8);
                _instructionPointer += 2;
                return value;
            }

            internal long ReadInt64()
            {
                int low = ReadInt32();
                int high = ReadInt32();
                return unchecked((long)((ulong)(uint)low | ((ulong)(uint)high << 32)));
            }

            internal string ReadString()
            {
                int byteCount = ReadInt32();
                if (byteCount < 0 || (byteCount & 1) != 0 || byteCount > _byteCode.Length - _instructionPointer)
                {
                    throw new InvalidProgramException("The Ldstr operand is truncated or has an invalid length.");
                }

                char[] characters = new char[byteCount / 2];
                for (int index = 0; index < characters.Length; index++)
                {
                    int byteIndex = _instructionPointer + index * 2;
                    characters[index] = (char)(_byteCode[byteIndex] | (_byteCode[byteIndex + 1] << 8));
                }

                _instructionPointer += byteCount;
                return new string(characters);
            }
        }
    }
}
