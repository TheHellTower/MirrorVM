// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class RemUn : IOpCode
    {
        public string Name
        {
            get { return "RemUn"; }
        }

        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Apply(ArithmeticOperation.RemainderUnsigned, left, right));
        }
    }
}
