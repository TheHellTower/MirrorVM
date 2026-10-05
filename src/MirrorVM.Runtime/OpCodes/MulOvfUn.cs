// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class MulOvfUn : IOpCode
    {
        public string Name
        {
            get { return "MulOvfUn"; }
        }

        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.MultiplyCheckedUnsigned, left, right));
        }
    }
}
