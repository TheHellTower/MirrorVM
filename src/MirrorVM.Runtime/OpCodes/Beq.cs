// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Beq : IOpCode
    {
        public string Name { get { return "Beq"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.Beq); }
    }
}
