// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.Collections.Generic;
using MirrorVM.OpCodes;

namespace MirrorVM
{
    public sealed class VirtualMachine
    {
        private static readonly Dictionary<byte, IOpCode> ByteCodeOpCodes = CreateByteCodeOpCodes();

        public static object Execute(byte[] byteCode, object[] arguments)
        {
            if (byteCode == null)
            {
                throw new ArgumentNullException("byteCode");
            }

            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            VirtualMachineState state = new VirtualMachineState(byteCode, arguments);

            while (state.HasRemainingByteCode)
            {
                byte opcode = state.ReadByte();
                IOpCode handler;
                if (!ByteCodeOpCodes.TryGetValue(opcode, out handler))
                {
                    throw new InvalidProgramException("Unknown MirrorVM opcode 0x" + opcode.ToString("X2") + ".");
                }

                handler.Execute(state);
            }

            if (state.StackCount != 1)
            {
                throw new InvalidProgramException("A virtual method must leave exactly one value on the stack.");
            }

            return state.Pop();
        }

        private static Dictionary<byte, IOpCode> CreateByteCodeOpCodes()
        {
            return new Dictionary<byte, IOpCode>
            {
                { (byte)VirtualOpCode.LoadArgument, new LoadArgument() },
                { (byte)VirtualOpCode.LoadInt32, new LoadInt32() },
                { (byte)VirtualOpCode.Ldstr, new Ldstr() },
                { (byte)VirtualOpCode.Add, new Add() },
                { (byte)VirtualOpCode.AddOvf, new AddOvf() },
                { (byte)VirtualOpCode.AddOvfUn, new AddOvfUn() },
                { (byte)VirtualOpCode.Sub, new Sub() },
                { (byte)VirtualOpCode.SubOvf, new SubOvf() },
                { (byte)VirtualOpCode.SubOvfUn, new SubOvfUn() },
                { (byte)VirtualOpCode.Mul, new Mul() },
                { (byte)VirtualOpCode.MulOvf, new MulOvf() },
                { (byte)VirtualOpCode.MulOvfUn, new MulOvfUn() },
                { (byte)VirtualOpCode.Div, new Div() },
                { (byte)VirtualOpCode.DivUn, new DivUn() },
                { (byte)VirtualOpCode.Rem, new Rem() },
                { (byte)VirtualOpCode.RemUn, new RemUn() },
                { (byte)VirtualOpCode.Neg, new Neg() }
            };
        }
    }
}
