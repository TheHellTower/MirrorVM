// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfI2Un : IOpCode
    {
        public string Name { get { return "ConvOvfI2Un"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfI2Un, state.Pop()));
        }
    }
}
