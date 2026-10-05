// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class AddOvfUn : IOpCode
    {
        public string Name
        {
            get { return "AddOvfUn"; }
        }

        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.AddCheckedUnsigned, left, right));
        }
    }
}
