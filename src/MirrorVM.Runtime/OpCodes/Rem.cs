// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.OpCodes
{
    public sealed class Rem : IOpCode
    {
        public string Name
        {
            get { return "Rem"; }
        }

        public void Execute(VirtualMachineState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            if (state.StackCount < 2)
            {
                throw new InvalidProgramException(Name + " requires two values on the VM stack.");
            }

            object right = state.Pop();
            object left = state.Pop();
            object result;

            try
            {
                result = NumericArithmetic.Apply(ArithmeticOperation.Remainder, left, right);
            }
            catch
            {
                state.Push(left);
                state.Push(right);
                throw;
            }

            state.Push(result);
        }
    }
}
