// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class LoadInt32 : IOpCode
    {
        public string Name
        {
            get { return "LoadInt32"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(state.ReadInt32());
        }
    }
}
