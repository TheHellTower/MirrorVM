// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Shr : IOpCode
    {
        public string Name { get { return "Shr"; } }
        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue count = state.Pop();
            VirtualMachineValue value = state.Pop();
            state.Push(NumericArithmetic.Shift(VirtualOpCode.Shr, value, count));
        }
    }
}
