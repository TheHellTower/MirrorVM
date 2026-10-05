// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Neg : IOpCode
    {
        public string Name
        {
            get { return "Neg"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.Negate(state.Pop()));
        }
    }
}
