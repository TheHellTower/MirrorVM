// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvU : IOpCode
    {
        public string Name { get { return "ConvU"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvU, state.Pop()));
        }
    }
}
