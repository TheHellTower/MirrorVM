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
                Assert.Contains(fixturePrefix + "Int32AddNative", result.ConvertedMethods);
                Assert.Contains(result.SkippedMethods, method => method.StartsWith(fixturePrefix + "DecimalAdd ", StringComparison.Ordinal));

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

                    Assert.Equal(AddNativeReference(new IntPtr(0x100000000L), -1),
                        Invoke(assembly, "NativeAddInt32", new IntPtr(0x100000000L), -1));
                    Assert.Equal(AddNativeReference(new IntPtr(7), 12),
                        Invoke(assembly, "Int32AddNative", 12, new IntPtr(7)));
                    Assert.Equal(AddNativeReference(new IntPtr(7), 12),
                        Invoke(assembly, "NativeAdd", new IntPtr(7), new IntPtr(12)));

                    Assert.Equal(typeof(OverflowException), InvokeException(assembly, "AddUInt64Checked", ulong.MaxValue, 1UL).GetType());
                    Assert.Equal(NumericFixtures.DecimalAdd(1.25m, 2.5m), Invoke(assembly, "DecimalAdd", 1.25m, 2.5m));
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
