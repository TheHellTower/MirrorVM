// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class SubOvfUn : IOpCode
    {
        public string Name
        {
            get { return "SubOvfUn"; }
        }

        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.SubtractCheckedUnsigned, left, right));
        }
    }
}
