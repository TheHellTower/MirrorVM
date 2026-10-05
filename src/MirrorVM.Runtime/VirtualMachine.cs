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

            object result = state.Pop();
            return result is float ? (object)(double)(float)result : result;
        }

        private static Dictionary<byte, IOpCode> CreateByteCodeOpCodes()
        {
            return new Dictionary<byte, IOpCode>
            {
                { (byte)VirtualOpCode.LoadArgument, new LoadArgument() },
                { (byte)VirtualOpCode.LoadInt32, new LoadInt32() },
                { (byte)VirtualOpCode.Ldstr, new Ldstr() },
                { (byte)VirtualOpCode.LoadInt64, new LoadInt64() },
                { (byte)VirtualOpCode.LoadSingle, new LoadSingle() },
                { (byte)VirtualOpCode.LoadDouble, new LoadDouble() },
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
                { (byte)VirtualOpCode.Neg, new Neg() },
                { (byte)VirtualOpCode.Not, new OpCodes.Not() },
                { (byte)VirtualOpCode.And, new OpCodes.And() },
                { (byte)VirtualOpCode.Or, new OpCodes.Or() },
                { (byte)VirtualOpCode.Xor, new OpCodes.Xor() },
                { (byte)VirtualOpCode.Shl, new OpCodes.Shl() },
                { (byte)VirtualOpCode.Shr, new OpCodes.Shr() },
                { (byte)VirtualOpCode.ShrUn, new OpCodes.ShrUn() },
                { (byte)VirtualOpCode.Ceq, new OpCodes.Ceq() },
                { (byte)VirtualOpCode.Cgt, new OpCodes.Cgt() },
                { (byte)VirtualOpCode.CgtUn, new OpCodes.CgtUn() },
                { (byte)VirtualOpCode.Clt, new OpCodes.Clt() },
                { (byte)VirtualOpCode.CltUn, new OpCodes.CltUn() },
                { (byte)VirtualOpCode.Ckfinite, new OpCodes.Ckfinite() },
                { (byte)VirtualOpCode.ConvI1, new OpCodes.ConvI1() },
                { (byte)VirtualOpCode.ConvU1, new OpCodes.ConvU1() },
                { (byte)VirtualOpCode.ConvI2, new OpCodes.ConvI2() },
                { (byte)VirtualOpCode.ConvU2, new OpCodes.ConvU2() },
                { (byte)VirtualOpCode.ConvI4, new OpCodes.ConvI4() },
                { (byte)VirtualOpCode.ConvU4, new OpCodes.ConvU4() },
                { (byte)VirtualOpCode.ConvI8, new OpCodes.ConvI8() },
                { (byte)VirtualOpCode.ConvU8, new OpCodes.ConvU8() },
                { (byte)VirtualOpCode.ConvI, new OpCodes.ConvI() },
                { (byte)VirtualOpCode.ConvU, new OpCodes.ConvU() },
                { (byte)VirtualOpCode.ConvR4, new OpCodes.ConvR4() },
                { (byte)VirtualOpCode.ConvR8, new OpCodes.ConvR8() },
                { (byte)VirtualOpCode.ConvRUn, new OpCodes.ConvRUn() },
                { (byte)VirtualOpCode.ConvOvfI1, new OpCodes.ConvOvfI1() },
                { (byte)VirtualOpCode.ConvOvfU1, new OpCodes.ConvOvfU1() },
                { (byte)VirtualOpCode.ConvOvfI2, new OpCodes.ConvOvfI2() },
                { (byte)VirtualOpCode.ConvOvfU2, new OpCodes.ConvOvfU2() },
                { (byte)VirtualOpCode.ConvOvfI4, new OpCodes.ConvOvfI4() },
                { (byte)VirtualOpCode.ConvOvfU4, new OpCodes.ConvOvfU4() },
                { (byte)VirtualOpCode.ConvOvfI8, new OpCodes.ConvOvfI8() },
                { (byte)VirtualOpCode.ConvOvfU8, new OpCodes.ConvOvfU8() },
                { (byte)VirtualOpCode.ConvOvfI, new OpCodes.ConvOvfI() },
                { (byte)VirtualOpCode.ConvOvfU, new OpCodes.ConvOvfU() },
                { (byte)VirtualOpCode.ConvOvfI1Un, new OpCodes.ConvOvfI1Un() },
                { (byte)VirtualOpCode.ConvOvfU1Un, new OpCodes.ConvOvfU1Un() },
                { (byte)VirtualOpCode.ConvOvfI2Un, new OpCodes.ConvOvfI2Un() },
                { (byte)VirtualOpCode.ConvOvfU2Un, new OpCodes.ConvOvfU2Un() },
                { (byte)VirtualOpCode.ConvOvfI4Un, new OpCodes.ConvOvfI4Un() },
                { (byte)VirtualOpCode.ConvOvfU4Un, new OpCodes.ConvOvfU4Un() },
                { (byte)VirtualOpCode.ConvOvfI8Un, new OpCodes.ConvOvfI8Un() },
                { (byte)VirtualOpCode.ConvOvfU8Un, new OpCodes.ConvOvfU8Un() },
                { (byte)VirtualOpCode.ConvOvfIUn, new OpCodes.ConvOvfIUn() },
                { (byte)VirtualOpCode.ConvOvfUUn, new OpCodes.ConvOvfUUn() }
            };
        }
    }
}
