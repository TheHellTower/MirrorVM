// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class CltUn : IOpCode
    {
        public string Name { get { return "CltUn"; } }
        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Compare(VirtualOpCode.CltUn, left, right));
        }
    }
}
