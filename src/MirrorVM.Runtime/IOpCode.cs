// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM
{
    public interface IOpCode
    {
        string Name { get; }

        void Execute(VirtualMachineState state);
    }
}
