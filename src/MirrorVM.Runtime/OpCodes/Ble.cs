// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ble : IOpCode
    {
        public string Name { get { return "Ble"; } }
        public void Execute(VirtualMachineState state) { state.BranchIf(VirtualOpCode.Ble); }
    }
}
