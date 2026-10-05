// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ShrUn : IOpCode
    {
        public string Name { get { return "ShrUn"; } }
        public void Execute(VirtualMachineState state)
        {
            object count = state.Pop();
            object value = state.Pop();
            state.Push(NumericArithmetic.Shift(VirtualOpCode.ShrUn, value, count));
        }
    }
}
