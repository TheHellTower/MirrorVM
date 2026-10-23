// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM
{
    public sealed class VirtualMachineState
    {
        private VirtualMachineValue[] _stack = new VirtualMachineValue[4];
        private readonly object[] _arguments;
        private readonly VirtualMachineValue[] _locals;
        private int _stackCount;
        private VirtualMachineValue _operand;
        private int _instructionPointer;
        private bool _returned;

        public VirtualMachineState()
        {
            _locals = new VirtualMachineValue[0];
        }

        internal VirtualMachineState(object[] arguments, VirtualMachineValue[] localDefaults)
        {
            _arguments = arguments;
            _locals = (VirtualMachineValue[])localDefaults.Clone();
        }

        public int StackCount { get { return _stackCount; } }

        public VirtualMachineValue Pop()
        {
            if (_stackCount == 0)
            {
                throw new InvalidProgramException("The VM stack is empty.");
            }

            VirtualMachineValue value = _stack[--_stackCount];
            _stack[_stackCount] = default(VirtualMachineValue);
            return value;
        }

        public void Push(VirtualMachineValue value)
        {
            if (_stackCount == _stack.Length)
            {
                Array.Resize(ref _stack, _stack.Length * 2);
            }

            _stack[_stackCount++] = value;
        }

        internal void SetInstructionOperand(VirtualMachineValue operand)
        {
            _operand = operand;
        }

        internal int ReadInt32()
        {
            return _operand.AsInt32();
        }

        internal VirtualMachineValue ReadArgument()
        {
            int index = ReadInt32();
            if ((uint)index >= (uint)_arguments.Length)
            {
                throw new InvalidProgramException("The bytecode references a missing method argument.");
            }

            return NumericArithmetic.NormalizeStackValue(_arguments[index]);
        }

        internal long ReadInt64()
        {
            return _operand.AsInt64();
        }

        internal float ReadSingle()
        {
            return _operand.AsSingle();
        }

        internal double ReadDouble()
        {
            return _operand.AsDouble();
        }

        internal string ReadString()
        {
            return (string)_operand.AsObject();
        }

        internal int InstructionPointer
        {
            get { return _instructionPointer; }
            set { _instructionPointer = value; }
        }

        internal bool Returned { get { return _returned; } }

        internal void Branch()
        {
            _instructionPointer = ReadInt32();
        }

        internal void BranchIf(VirtualOpCode opcode)
        {
            VirtualMachineValue right = Pop();
            VirtualMachineValue left = Pop();
            if (NumericArithmetic.Compare(opcode, left, right).AsInt32() != 0)
            {
                Branch();
            }
        }

        internal void BranchOnValue(bool branchWhenTrue)
        {
            VirtualMachineValue value = Pop();
            bool truth;
            switch (value.Kind)
            {
                case VirtualMachineValueKind.Object: truth = value.AsObject() != null; break;
                case VirtualMachineValueKind.Int32: truth = value.AsInt32() != 0; break;
                case VirtualMachineValueKind.Int64: truth = value.AsInt64() != 0; break;
                case VirtualMachineValueKind.NativeInt: truth = value.AsNativeInt() != IntPtr.Zero; break;
                default: throw new InvalidProgramException("The branch value is not supported.");
            }

            if (truth == branchWhenTrue)
            {
                Branch();
            }
        }

        internal void Switch()
        {
            VirtualMachineValue selector = Pop();
            if (selector.Kind != VirtualMachineValueKind.Int32)
            {
                throw new InvalidProgramException("The switch selector is not an Int32.");
            }

            int[] targets = (int[])_operand.AsObject();
            int index = selector.AsInt32();
            if ((uint)index < (uint)targets.Length)
            {
                _instructionPointer = targets[index];
            }
        }

        internal void Return()
        {
            _returned = true;
        }

        internal void LoadLocal()
        {
            Push(_locals[ReadLocalIndex()]);
        }

        internal void StoreLocal()
        {
            int index = ReadLocalIndex();
            VirtualMachineLocalKind kind = ReadLocalKind();
            VirtualMachineValue value = Pop();
            switch (kind)
            {
                case VirtualMachineLocalKind.Int32:
                    if (value.Kind != VirtualMachineValueKind.Int32) throw InvalidLocalValue();
                    break;
                case VirtualMachineLocalKind.Int64:
                    if (value.Kind != VirtualMachineValueKind.Int64) throw InvalidLocalValue();
                    break;
                case VirtualMachineLocalKind.NativeInt:
                    if (value.Kind != VirtualMachineValueKind.NativeInt) throw InvalidLocalValue();
                    break;
                case VirtualMachineLocalKind.Single:
                    if (value.Kind == VirtualMachineValueKind.Double)
                    {
                        value = VirtualMachineValue.FromSingle((float)value.AsDouble());
                    }
                    else if (value.Kind != VirtualMachineValueKind.Single)
                    {
                        throw InvalidLocalValue();
                    }
                    break;
                case VirtualMachineLocalKind.Double:
                    if (value.Kind == VirtualMachineValueKind.Single)
                    {
                        value = VirtualMachineValue.FromDouble((double)value.AsSingle());
                    }
                    else if (value.Kind != VirtualMachineValueKind.Double)
                    {
                        throw InvalidLocalValue();
                    }
                    break;
                case VirtualMachineLocalKind.Decimal:
                    if (!(value.AsObject() is decimal)) throw InvalidLocalValue();
                    break;
                case VirtualMachineLocalKind.String:
                    if (value.Kind != VirtualMachineValueKind.Object ||
                        value.AsObject() != null && !(value.AsObject() is string)) throw InvalidLocalValue();
                    break;
            }

            _locals[index] = value;
        }

        private int ReadLocalIndex()
        {
            return _operand.AsInt32() & 0xFFFF;
        }

        private VirtualMachineLocalKind ReadLocalKind()
        {
            return (VirtualMachineLocalKind)((_operand.AsInt32() >> 16) & 0xFF);
        }

        private static InvalidProgramException InvalidLocalValue()
        {
            return new InvalidProgramException("A local received a value of the wrong kind.");
        }
    }
}
