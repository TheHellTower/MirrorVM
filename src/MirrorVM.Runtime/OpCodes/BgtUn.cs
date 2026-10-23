// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class BgtUn : IOpCode
    {
        public string Name { get { return "BgtUn"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.BgtUn); }
    }
}
