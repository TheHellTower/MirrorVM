// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ldloc : IOpCode
    {
        public string Name { get { return "Ldloc"; } }
        public void Execute(VirtualMachineState state) { state.LoadLocal(); }
    }
}
