// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Stloc : IOpCode
    {
        public string Name { get { return "Stloc"; } }
        public void Execute(VirtualMachineState state) { state.StoreLocal(); }
    }
}
