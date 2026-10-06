// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM
{
    public sealed class VirtualMachineProgram
    {
        internal readonly VirtualMachineInstruction[] Instructions;

        internal VirtualMachineProgram(VirtualMachineInstruction[] instructions)
        {
            Instructions = instructions;
        }
    }

    internal struct VirtualMachineInstruction
    {
        internal readonly byte OpCode;
        internal readonly VirtualMachineValue Operand;

        internal VirtualMachineInstruction(byte opCode, VirtualMachineValue operand)
        {
            OpCode = opCode;
            Operand = operand;
        }
    }
}
