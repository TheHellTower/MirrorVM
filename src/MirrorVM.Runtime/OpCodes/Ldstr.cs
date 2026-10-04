// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

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
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            state.Push(state.ReadString());
        }
    }
}
