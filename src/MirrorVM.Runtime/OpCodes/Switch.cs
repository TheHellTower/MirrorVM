// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Switch : IOpCode
    {
        public string Name { get { return "Switch"; } }
        public void Execute(VirtualMachineState state) { state.Switch(); }
    }
}
