// SPDX-License-Identifier: AGPL-3.0-or-later
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
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.Remainder, left, right));
        }
    }
}
