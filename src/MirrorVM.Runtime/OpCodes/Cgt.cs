// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Cgt : IOpCode
    {
        public string Name { get { return "Cgt"; } }
        public void Execute(VirtualMachineState state)
        {
            object right = state.Pop();
            object left = state.Pop();
            state.Push(NumericArithmetic.Compare(VirtualOpCode.Cgt, left, right));
        }
    }
}
