// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class BgeUn : IOpCode
    {
        public string Name { get { return "BgeUn"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.BgeUn); }
    }
}
