// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Bgt : IOpCode
    {
        public string Name { get { return "Bgt"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.Bgt); }
    }
}
