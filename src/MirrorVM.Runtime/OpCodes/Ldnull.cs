// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ldnull : IOpCode
    {
        public string Name { get { return "Ldnull"; } }
        public void Execute(VirtualMachineState state) { state.Push(VirtualMachineValue.FromObject(null)); }
    }
}
