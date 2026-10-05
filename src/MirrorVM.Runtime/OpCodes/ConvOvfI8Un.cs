// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfI8Un : IOpCode
    {
        public string Name { get { return "ConvOvfI8Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfI8Un, state.Pop()));
        }
    }
}
