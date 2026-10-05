// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfU1Un : IOpCode
    {
        public string Name { get { return "ConvOvfU1Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfU1Un, state.Pop()));
        }
    }
}
