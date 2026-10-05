// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ckfinite : IOpCode
    {
        public string Name { get { return "Ckfinite"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.CheckFinite(state.Pop()));
        }
    }
}
