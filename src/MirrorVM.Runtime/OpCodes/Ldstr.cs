// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Ldstr : IOpCode
    {
        public string Name
        {
            get { return "Ldstr"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(state.ReadString());
        }
    }
}
