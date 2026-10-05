// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvOvfI : IOpCode
    {
        public string Name { get { return "ConvOvfI"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvOvfI, state.Pop()));
        }
    }
}
