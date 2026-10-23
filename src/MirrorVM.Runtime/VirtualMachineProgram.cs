// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM
{
    public sealed class VirtualMachineProgram
    {
        internal readonly VirtualMachineInstruction[] Instructions;
        internal readonly VirtualMachineValue[] LocalDefaults;

        internal VirtualMachineProgram(
            VirtualMachineInstruction[] instructions,
            VirtualMachineValue[] localDefaults)
        {
            Instructions = instructions;
            LocalDefaults = localDefaults;
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

    internal enum VirtualMachineLocalKind : byte
    {
        Int32,
        Int64,
        NativeInt,
        Single,
        Double,
        Decimal,
        String
    }
}
