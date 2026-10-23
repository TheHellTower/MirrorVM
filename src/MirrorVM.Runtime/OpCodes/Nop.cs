// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Nop : IOpCode
    {
        public string Name { get { return "Nop"; } }
        public void Execute(VirtualMachineState state) { }
    }
}
