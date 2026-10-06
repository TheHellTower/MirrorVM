// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Sub : IOpCode
    {
        public string Name
        {
            get { return "Sub"; }
        }

        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.Subtract, left, right));
        }
    }
}
