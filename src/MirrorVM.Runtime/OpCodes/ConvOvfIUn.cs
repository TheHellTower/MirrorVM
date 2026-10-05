// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfIUn : IOpCode
    {
        public string Name { get { return "ConvOvfIUn"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfIUn, state.Pop()));
        }
    }
}
