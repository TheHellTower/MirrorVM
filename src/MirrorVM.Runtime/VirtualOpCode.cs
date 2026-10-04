// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM
{
    public enum VirtualOpCode : byte
    {
        LoadArgument = 0x01,
        LoadInt32 = 0x02,
        Ldstr = 0x03,

        Add = 0x10,
        AddOvf = 0x11,
        AddOvfUn = 0x12,
        Sub = 0x13,
        SubOvf = 0x14,
        SubOvfUn = 0x15,
        Mul = 0x16,
        MulOvf = 0x17,
        MulOvfUn = 0x18,
        Div = 0x19,
        DivUn = 0x1A,
        Rem = 0x1B,
        RemUn = 0x1C,
        Neg = 0x1D
    }
}
