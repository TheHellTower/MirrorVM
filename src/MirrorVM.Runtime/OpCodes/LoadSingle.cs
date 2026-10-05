// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class LoadSingle : IOpCode
    {
        public string Name
        {
            get { return "LoadSingle"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(state.ReadSingle());
        }
    }
}
