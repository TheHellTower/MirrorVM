// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM
{
    public sealed class VirtualMachineState
    {
        private VirtualMachineValue[] _stack = new VirtualMachineValue[4];
        private readonly object[] _arguments;
        private int _stackCount;
        private VirtualMachineValue _operand;

        public VirtualMachineState()
        {
        }

        internal VirtualMachineState(object[] arguments)
        {
            _arguments = arguments;
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

        internal byte ReadByte()
        {
            return unchecked((byte)_operand.AsInt32());
        }

        internal int ReadInt32()
        {
            return _operand.AsInt32();
        }

        internal VirtualMachineValue ReadArgument()
        {
            int index = ReadByte();
            if (index >= _arguments.Length)
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
    }
}
