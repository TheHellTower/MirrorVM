// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

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
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            state.Push(state.ReadInt32());
        }
    }
}
