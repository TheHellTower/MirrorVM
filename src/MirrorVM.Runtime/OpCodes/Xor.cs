// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Xor : IOpCode
    {
        public string Name { get { return "Xor"; } }
        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Bitwise(VirtualOpCode.Xor, left, right));
        }
    }
}
