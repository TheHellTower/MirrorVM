// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Shl : IOpCode
    {
        public string Name { get { return "Shl"; } }
        public void Execute(VirtualMachineState state)
        {
            object count = state.Pop();
            object value = state.Pop();
            state.Push(NumericArithmetic.Shift(VirtualOpCode.Shl, value, count));
        }
    }
}
