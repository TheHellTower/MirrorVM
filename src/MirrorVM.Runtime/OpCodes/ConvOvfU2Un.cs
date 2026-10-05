// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfU2Un : IOpCode
    {
        public string Name { get { return "ConvOvfU2Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfU2Un, state.Pop()));
        }
    }
}
