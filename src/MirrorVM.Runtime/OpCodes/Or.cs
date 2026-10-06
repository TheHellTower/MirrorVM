// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Or : IOpCode
    {
        public string Name { get { return "Or"; } }
        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Bitwise(VirtualOpCode.Or, left, right));
        }
    }
}
