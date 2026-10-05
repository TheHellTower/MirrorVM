// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfI1Un : IOpCode
    {
        public string Name { get { return "ConvOvfI1Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfI1Un, state.Pop()));
        }
    }
}
