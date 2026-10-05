// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvU2 : IOpCode
    {
        public string Name { get { return "ConvU2"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvU2, state.Pop()));
        }
    }
}
