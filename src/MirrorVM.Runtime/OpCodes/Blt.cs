// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Blt : IOpCode
    {
        public string Name { get { return "Blt"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.Blt); }
    }
}
