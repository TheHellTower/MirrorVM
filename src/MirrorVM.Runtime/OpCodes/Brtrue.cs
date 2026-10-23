// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Brtrue : IOpCode
    {
        public string Name { get { return "Brtrue"; } }
        public void Execute(VirtualMachineState state) { state.BranchOnValue(true); }
    }
}
