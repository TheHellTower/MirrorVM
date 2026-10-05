// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class CgtUn : IOpCode
    {
        public string Name { get { return "CgtUn"; } }
        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Compare(VirtualOpCode.CgtUn, left, right));
        }
    }
}
