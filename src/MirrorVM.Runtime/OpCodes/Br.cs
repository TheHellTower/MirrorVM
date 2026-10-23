// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Br : IOpCode
    {
        public string Name { get { return "Br"; } }
        public void Execute(VirtualMachineState state) { state.Branch(); }
    }
}
