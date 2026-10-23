// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using MirrorVM;
using MirrorVM.OpCodes;
using Xunit;

namespace MirrorVM.Tests
{
    public sealed class VirtualMachineTests
    {
        [Fact]
        public void OpcodeNamesMatchTheirHandlers()
        {
            Assert.Equal("LoadArgument", new LoadArgument().Name);
            Assert.Equal("LoadInt32", new LoadInt32().Name);
            Assert.Equal("Ldstr", new Ldstr().Name);
            Assert.Equal("LoadInt64", new LoadInt64().Name);
            Assert.Equal("LoadSingle", new LoadSingle().Name);
            Assert.Equal("LoadDouble", new LoadDouble().Name);
            Assert.Equal("Br", new Br().Name);
            Assert.Equal("Brfalse", new Brfalse().Name);
            Assert.Equal("Brtrue", new Brtrue().Name);
            Assert.Equal("Beq", new Beq().Name);
            Assert.Equal("Bge", new Bge().Name);
            Assert.Equal("BgeUn", new BgeUn().Name);
            Assert.Equal("Bgt", new Bgt().Name);
            Assert.Equal("BgtUn", new BgtUn().Name);
            Assert.Equal("Ble", new Ble().Name);
            Assert.Equal("BleUn", new BleUn().Name);
            Assert.Equal("Blt", new Blt().Name);
            Assert.Equal("BltUn", new BltUn().Name);
            Assert.Equal("BneUn", new BneUn().Name);
            Assert.Equal("Switch", new Switch().Name);
            Assert.Equal("Ret", new Ret().Name);
            Assert.Equal("Add", new Add().Name);
            Assert.Equal("Sub", new Sub().Name);
            Assert.Equal("Mul", new Mul().Name);
            Assert.Equal("Div", new Div().Name);
            Assert.Equal("Rem", new Rem().Name);
            Assert.Equal("AddOvf", new AddOvf().Name);
            Assert.Equal("AddOvfUn", new AddOvfUn().Name);
            Assert.Equal("SubOvf", new SubOvf().Name);
            Assert.Equal("SubOvfUn", new SubOvfUn().Name);
            Assert.Equal("MulOvf", new MulOvf().Name);
            Assert.Equal("MulOvfUn", new MulOvfUn().Name);
            Assert.Equal("DivUn", new DivUn().Name);
            Assert.Equal("RemUn", new RemUn().Name);
            Assert.Equal("Neg", new Neg().Name);
        }

        [Fact]
        public void BytecodeExecutionReturnsArithmeticAsAnObject()
        {
            byte[] byteCode =
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)VirtualOpCode.LoadArgument, 1, 0,
                (byte)VirtualOpCode.Add,
                (byte)VirtualOpCode.LoadArgument, 2, 0,
                (byte)VirtualOpCode.Sub
            };

            object result = Execute(byteCode, new object[] { 40, 4, 2 });

            Assert.IsType<int>(result);
            Assert.Equal(42, result);
        }

        [Fact]
        public void LdstrPushesItsStringOntoTheSingleVmStack()
        {
            string expected = "MirrorVM ✓\uD800\0\uDC00";
            byte[] byteCode = new byte[5 + expected.Length * 2];
            byteCode[0] = (byte)VirtualOpCode.Ldstr;
            WriteInt32(byteCode, 1, expected.Length * 2);
            for (int index = 0; index < expected.Length; index++)
            {
                byteCode[5 + index * 2] = (byte)expected[index];
                byteCode[6 + index * 2] = (byte)(expected[index] >> 8);
            }

            object result = Execute(byteCode, new object[0]);

            Assert.IsType<string>(result);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EqualLdstrOperandsShareTheInternedStringReference()
        {
            const string value = "same literal";
            byte[] byteCode = new byte[2 * (5 + value.Length * 2) + 2];
            int offset = 0;
            for (int literal = 0; literal < 2; literal++)
            {
                byteCode[offset++] = (byte)VirtualOpCode.Ldstr;
                WriteInt32(byteCode, offset, value.Length * 2);
                offset += 4;
                for (int index = 0; index < value.Length; index++)
                {
                    byteCode[offset++] = (byte)value[index];
                    byteCode[offset++] = (byte)(value[index] >> 8);
                }
            }
            byteCode[offset++] = (byte)VirtualOpCode.Ceq;
            byteCode[offset] = (byte)VirtualOpCode.Ret;

            Assert.Equal(1, Execute(byteCode, new object[0]));
        }

        [Fact]
        public void LoadInt32AndArithmeticRunThroughTheCommonExecuteEntryPoint()
        {
            byte[] byteCode =
            {
                (byte)VirtualOpCode.LoadInt32, 40, 0, 0, 0,
                (byte)VirtualOpCode.LoadInt32, 2, 0, 0, 0,
                (byte)VirtualOpCode.Add
            };

            Assert.Equal(42, Execute(byteCode, new object[0]));
        }

        [Fact]
        public void CompiledBytecodeCanBeExecutedRepeatedlyWithDifferentArguments()
        {
            VirtualMachineProgram program = VirtualMachine.Compile(new byte[]
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)VirtualOpCode.LoadArgument, 1, 0,
                (byte)VirtualOpCode.Add
            });

            Assert.Equal(42, VirtualMachine.Execute(program, new object[] { 40, 2 }));
            Assert.Equal(17, VirtualMachine.Execute(program, new object[] { 8, 9 }));
        }

        [Fact]
        public void ArgumentOperandsSupportIndicesBeyondOneByte()
        {
            byte[] byteCode =
            {
                (byte)VirtualOpCode.LoadArgument, 0x2C, 0x01,
                (byte)VirtualOpCode.Ret
            };
            object[] arguments = new object[301];
            arguments[300] = 42;

            Assert.Equal(42, Execute(byteCode, arguments));
        }

        [Fact]
        public void ReturnWithoutAValueReturnsNullForVoidMethods()
        {
            Assert.Null(Execute(new byte[] { (byte)VirtualOpCode.Ret }, new object[0]));
        }

        [Fact]
        public void BranchComparisonsMatchSignedUnsignedAndUnorderedCilSemantics()
        {
            Assert.Equal(1, CompareBranch(VirtualOpCode.Beq, 42, 42));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Beq, 42, 43));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Bge, -1, 0));
            Assert.Equal(1, CompareBranch(VirtualOpCode.Bge, 1, 1));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BgeUn, -1, 1));
            Assert.Equal(1, CompareBranch(VirtualOpCode.Bgt, 2L, 1L));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BgtUn, ulong.MaxValue, 1UL));
            Assert.Equal(1, CompareBranch(VirtualOpCode.Ble, -1, 0));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BleUn, 1U, uint.MaxValue));
            Assert.Equal(1, CompareBranch(VirtualOpCode.Blt, -1L, 0L));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BltUn, 1UL, ulong.MaxValue));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BgeUn, new IntPtr(-1), new IntPtr(1)));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BneUn, 1, 2));
            Assert.Equal(0, CompareBranch(VirtualOpCode.BneUn, 1, 1));

            Assert.Equal(0, CompareBranch(VirtualOpCode.Bge, double.NaN, 0d));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BgeUn, double.NaN, 0d));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Bgt, float.NaN, 0f));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BgtUn, float.NaN, 0f));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Ble, double.NaN, 0d));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BleUn, double.NaN, 0d));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Blt, double.NaN, 0d));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BltUn, double.NaN, 0d));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BneUn, double.NaN, 0d));

            string same = new string('x', 1);
            string different = new string('x', 1);
            Assert.Equal(1, CompareBranch(VirtualOpCode.Beq, same, same));
            Assert.Equal(0, CompareBranch(VirtualOpCode.Beq, same, different));
            Assert.Equal(1, CompareBranch(VirtualOpCode.BneUn, same, different));
        }

        [Fact]
        public void TruthBranchesAndSwitchUseDecodedTargets()
        {
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brtrue, 42));
            Assert.Equal(0, TruthBranch(VirtualOpCode.Brtrue, 0));
            Assert.Equal(0, TruthBranch(VirtualOpCode.Brfalse, 42));
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brfalse, 0));
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brfalse, (object)null));
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brtrue, "value"));
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brtrue, 1L));
            Assert.Equal(0, TruthBranch(VirtualOpCode.Brtrue, 0L));
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brtrue, new IntPtr(1)));
            Assert.Equal(0, TruthBranch(VirtualOpCode.Brtrue, IntPtr.Zero));
            UIntPtr unsignedPointer = IntPtr.Size == 4 ? new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue);
            Assert.Equal(1, TruthBranch(VirtualOpCode.Brtrue, unsignedPointer));
            Assert.Equal(0, TruthBranch(VirtualOpCode.Brtrue, new UIntPtr(0U)));
            Assert.Throws<InvalidProgramException>(() => TruthBranch(VirtualOpCode.Brtrue, float.NaN));
            Assert.Throws<InvalidProgramException>(() => TruthBranch(VirtualOpCode.Brfalse, -0d));

            Assert.Equal(10, Execute(BuildSwitch(), new object[] { 0 }));
            Assert.Equal(20, Execute(BuildSwitch(), new object[] { 1 }));
            Assert.Equal(99, Execute(BuildSwitch(), new object[] { -1 }));
            Assert.Equal(99, Execute(BuildSwitch(), new object[] { 2 }));
        }

        [Fact]
        public void NullConstantsAndReferenceComparisonsUseObjectIdentity()
        {
            Assert.Null(Execute(new byte[] { (byte)VirtualOpCode.Ldnull, (byte)VirtualOpCode.Ret }, new object[0]));

            byte[] equalsNull =
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)VirtualOpCode.Ldnull,
                (byte)VirtualOpCode.Ceq,
                (byte)VirtualOpCode.Ret
            };
            Assert.Equal(1, Execute(equalsNull, new object[] { null }));
            Assert.Equal(0, Execute(equalsNull, new object[] { "text" }));

            byte[] equalLiterals =
            {
                (byte)VirtualOpCode.Ldstr, 2, 0, 0, 0, 0, 0xD8,
                (byte)VirtualOpCode.Ldstr, 2, 0, 0, 0, 0, 0xD8,
                (byte)VirtualOpCode.Ceq,
                (byte)VirtualOpCode.Ret
            };
            Assert.Equal(1, Execute(equalLiterals, new object[0]));
        }

        [Fact]
        public void LocalsUseClrDefaultsAndRoundSinglePrecisionOnStore()
        {
            Assert.Equal(0, ExecuteLocal("Int32", null, false));
            Assert.Equal(0L, ExecuteLocal("Int64", null, false));
            Assert.Equal(IntPtr.Zero, ExecuteLocal("NativeInt", null, false));
            Assert.Equal(0d, ExecuteLocal("Single", null, false));
            Assert.Equal(0d, ExecuteLocal("Double", null, false));
            Assert.Equal(0m, ExecuteLocal("Decimal", null, false));
            Assert.Null(ExecuteLocal("String", null, false));

            Assert.Equal(-1, ExecuteLocal("Int32", new object[] { uint.MaxValue }, true));
            Assert.Equal(-1L, ExecuteLocal("Int64", new object[] { ulong.MaxValue }, true));
            UIntPtr unsignedPointerMax = IntPtr.Size == 4 ? new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue);
            Assert.Equal(new IntPtr(-1), ExecuteLocal("NativeInt", new object[] { unsignedPointerMax }, true));
            Assert.Equal((double)(float)0.1d, ExecuteLocal("Single", new object[] { 0.1d }, true));
            Assert.Equal((double)0.1f, ExecuteLocal("Double", new object[] { 0.1f }, true));
            Assert.Equal(123.45m, ExecuteLocal("Decimal", new object[] { 123.45m }, true));
            Assert.Equal("local", ExecuteLocal("String", new object[] { "local" }, true));

            byte[] storedLocal =
            {
                (byte)VirtualOpCode.LoadInt32, 42, 0, 0, 0,
                (byte)VirtualOpCode.Stloc, 0, 0, 0,
                (byte)VirtualOpCode.Ldloc, 0, 0, 0,
                (byte)VirtualOpCode.Ret
            };
            Assert.Equal(42, Execute(storedLocal, new object[0]));

            double halfway = 1.0000000596046448d;
            byte[] singleLocal = new byte[9 + 4 + 4 + 1];
            int offset = 0;
            singleLocal[offset++] = (byte)VirtualOpCode.LoadDouble;
            WriteInt64(singleLocal, offset, BitConverter.DoubleToInt64Bits(halfway));
            offset += 8;
            singleLocal[offset++] = (byte)VirtualOpCode.Stloc;
            singleLocal[offset++] = 0;
            singleLocal[offset++] = 0;
            singleLocal[offset++] = 3;
            singleLocal[offset++] = (byte)VirtualOpCode.Ldloc;
            singleLocal[offset++] = 0;
            singleLocal[offset++] = 0;
            singleLocal[offset++] = 3;
            singleLocal[offset] = (byte)VirtualOpCode.Ret;
            Assert.Equal((double)1.0f, Execute(singleLocal, new object[0]));
        }

        private static object ExecuteLocal(string kindName, object[] arguments, bool store)
        {
            Type kindType = typeof(VirtualMachineProgram).Assembly.GetType("MirrorVM.VirtualMachineLocalKind", true);
            byte kind = Convert.ToByte(Enum.Parse(kindType, kindName));
            byte[] code = store
                ? new byte[]
                {
                    (byte)VirtualOpCode.LoadArgument, 0, 0,
                    (byte)VirtualOpCode.Stloc, 0, 0, kind,
                    (byte)VirtualOpCode.Ldloc, 0, 0, kind,
                    (byte)VirtualOpCode.Ret
                }
                : new byte[]
                {
                    (byte)VirtualOpCode.Ldloc, 0, 0, kind,
                    (byte)VirtualOpCode.Ret
                };
            return Execute(code, arguments ?? new object[0]);
        }

        [Fact]
        public void CompileRejectsMalformedControlFlowOperands()
        {
            Assert.Throws<InvalidProgramException>(() => VirtualMachine.Compile(new byte[]
            {
                (byte)VirtualOpCode.Br, 1, 0, 0, 0
            }));
            Assert.Throws<InvalidProgramException>(() => VirtualMachine.Compile(new byte[]
            {
                (byte)VirtualOpCode.Switch, 1, 0, 0, 0, 0
            }));
            Assert.Throws<InvalidProgramException>(() => VirtualMachine.Compile(new byte[]
            {
                (byte)VirtualOpCode.Ldloc, 0, 0, 0xFF
            }));
        }

        [Fact]
        public void NumericLoadOpcodesReadLittleEndianConstants()
        {
            long expectedInt64 = unchecked((long)0xFEDCBA9876543210UL);
            byte[] int64Code = new byte[9];
            int64Code[0] = (byte)VirtualOpCode.LoadInt64;
            WriteInt64(int64Code, 1, expectedInt64);
            Assert.Equal(expectedInt64, Execute(int64Code, new object[0]));

            float expectedSingle = 1.25f;
            byte[] singleCode = new byte[5];
            singleCode[0] = (byte)VirtualOpCode.LoadSingle;
            WriteInt32(singleCode, 1, BitConverter.ToInt32(BitConverter.GetBytes(expectedSingle), 0));
            Assert.Equal((double)expectedSingle, Execute(singleCode, new object[0]));

            double expectedDouble = -Math.PI;
            byte[] doubleCode = new byte[9];
            doubleCode[0] = (byte)VirtualOpCode.LoadDouble;
            WriteInt64(doubleCode, 1, BitConverter.ToInt64(BitConverter.GetBytes(expectedDouble), 0));
            Assert.Equal(expectedDouble, Execute(doubleCode, new object[0]));
        }

        [Fact]
        public void VmStackHasOneGenericPushAndPopPair()
        {
            VirtualMachineState state = new VirtualMachineState();

            state.Push(40);
            state.Push(2);
            state.Push(1);
            state.Push(2);
            state.Push(3);

            Assert.Equal(5, state.StackCount);
            Assert.Equal(3, state.Pop().ToObject());
            Assert.Equal(2, state.Pop().ToObject());
            Assert.Equal(1, state.Pop().ToObject());
            Assert.Equal(2, state.Pop().ToObject());
            Assert.Equal(40, state.Pop().ToObject());
            Assert.Equal(0, state.StackCount);
        }

        [Fact]
        public void ArithmeticCoversTheSupportedNumericValueKinds()
        {
            object[][] cases = new object[][]
            {
                new object[] { (sbyte)20, (sbyte)6, 26, 14, 120, 3, 2 },
                new object[] { (byte)20, (byte)6, 26, 14, 120, 3, 2 },
                new object[] { (short)20, (short)6, 26, 14, 120, 3, 2 },
                new object[] { (ushort)20, (ushort)6, 26, 14, 120, 3, 2 },
                new object[] { 'T', 'F', 154, 14, 5880, 1, 14 },
                new object[] { SmallValues.Six, SmallValues.Two, 8, 4, 12, 3, 0 },
                new object[] { 20, 6, 26, 14, 120, 3, 2 },
                new object[] { 20U, 6U, 26, 14, 120, 3, 2 },
                new object[] { 20L, 6L, 26L, 14L, 120L, 3L, 2L },
                new object[] { 20UL, 6UL, 26L, 14L, 120L, 3L, 2L },
                new object[] { true, true, 2, 0, 1, 1, 0 },
                new object[] { 20.0f, 6.0f, 26.0, 14.0, 120.0, (double)(20.0f / 6.0f), 2.0 },
                new object[] { 20.0, 6.0, 26.0, 14.0, 120.0, 20.0 / 6.0, 2.0 },
                new object[] { new IntPtr(20), new IntPtr(6), new IntPtr(26), new IntPtr(14), new IntPtr(120), new IntPtr(3), new IntPtr(2) },
                new object[] { new UIntPtr(20U), new UIntPtr(6U), new IntPtr(26), new IntPtr(14), new IntPtr(120), new IntPtr(3), new IntPtr(2) }
            };

            foreach (object[] testCase in cases)
            {
                AssertBinary(VirtualOpCode.Add, testCase[0], testCase[1], testCase[2]);
                AssertBinary(VirtualOpCode.Sub, testCase[0], testCase[1], testCase[3]);
                AssertBinary(VirtualOpCode.Mul, testCase[0], testCase[1], testCase[4]);
                AssertBinary(VirtualOpCode.Div, testCase[0], testCase[1], testCase[5]);
                AssertBinary(VirtualOpCode.Rem, testCase[0], testCase[1], testCase[6]);
            }
        }

        [Fact]
        public void CheckedOpcodesDetectSignedAndUnsignedOverflow()
        {
            AssertBinaryThrows(VirtualOpCode.AddOvf, int.MaxValue, 1, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.AddOvfUn, uint.MaxValue, 1U, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvf, int.MinValue, 1, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvfUn, 0U, 1U, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvf, int.MaxValue, 2, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvfUn, uint.MaxValue, 2U, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.AddOvf, long.MaxValue, 1L, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.AddOvfUn, ulong.MaxValue, 1UL, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvf, long.MinValue, 1L, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvfUn, 0UL, 1UL, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvf, long.MaxValue, 2L, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvfUn, ulong.MaxValue, 2UL, typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.AddOvf, NativeMaximum(), new IntPtr(1), typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.AddOvfUn, NativeUnsignedMaximum(), new UIntPtr(1U), typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvf, NativeMinimum(), new IntPtr(1), typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.SubOvfUn, UIntPtr.Zero, new UIntPtr(1U), typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvf, NativeMaximum(), new IntPtr(2), typeof(OverflowException));
            AssertBinaryThrows(VirtualOpCode.MulOvfUn, NativeUnsignedMaximum(), new UIntPtr(2U), typeof(OverflowException));
            AssertBinary(VirtualOpCode.AddOvf, int.MaxValue - 1, 1, int.MaxValue);
            AssertBinary(VirtualOpCode.AddOvfUn, 0x7ffffffeU, 1U, int.MaxValue);
            AssertBinary(VirtualOpCode.AddOvfUn, 0x7fffffffU, 1U, int.MinValue);
        }

        [Fact]
        public void UncheckedArithmeticWrapsAtEachIntegerWidth()
        {
            AssertBinary(VirtualOpCode.Add, int.MaxValue, 1, int.MinValue);
            AssertBinary(VirtualOpCode.Add, uint.MaxValue, 1U, 0);
            AssertBinary(VirtualOpCode.Add, long.MaxValue, 1L, long.MinValue);
            AssertBinary(VirtualOpCode.Add, ulong.MaxValue, 1UL, 0L);

            IntPtr nativeMax = IntPtr.Size == 4 ? new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue);
            IntPtr nativeMin = IntPtr.Size == 4 ? new IntPtr(int.MinValue) : new IntPtr(long.MinValue);
            AssertBinary(VirtualOpCode.Add, nativeMax, new IntPtr(1), nativeMin);
            AssertBinary(VirtualOpCode.Add, new IntPtr(10), 2, new IntPtr(12));
            AssertBinary(VirtualOpCode.Add, 2, new IntPtr(10), new IntPtr(12));
            AssertBinary(VirtualOpCode.Sub, new IntPtr(10), 2, new IntPtr(8));
        }

        [Fact]
        public void UnsignedDivisionAndRemainderUseOpcodeSemantics()
        {
            AssertBinary(VirtualOpCode.Div, uint.MaxValue, 2U, 0);
            AssertBinary(VirtualOpCode.DivUn, uint.MaxValue, 2U, int.MaxValue);
            AssertBinary(VirtualOpCode.RemUn, uint.MaxValue, 10U, 5);
            AssertBinary(VirtualOpCode.DivUn, ulong.MaxValue, 2UL, long.MaxValue);
            AssertBinary(VirtualOpCode.RemUn, ulong.MaxValue, 10UL, 5L);
        }

        [Fact]
        public void NativeUnsignedValuesUseProcessPointerWidth()
        {
            UIntPtr maximum = IntPtr.Size == 4
                ? new UIntPtr(uint.MaxValue)
                : new UIntPtr(ulong.MaxValue);

            AssertBinary(VirtualOpCode.Add, maximum, new UIntPtr(1U), IntPtr.Zero);
            AssertBinary(VirtualOpCode.DivUn, maximum, new UIntPtr(2U),
                IntPtr.Size == 4 ? (object)new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue));
        }

        [Fact]
        public void ArithmeticRejectsInvalidKindsAndReportsClrFailures()
        {
            AssertBinaryThrows(VirtualOpCode.DivUn, uint.MaxValue, 0U, typeof(DivideByZeroException));
            AssertBinaryThrows(VirtualOpCode.Add, 1, 2L, typeof(InvalidOperationException));
            AssertBinaryThrows(VirtualOpCode.AddOvf, 1.0, 2.0, typeof(InvalidOperationException));
            AssertBinaryThrows(VirtualOpCode.Add, decimal.MaxValue, 1m, typeof(ArgumentException));
        }

        [Fact]
        public void NegationSupportsIntegerFloatingAndNativeKinds()
        {
            AssertUnary(VirtualOpCode.Neg, int.MinValue, int.MinValue);
            AssertUnary(VirtualOpCode.Neg, 12L, -12L);
            AssertUnary(VirtualOpCode.Neg, 1.5f, -1.5d);
            AssertUnary(VirtualOpCode.Neg, 1.5, -1.5);
            AssertUnary(VirtualOpCode.Neg, new IntPtr(12), new IntPtr(-12));
        }

        [Fact]
        public void ExecuteRejectsMalformedBytecodeAndIncorrectStackResults()
        {
            Assert.Throws<ArgumentNullException>(() => Execute(null, new object[0]));
            Assert.Throws<ArgumentNullException>(() => Execute(new byte[0], null));
            Assert.Throws<InvalidProgramException>(() => Execute(new byte[0], new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(new byte[] { 0xFE }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadArgument }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadArgument, 0 }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadInt32, 1 }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadInt64, 1, 2, 3, 4 }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadSingle, 1 }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadDouble, 1, 2, 3, 4 }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.Ldstr, 4, 0, 0, 0, (byte)'T' }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.Add }, new object[0]));
            Assert.Throws<InvalidProgramException>(() => Execute(
                new byte[] { (byte)VirtualOpCode.LoadArgument, 0, 0, (byte)VirtualOpCode.LoadArgument, 1 },
                new object[] { 1, 2 }));
        }

        private static void AssertBinary(VirtualOpCode opcode, object left, object right, object expected)
        {
            object actual = Execute(
                new byte[]
                {
                    (byte)VirtualOpCode.LoadArgument, 0, 0,
                    (byte)VirtualOpCode.LoadArgument, 1, 0,
                    (byte)opcode
                },
                new object[] { left, right });

            Assert.Equal(expected, actual);
            Assert.Equal(expected.GetType(), actual.GetType());
        }

        private static int CompareBranch(VirtualOpCode opcode, object left, object right)
        {
            byte[] code =
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)VirtualOpCode.LoadArgument, 1, 0,
                (byte)opcode, 5, 0, 0, 0,
                (byte)VirtualOpCode.LoadInt32, 0, 0, 0, 0,
                (byte)VirtualOpCode.Ret,
                (byte)VirtualOpCode.LoadInt32, 1, 0, 0, 0,
                (byte)VirtualOpCode.Ret
            };
            return (int)Execute(code, new object[] { left, right });
        }

        private static int TruthBranch(VirtualOpCode opcode, object value)
        {
            byte[] code =
            {
                (byte)VirtualOpCode.LoadArgument, 0, 0,
                (byte)opcode, 4, 0, 0, 0,
                (byte)VirtualOpCode.LoadInt32, 0, 0, 0, 0,
                (byte)VirtualOpCode.Ret,
                (byte)VirtualOpCode.LoadInt32, 1, 0, 0, 0,
                (byte)VirtualOpCode.Ret
            };
            return (int)Execute(code, new object[] { value });
        }

        private static byte[] BuildSwitch()
        {
            byte[] code = new byte[3 + 1 + 4 + 8 + 5 + 1 + 5 + 1 + 5 + 1];
            int offset = 0;
            code[offset++] = (byte)VirtualOpCode.LoadArgument;
            code[offset++] = 0;
            code[offset++] = 0;
            code[offset++] = (byte)VirtualOpCode.Switch;
            WriteInt32(code, offset, 2);
            offset += 4;
            WriteInt32(code, offset, 4);
            offset += 4;
            WriteInt32(code, offset, 6);
            offset += 4;
            code[offset++] = (byte)VirtualOpCode.LoadInt32;
            WriteInt32(code, offset, 99);
            offset += 4;
            code[offset++] = (byte)VirtualOpCode.Ret;
            code[offset++] = (byte)VirtualOpCode.LoadInt32;
            WriteInt32(code, offset, 10);
            offset += 4;
            code[offset++] = (byte)VirtualOpCode.Ret;
            code[offset++] = (byte)VirtualOpCode.LoadInt32;
            WriteInt32(code, offset, 20);
            offset += 4;
            code[offset] = (byte)VirtualOpCode.Ret;
            return code;
        }

        private static void AssertBinaryThrows(
            VirtualOpCode opcode,
            object left,
            object right,
            Type exceptionType)
        {
            Exception exception = Record.Exception(() => Execute(
                new byte[]
                {
                    (byte)VirtualOpCode.LoadArgument, 0, 0,
                    (byte)VirtualOpCode.LoadArgument, 1, 0,
                    (byte)opcode
                },
                new object[] { left, right }));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
        }

        private static void AssertUnary(VirtualOpCode opcode, object value, object expected)
        {
            object actual = Execute(
                new byte[] { (byte)VirtualOpCode.LoadArgument, 0, 0, (byte)opcode },
                new object[] { value });

            Assert.Equal(expected, actual);
            Assert.Equal(expected.GetType(), actual.GetType());
        }

        private static object Execute(byte[] byteCode, object[] arguments)
        {
            return VirtualMachine.Execute(VirtualMachine.Compile(byteCode), arguments);
        }

        private static void WriteInt32(byte[] target, int offset, int value)
        {
            unchecked
            {
                target[offset] = (byte)value;
                target[offset + 1] = (byte)(value >> 8);
                target[offset + 2] = (byte)(value >> 16);
                target[offset + 3] = (byte)(value >> 24);
            }
        }

        private static void WriteInt64(byte[] target, int offset, long value)
        {
            WriteInt32(target, offset, unchecked((int)value));
            WriteInt32(target, offset + 4, unchecked((int)(value >> 32)));
        }

        private static IntPtr NativeMaximum()
        {
            return IntPtr.Size == 4 ? new IntPtr(int.MaxValue) : new IntPtr(long.MaxValue);
        }

        private static IntPtr NativeMinimum()
        {
            return IntPtr.Size == 4 ? new IntPtr(int.MinValue) : new IntPtr(long.MinValue);
        }

        private static UIntPtr NativeUnsignedMaximum()
        {
            return IntPtr.Size == 4 ? new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue);
        }

        private enum SmallValues : short
        {
            Six = 6,
            Two = 2
        }
    }
}
