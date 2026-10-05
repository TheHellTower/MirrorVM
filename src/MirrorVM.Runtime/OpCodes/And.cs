// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class And : IOpCode
    {
        public string Name { get { return "And"; } }
        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Bitwise(VirtualOpCode.And, left, right));
        }
    }
}
