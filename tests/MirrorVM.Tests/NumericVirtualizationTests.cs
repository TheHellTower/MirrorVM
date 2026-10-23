// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MirrorVM;
using MirrorVM.Protector;
using MirrorVM.Sample;
using Xunit;
using CilOpCodes = dnlib.DotNet.Emit.OpCodes;

namespace MirrorVM.Tests
{
    public sealed class NumericVirtualizationTests
    {
        [Fact]
        public void NumericSignaturesConstantsAndArithmeticSurviveProtection()
        {
            string directory = Path.Combine(Path.GetTempPath(), "MirrorVM-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                string inputPath = Path.Combine(directory, "MirrorVM.Sample.dll");
                File.Copy(typeof(NumericFixtures).Assembly.Location, inputPath);
                File.Copy(typeof(VirtualMachine).Assembly.Location, Path.Combine(directory, "MirrorVM.Runtime.dll"));
                RewriteNativeAddMethods(inputPath, Path.Combine(directory, "rewritten.dll"));

                ProtectionResult result = AssemblyVirtualizer.Protect(inputPath);
                string fixturePrefix = "MirrorVM.Sample.NumericFixtures::";
                Assert.Contains(fixturePrefix + "BooleanIdentity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "ByteIdentity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "UInt64Identity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "UIntPtrIdentity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "SingleIdentity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "DecimalIdentity", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "StringIdentity", result.ConvertedMethods);
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "EnumIdentity ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "StructIdentity ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "ObjectIdentity ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "ArrayIdentity ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "ByRefValue ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "GenericIdentity ", StringComparison.Ordinal));
                Assert.Contains(fixturePrefix + "Int32AddNative", result.ConvertedMethods);
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "DecimalAdd ", StringComparison.Ordinal));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "BadMergeHeight ", StringComparison.Ordinal) && method.Contains("incompatible stack heights"));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "BadMergeKind ", StringComparison.Ordinal) && method.Contains("incompatible stack kinds"));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "BadFallThrough ", StringComparison.Ordinal) && method.Contains("falls through"));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "BadLocalAssignment ", StringComparison.Ordinal) && method.Contains("before it is assigned"));
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "BadStackUnderflow ", StringComparison.Ordinal) && method.Contains("underflow"));

                string controlFlowPrefix = "MirrorVM.Sample.ControlFlowFixtures::";
                Assert.Contains(controlFlowPrefix + "Choose", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "SumTo", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "SwitchChoice", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "LongSwitchBranch", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "UnsignedAtLeast", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "FloatLessThan", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "NestedChoice", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "IsNull", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "UseOrFallback", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "LoneSurrogate", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "ChooseNullOrValue", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "VoidReturn", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "VoidConditional", result.ConvertedMethods);
                Assert.Contains(controlFlowPrefix + "FloatMerge", result.ConvertedMethods);
                Assert.Contains(fixturePrefix + "Argument299", result.ConvertedMethods);

                InvokeProtected(directory, result.OutputPath, assembly =>
                {
                    Assert.Equal(true, Invoke(assembly, "BooleanIdentity", true));
                    Assert.Equal((sbyte)-101, Invoke(assembly, "SByteIdentity", (sbyte)-101));
                    Assert.Equal((byte)251, Invoke(assembly, "ByteIdentity", (byte)251));
                    Assert.Equal((short)-30001, Invoke(assembly, "Int16Identity", (short)-30001));
                    Assert.Equal((ushort)60001, Invoke(assembly, "UInt16Identity", (ushort)60001));
                    Assert.Equal('\uD7FF', Invoke(assembly, "CharIdentity", '\uD7FF'));
                    Assert.Equal(int.MinValue + 12, Invoke(assembly, "Int32Identity", int.MinValue + 12));
                    Assert.Equal(uint.MaxValue, Invoke(assembly, "UInt32Identity", uint.MaxValue));
                    Assert.Equal(long.MinValue + 12, Invoke(assembly, "Int64Identity", long.MinValue + 12));
                    Assert.Equal(ulong.MaxValue, Invoke(assembly, "UInt64Identity", ulong.MaxValue));
                    Assert.Equal(new IntPtr(0x12345678), Invoke(assembly, "IntPtrIdentity", new IntPtr(0x12345678)));
                    Assert.Equal(new UIntPtr(0x12345678U), Invoke(assembly, "UIntPtrIdentity", new UIntPtr(0x12345678U)));
                    Assert.Equal(1.25f, Invoke(assembly, "SingleIdentity", 1.25f));
                    Assert.Equal(1.25d, Invoke(assembly, "DoubleIdentity", 1.25d));
                    Assert.Equal(123.45m, Invoke(assembly, "DecimalIdentity", 123.45m));
                    Assert.Equal("local string", Invoke(assembly, "StringIdentity", "local string"));
                    Assert.Null(Invoke(assembly, "StringIdentity", (object)null));

                    Assert.Equal(NumericFixtures.Int64Constant(), Invoke(assembly, "Int64Constant"));
                    Assert.Equal(NumericFixtures.SingleConstant(), Invoke(assembly, "SingleConstant"));
                    Assert.Equal(NumericFixtures.DoubleConstant(), Invoke(assembly, "DoubleConstant"));
                    Assert.Equal(NumericFixtures.AddInt64(40L, 2L), Invoke(assembly, "AddInt64", 40L, 2L));
                    Assert.Equal(NumericFixtures.AddUInt64Checked(40UL, 2UL), Invoke(assembly, "AddUInt64Checked", 40UL, 2UL));
                    Assert.Equal(NumericFixtures.DivideUInt64(ulong.MaxValue, 2UL), Invoke(assembly, "DivideUInt64", ulong.MaxValue, 2UL));
                    Assert.Equal(NumericFixtures.AddSingle(0.25f, 0.5f), Invoke(assembly, "AddSingle", 0.25f, 0.5f));
                    Assert.Equal(NumericFixtures.AddDouble(0.25d, 0.5d), Invoke(assembly, "AddDouble", 0.25d, 0.5d));
                    Assert.Equal(NumericFixtures.SinglePrecision(16777216f, 1f, 16777216f),
                        Invoke(assembly, "SinglePrecision", 16777216f, 1f, 16777216f));
                    Assert.Equal(NumericFixtures.ConvertSByte(257), Invoke(assembly, "ConvertSByte", 257));
                    Assert.Equal(NumericFixtures.CheckedUInt32(42L), Invoke(assembly, "CheckedUInt32", 42L));
                    Assert.Equal(NumericFixtures.ConvertUInt64(42U), Invoke(assembly, "ConvertUInt64", 42U));
                    Assert.Equal(NumericFixtures.ConvertSingle(42L), Invoke(assembly, "ConvertSingle", 42L));
                    Assert.Equal(NumericFixtures.ConvertUInt64ToDouble(ulong.MaxValue),
                        Invoke(assembly, "ConvertUInt64ToDouble", ulong.MaxValue));
                    Assert.Equal(NumericFixtures.And(240, 51), Invoke(assembly, "And", 240, 51));
                    Assert.Equal(NumericFixtures.Or(240, 51), Invoke(assembly, "Or", 240, 51));
                    Assert.Equal(NumericFixtures.Xor(240, 51), Invoke(assembly, "Xor", 240, 51));
                    Assert.Equal(NumericFixtures.Not(0), Invoke(assembly, "Not", 0));
                    Assert.Equal(NumericFixtures.ShiftLeft(1, 4), Invoke(assembly, "ShiftLeft", 1, 4));
                    Assert.Equal(NumericFixtures.ShiftRight(-16, 2), Invoke(assembly, "ShiftRight", -16, 2));
                    Assert.Equal(NumericFixtures.ShiftRightUnsigned(0x80000000U, 31),
                        Invoke(assembly, "ShiftRightUnsigned", 0x80000000U, 31));
                    Assert.Equal(NumericFixtures.Equal(42, 42), Invoke(assembly, "Equal", 42, 42));
                    Assert.Equal(NumericFixtures.GreaterThan(42, 41), Invoke(assembly, "GreaterThan", 42, 41));
                    Assert.Equal(NumericFixtures.GreaterThanUnsigned(uint.MaxValue, 1U),
                        Invoke(assembly, "GreaterThanUnsigned", uint.MaxValue, 1U));
                    Assert.Equal(NumericFixtures.LessThan(41, 42), Invoke(assembly, "LessThan", 41, 42));
                    Assert.Equal(NumericFixtures.LessThanUnsigned(1U, uint.MaxValue),
                        Invoke(assembly, "LessThanUnsigned", 1U, uint.MaxValue));

                    Assert.Equal(ControlFlowFixtures.Choose(-5), InvokeFlow(assembly, "Choose", -5));
                    Assert.Equal(ControlFlowFixtures.Choose(0), InvokeFlow(assembly, "Choose", 0));
                    Assert.Equal(ControlFlowFixtures.Choose(12), InvokeFlow(assembly, "Choose", 12));
                    Assert.Equal(ControlFlowFixtures.SumTo(10), InvokeFlow(assembly, "SumTo", 10));
                    Assert.Equal(ControlFlowFixtures.SumTo(-1), InvokeFlow(assembly, "SumTo", -1));
                    for (int value = -1; value <= 4; value++)
                    {
                        Assert.Equal(ControlFlowFixtures.SwitchChoice(value), InvokeFlow(assembly, "SwitchChoice", value));
                    }

                    using (ModuleDefMD module = ModuleDefMD.Load(inputPath))
                    {
                        MethodDef longSwitch = module.GetTypes()
                            .Single(type => type.FullName == "MirrorVM.Sample.ControlFlowFixtures")
                            .Methods.Single(method => method.Name == "LongSwitchBranch");
                        Assert.Contains(longSwitch.Body.Instructions,
                            instruction => instruction.OpCode.Code == Code.Blt);
                        Assert.Contains(longSwitch.Body.Instructions,
                            instruction => instruction.OpCode.Code == Code.Switch);
                    }

                    foreach (int value in new[] { -1, 0, 15, 39, 40 })
                    {
                        Assert.Equal(ControlFlowFixtures.LongSwitchBranch(value),
                            InvokeFlow(assembly, "LongSwitchBranch", value));
                    }

                    Assert.Equal(ControlFlowFixtures.UnsignedAtLeast(uint.MaxValue, 1U),
                        InvokeFlow(assembly, "UnsignedAtLeast", uint.MaxValue, 1U));
                    Assert.Equal(ControlFlowFixtures.UnsignedAtLeast(1U, uint.MaxValue),
                        InvokeFlow(assembly, "UnsignedAtLeast", 1U, uint.MaxValue));
                    Assert.Equal(ControlFlowFixtures.FloatLessThan(float.NaN, 1f),
                        InvokeFlow(assembly, "FloatLessThan", float.NaN, 1f));
                    Assert.Equal(ControlFlowFixtures.FloatLessThan(0.5f, 1f),
                        InvokeFlow(assembly, "FloatLessThan", 0.5f, 1f));
                    Assert.Equal(ControlFlowFixtures.NestedChoice(-6), InvokeFlow(assembly, "NestedChoice", -6));
                    Assert.Equal(ControlFlowFixtures.NestedChoice(0), InvokeFlow(assembly, "NestedChoice", 0));
                    Assert.Equal(ControlFlowFixtures.NestedChoice(4), InvokeFlow(assembly, "NestedChoice", 4));
                    Assert.Equal(ControlFlowFixtures.IsNull(null), InvokeFlow(assembly, "IsNull", (object)null));
                    Assert.Equal(ControlFlowFixtures.IsNull("text"), InvokeFlow(assembly, "IsNull", "text"));
                    Assert.Equal(ControlFlowFixtures.UseOrFallback(null), InvokeFlow(assembly, "UseOrFallback", (object)null));
                    Assert.Equal(ControlFlowFixtures.UseOrFallback("text"), InvokeFlow(assembly, "UseOrFallback", "text"));
                    Assert.Equal(ControlFlowFixtures.LoneSurrogate(), InvokeFlow(assembly, "LoneSurrogate"));
                    Assert.Equal(ControlFlowFixtures.ChooseNullOrValue(false, "text"),
                        InvokeFlow(assembly, "ChooseNullOrValue", false, "text"));
                    Assert.Equal(ControlFlowFixtures.ChooseNullOrValue(true, "text"),
                        InvokeFlow(assembly, "ChooseNullOrValue", true, "text"));

                    Assert.Equal(AddNativeReference(new IntPtr(0x100000000L), -1),
                        Invoke(assembly, "NativeAddInt32", new IntPtr(0x100000000L), -1));
                    Assert.Equal(AddNativeReference(new IntPtr(7), 12),
                        Invoke(assembly, "Int32AddNative", 12, new IntPtr(7)));
                    Assert.Equal(AddNativeReference(new IntPtr(7), 12),
                        Invoke(assembly, "NativeAdd", new IntPtr(7), new IntPtr(12)));

                    Assert.Equal(typeof(OverflowException), InvokeException(assembly, "AddUInt64Checked", ulong.MaxValue, 1UL).GetType());
                    Assert.Equal(NumericFixtures.DecimalAdd(1.25m, 2.5m), Invoke(assembly, "DecimalAdd", 1.25m, 2.5m));
                    Assert.Null(InvokeFlow(assembly, "VoidReturn"));
                    Assert.Null(InvokeFlow(assembly, "VoidConditional", true));
                    Assert.Null(InvokeFlow(assembly, "VoidConditional", false));
                    Assert.Equal(1.5d, InvokeFlow(assembly, "FloatMerge", true));
                    Assert.Equal(2.5d, InvokeFlow(assembly, "FloatMerge", false));

                    object[] arguments = new object[300];
                    for (int index = 0; index < arguments.Length; index++) arguments[index] = index;
                    Assert.Equal(299, Invoke(assembly, "Argument299", arguments));
                });
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static void RewriteNativeAddMethods(string inputPath, string rewrittenPath)
        {
            using (ModuleDefMD module = ModuleDefMD.Load(inputPath))
            {
                TypeDef fixture = module.GetTypes().Single(type => type.FullName == "MirrorVM.Sample.NumericFixtures");
                RewriteNativeAdd(fixture.Methods.Single(method => method.Name == "NativeAdd"));
                RewriteNativeAdd(fixture.Methods.Single(method => method.Name == "NativeAddInt32"));
                RewriteNativeAdd(fixture.Methods.Single(method => method.Name == "Int32AddNative"));
                RewriteIdentityMethods(fixture);
                AddHighArityMethod(module, fixture);
                AddInvalidControlFlowMethods(module, fixture);
                MethodDef badBranchTarget = fixture.Methods.Single(method => method.Name == "BadBranchTarget");
                AssertConverterRejects(badBranchTarget, "branch target outside the method");
                fixture.Methods.Remove(badBranchTarget);
                MethodDef badSwitchTarget = fixture.Methods.Single(method => method.Name == "BadSwitchTarget");
                AssertConverterRejects(badSwitchTarget, "switch target outside the method");
                fixture.Methods.Remove(badSwitchTarget);
                MethodDef badMaxStack = fixture.Methods.Single(method => method.Name == "BadMaxStack");
                AssertConverterRejects(badMaxStack, "maxstack");
                fixture.Methods.Remove(badMaxStack);
                TypeDef controlFlow = module.GetTypes().Single(type => type.FullName == "MirrorVM.Sample.ControlFlowFixtures");
                AddVoidConditionalMethod(module, controlFlow);
                AddFloatMergeMethod(module, controlFlow);
                module.Write(rewrittenPath);
            }

            File.Delete(inputPath);
            File.Move(rewrittenPath, inputPath);
        }

        private static void RewriteNativeAdd(MethodDef method)
        {
            method.Body = new CilBody { MaxStack = 2 };
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg_0));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg_1));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Add));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ret));
        }

        private static void RewriteIdentityMethods(TypeDef fixture)
        {
            string[] names =
            {
                "BooleanIdentity", "SByteIdentity", "ByteIdentity", "Int16Identity", "UInt16Identity",
                "CharIdentity", "Int32Identity", "UInt32Identity", "Int64Identity", "UInt64Identity",
                "IntPtrIdentity", "UIntPtrIdentity", "SingleIdentity", "DoubleIdentity", "DecimalIdentity",
                "StringIdentity"
            };
            foreach (string name in names)
            {
                MethodDef method = fixture.Methods.Single(candidate => candidate.Name == name);
                CilBody body = new CilBody { MaxStack = 1, InitLocals = true };
                body.Variables.Add(new Local(method.MethodSig.Params[0]));
                body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg_0));
                body.Instructions.Add(Instruction.Create(CilOpCodes.Stloc_0));
                body.Instructions.Add(Instruction.Create(CilOpCodes.Ldloc_0));
                body.Instructions.Add(Instruction.Create(CilOpCodes.Ret));
                method.Body = body;
            }
        }

        private static void AddHighArityMethod(ModuleDef module, TypeDef fixture)
        {
            MethodSig signature = MethodSig.CreateStatic(module.CorLibTypes.Int32);
            for (int index = 0; index < 300; index++)
            {
                signature.Params.Add(module.CorLibTypes.Int32);
            }

            MethodDefUser method = new MethodDefUser(
                "Argument299",
                signature,
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig);
            method.Body = new CilBody { MaxStack = 1 };
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg, method.Parameters[299]));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ret));
            fixture.Methods.Add(method);
        }

        private static void AddVoidConditionalMethod(ModuleDef module, TypeDef fixture)
        {
            Instruction falseReturn = Instruction.Create(CilOpCodes.Ret);
            MethodDefUser method = new MethodDefUser(
                "VoidConditional",
                MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Boolean),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig);
            method.Body = new CilBody { MaxStack = 1 };
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg_0));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Brfalse_S, falseReturn));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ret));
            method.Body.Instructions.Add(falseReturn);
            fixture.Methods.Add(method);
        }

        private static void AddFloatMergeMethod(ModuleDef module, TypeDef fixture)
        {
            Instruction falseValue = Instruction.Create(CilOpCodes.Ldc_R8, 2.5d);
            Instruction returnValue = Instruction.Create(CilOpCodes.Ret);
            MethodDefUser method = new MethodDefUser(
                "FloatMerge",
                MethodSig.CreateStatic(module.CorLibTypes.Double, module.CorLibTypes.Boolean),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig);
            method.Body = new CilBody { MaxStack = 1 };
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldarg_0));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Brfalse_S, falseValue));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Ldc_R4, 1.5f));
            method.Body.Instructions.Add(Instruction.Create(CilOpCodes.Br_S, returnValue));
            method.Body.Instructions.Add(falseValue);
            method.Body.Instructions.Add(returnValue);
            fixture.Methods.Add(method);
        }

        private static void AddInvalidControlFlowMethods(ModuleDef module, TypeDef fixture)
        {
            Instruction heightJoin = Instruction.Create(CilOpCodes.Nop);
            AddMethod(module, fixture, "BadMergeHeight", new CilBody { MaxStack = 1 },
                Instruction.Create(CilOpCodes.Ldc_I4_0),
                Instruction.Create(CilOpCodes.Brtrue_S, heightJoin),
                Instruction.Create(CilOpCodes.Ldc_I4_1),
                Instruction.Create(CilOpCodes.Br_S, heightJoin),
                heightJoin,
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Ret));

            Instruction kindJoin = Instruction.Create(CilOpCodes.Nop);
            Instruction doubleValue = Instruction.Create(CilOpCodes.Ldc_R8, 1d);
            AddMethod(module, fixture, "BadMergeKind", new CilBody { MaxStack = 1 },
                Instruction.Create(CilOpCodes.Ldc_I4_0),
                Instruction.Create(CilOpCodes.Brtrue_S, doubleValue),
                Instruction.Create(CilOpCodes.Ldc_I4_1),
                Instruction.Create(CilOpCodes.Br_S, kindJoin),
                doubleValue,
                Instruction.Create(CilOpCodes.Br_S, kindJoin),
                kindJoin,
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Nop),
                Instruction.Create(CilOpCodes.Ret));

            AddMethod(module, fixture, "BadFallThrough", new CilBody { MaxStack = 1 },
                Instruction.Create(CilOpCodes.Ldc_I4_1));

            AddMethod(module, fixture, "BadMaxStack", new CilBody { MaxStack = 0 },
                Instruction.Create(CilOpCodes.Ldc_I4_1),
                Instruction.Create(CilOpCodes.Ret));

            CilBody localBody = new CilBody { MaxStack = 1, InitLocals = false };
            localBody.Variables.Add(new Local(module.CorLibTypes.Int32));
            Instruction localLoad = Instruction.Create(CilOpCodes.Ldloc_0);
            AddMethod(module, fixture, "BadLocalAssignment", localBody,
                Instruction.Create(CilOpCodes.Ldc_I4_0),
                Instruction.Create(CilOpCodes.Brtrue_S, localLoad),
                Instruction.Create(CilOpCodes.Ldc_I4_1),
                Instruction.Create(CilOpCodes.Stloc_0),
                Instruction.Create(CilOpCodes.Br_S, localLoad),
                localLoad,
                Instruction.Create(CilOpCodes.Ret));

            AddMethod(module, fixture, "BadStackUnderflow", new CilBody { MaxStack = 2 },
                Instruction.Create(CilOpCodes.Add),
                Instruction.Create(CilOpCodes.Ret));

            Instruction branchOutside = Instruction.Create(CilOpCodes.Ret);
            AddMethod(module, fixture, "BadBranchTarget", new CilBody { MaxStack = 0 },
                Instruction.Create(CilOpCodes.Br_S, branchOutside));

            Instruction switchOutside = Instruction.Create(CilOpCodes.Ret);
            AddMethod(module, fixture, "BadSwitchTarget", new CilBody { MaxStack = 1 },
                Instruction.Create(CilOpCodes.Ldc_I4_0),
                Instruction.Create(CilOpCodes.Switch, new Instruction[] { switchOutside }),
                Instruction.Create(CilOpCodes.Ldc_I4_0),
                Instruction.Create(CilOpCodes.Ret));
        }

        private static void AddMethod(
            ModuleDef module,
            TypeDef fixture,
            string name,
            CilBody body,
            params Instruction[] instructions)
        {
            body.KeepOldMaxStack = true;
            MethodDefUser method = new MethodDefUser(
                name,
                MethodSig.CreateStatic(module.CorLibTypes.Int32),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig);
            method.Body = body;
            foreach (Instruction instruction in instructions)
            {
                method.Body.Instructions.Add(instruction);
            }
            fixture.Methods.Add(method);
        }

        private static void AssertConverterRejects(MethodDef method, string expectedReason)
        {
            Type converter = typeof(AssemblyVirtualizer).Assembly.GetType("MirrorVM.Protector.MethodBodyConverter", true);
            MethodInfo tryConvert = converter.GetMethod("TryConvert", BindingFlags.Public | BindingFlags.Static);
            object[] arguments = { method, null, null };
            Assert.False((bool)tryConvert.Invoke(null, arguments));
            Assert.Contains(expectedReason, (string)arguments[2]);
        }

        private static void InvokeProtected(string directory, string assemblyPath, Action<Assembly> action)
        {
            ProtectedAssemblyLoadContext context = new ProtectedAssemblyLoadContext(directory);
            try
            {
                using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(assemblyPath)))
                {
                    action(context.LoadFromStream(stream));
                }
            }
            finally
            {
                context.Unload();
            }
        }

        private static object Invoke(Assembly assembly, string name, params object[] arguments)
        {
            MethodInfo method = assembly.GetType("MirrorVM.Sample.NumericFixtures", true)
                .GetMethod(name, BindingFlags.Public | BindingFlags.Static);

            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static object InvokeFlow(Assembly assembly, string name, params object[] arguments)
        {
            MethodInfo method = assembly.GetType("MirrorVM.Sample.ControlFlowFixtures", true)
                .GetMethod(name, BindingFlags.Public | BindingFlags.Static);

            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static Exception InvokeException(Assembly assembly, string name, params object[] arguments)
        {
            try
            {
                Invoke(assembly, name, arguments);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static IntPtr AddNativeReference(IntPtr value, int offset)
        {
            return IntPtr.Size == 4
                ? new IntPtr(unchecked(value.ToInt32() + offset))
                : new IntPtr(unchecked(value.ToInt64() + offset));
        }

        private sealed class ProtectedAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly string _directory;

            public ProtectedAssemblyLoadContext(string directory)
                : base("MirrorVM numeric fixture", true)
            {
                _directory = directory;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (assemblyName.Name == "MirrorVM.Runtime")
                {
                    return typeof(VirtualMachine).Assembly;
                }

                string path = Path.Combine(_directory, assemblyName.Name + ".dll");
                return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
            }
        }
    }
}
