// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.OpCodes
{
    public abstract class UnaryArithmeticOpCode : IOpCode
    {
        public abstract string Name { get; }

        protected abstract object Calculate(object value);

        public void Execute(VirtualMachineState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            if (state.StackCount < 1)
            {
                throw new InvalidProgramException(Name + " requires one value on the VM stack.");
            }

            object value = state.Pop();
            object result;

            try
            {
                result = Calculate(value);
            }
            catch
            {
                state.Push(value);
                throw;
            }

            state.Push(result);
        }
    }
}
