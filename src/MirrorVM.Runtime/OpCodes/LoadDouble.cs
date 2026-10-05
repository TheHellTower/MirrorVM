// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class LoadDouble : IOpCode
    {
        public string Name
        {
            get { return "LoadDouble"; }
        }

        public void Execute(VirtualMachineState state)
        {
            state.Push(state.ReadDouble());
        }
    }
}
