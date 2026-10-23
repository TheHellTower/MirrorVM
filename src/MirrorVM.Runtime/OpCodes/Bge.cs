// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Bge : IOpCode
    {
        public string Name { get { return "Bge"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.Bge); }
    }
}
