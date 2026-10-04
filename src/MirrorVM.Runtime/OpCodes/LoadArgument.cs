// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

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
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            state.Push(state.ReadArgument());
        }
    }
}
