// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfUUn : IOpCode
    {
        public string Name { get { return "ConvOvfUUn"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfUUn, state.Pop()));
        }
    }
}
