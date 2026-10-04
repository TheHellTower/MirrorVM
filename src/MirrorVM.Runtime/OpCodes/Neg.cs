// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.OpCodes
{
    public sealed class Neg : IOpCode
    {
        public string Name
        {
            get { return "Neg"; }
        }

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
                result = NumericArithmetic.Negate(value);
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
