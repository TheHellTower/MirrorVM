// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfU8Un : IOpCode
    {
        public string Name { get { return "ConvOvfU8Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfU8Un, state.Pop()));
        }
    }
}
