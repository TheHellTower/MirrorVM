// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class LoadArgument : IOpCode
    {
        public string Name
        {
            get { return "LoadArgument"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(state.ReadArgument());
        }
    }
}
