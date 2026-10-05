// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class ConvRUn : IOpCode
    {
        public string Name { get { return "ConvRUn"; } }
        public void Execute(VirtualMachineState state)
        {
            state.Push(NumericArithmetic.ConvertValue(VirtualOpCode.ConvRUn, state.Pop()));
        }
    }
}
