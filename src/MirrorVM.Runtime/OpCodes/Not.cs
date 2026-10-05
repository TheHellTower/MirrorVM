// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Not : IOpCode
    {
        public string Name { get { return "Not"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.BitwiseNot(state.Pop()));
        }
    }
}
