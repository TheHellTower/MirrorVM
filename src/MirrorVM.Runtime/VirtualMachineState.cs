// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;
using System;
using System.Text;

namespace MirrorVM
{
    public sealed class VirtualMachineState
    {
        private readonly Stack<object> _stack = new Stack<object>();
        private readonly byte[] _byteCode;
        private readonly object[] _arguments;
        private int _instructionPointer;

        public VirtualMachineState()
            : this(null, null)
        {
        }

        internal VirtualMachineState(byte[] byteCode, object[] arguments)
        {
            _byteCode = byteCode;
            _arguments = arguments;
        }

        public int StackCount
        {
            get { return _stack.Count; }
        }

        public object Pop()
        {
            return _stack.Pop();
        }

        public void Push(object value)
        {
            _stack.Push(value);
        }

        internal bool HasRemainingByteCode
        {
            get { return _byteCode != null && _instructionPointer < _byteCode.Length; }
        }

        public byte ReadByte()
        {
            if (_byteCode == null || _instructionPointer >= _byteCode.Length)
            {
                throw new InvalidProgramException("The virtual bytecode is truncated.");
            }

            return _byteCode[_instructionPointer++];
        }

        public int ReadInt32()
        {
            if (_byteCode == null || _instructionPointer > _byteCode.Length - 4)
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

        public object ReadArgument()
        {
            int index = ReadByte();
            if (_arguments == null || index >= _arguments.Length)
            {
                throw new InvalidProgramException("The bytecode references a missing method argument.");
            }

            return _arguments[index];
        }

        public string ReadString()
        {
            int byteCount = ReadInt32();
            if (byteCount < 0 || _byteCode == null || byteCount > _byteCode.Length - _instructionPointer)
            {
                throw new InvalidProgramException("The Ldstr operand is truncated or has an invalid length.");
            }

            string value = Encoding.UTF8.GetString(_byteCode, _instructionPointer, byteCount);
            _instructionPointer += byteCount;
            return value;
        }
    }
}
