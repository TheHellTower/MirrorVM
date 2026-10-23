// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class BltUn : IOpCode
    {
        public string Name { get { return "BltUn"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.BltUn); }
    }
}
