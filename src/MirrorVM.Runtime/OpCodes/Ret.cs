// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ret : IOpCode
    {
        public string Name { get { return "Ret"; } }
        public void Execute(VirtualMachineState state) { state.Return(); }
    }
}
