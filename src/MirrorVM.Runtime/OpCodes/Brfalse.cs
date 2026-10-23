// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Brfalse : IOpCode
    {
        public string Name { get { return "Brfalse"; } }
        public void Execute(VirtualMachineState state) { state.BranchOnValue(false); }
    }
}
