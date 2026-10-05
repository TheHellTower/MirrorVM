// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvI8 : IOpCode
    {
        public string Name { get { return "ConvI8"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvI8, state.Pop()));
        }
    }
}
