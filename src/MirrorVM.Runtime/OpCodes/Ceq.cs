// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ceq : IOpCode
    {
        public string Name { get { return "Ceq"; } }
        public void Execute(VirtualMachineState state)
        {
            VirtualMachineValue right = state.Pop();
            VirtualMachineValue left = state.Pop();
            state.Push(NumericArithmetic.Compare(VirtualOpCode.Ceq, left, right));
        }
    }
}
