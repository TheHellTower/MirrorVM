// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace MirrorVM.Protector
{
    using OpCodes = global::dnlib.DotNet.Emit.OpCodes;
    public static class AssemblyVirtualizer
    {
        private const string RuntimeAssemblyName = "MirrorVM.Runtime";
        private const string RuntimeFileName = RuntimeAssemblyName + ".dll";

        public static ProtectionResult Protect(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("Provide the path to a managed .exe or .dll.", "inputPath");
            }

            string fullInputPath = Path.GetFullPath(inputPath);
            string extension = Path.GetExtension(fullInputPath);
            if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Input must have an .exe or .dll extension.", "inputPath");
            }

            if (!File.Exists(fullInputPath))
            {
                throw new FileNotFoundException("The input assembly was not found.", fullInputPath);
            }

            string outputPath = Path.Combine(
                Path.GetDirectoryName(fullInputPath),
                Path.GetFileNameWithoutExtension(fullInputPath) + "_MVM" + extension);
            if (File.Exists(outputPath))
            {
                throw new IOException("The output already exists: " + outputPath);
            }

            string runtimePath = Path.Combine(Path.GetDirectoryName(fullInputPath), RuntimeFileName);
            bool runtimeAlreadyAvailable = File.Exists(runtimePath);

            List<string> convertedMethods = new List<string>();
            List<string> skippedMethods = new List<string>();
            string temporaryAssemblyPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string temporaryRuntimePath = runtimeAlreadyAvailable
                ? null
                : runtimePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            List<KeyValuePair<string, string>> temporarySidecars = new List<KeyValuePair<string, string>>();
            List<string> installedFiles = new List<string>();

            try
            {
                using (ModuleDefMD module = LoadManagedModule(fullInputPath))
                {
                    if (module.Assembly == null)
                    {
                        throw new InvalidOperationException("The input is a managed module, not an assembly.");
                    }

                    if (!module.IsILOnly)
                    {
                        throw new InvalidOperationException("Mixed-mode assemblies and native apphosts are not supported.");
                    }

                    if (module.Assembly.HasPublicKey)
                    {
                        throw new InvalidOperationException(
                            "Strong-name signed assemblies cannot be rewritten without a signing key.");
                    }

                    IMethod runtimeEntryPoint = module.Import(GetRuntimeEntryPoint());
                    foreach (TypeDef type in module.GetTypes())
                    {
                        foreach (MethodDef method in type.Methods)
                        {
                            byte[] byteCode;
                            string reason;
                            if (!MethodBodyConverter.TryConvert(method, out byteCode, out reason))
                            {
                                skippedMethods.Add(FormatMethodName(type, method) + " — " + reason);
                                continue;
                            }

                            method.Body = CreateRuntimeStub(module, method, byteCode, runtimeEntryPoint);
                            convertedMethods.Add(FormatMethodName(type, method));
                        }
                    }

                    if (convertedMethods.Count == 0)
                    {
                        StringBuilder message = new StringBuilder("No supported methods were found. No output was written.");
                        MethodListFormatter.AppendIndented(message, skippedMethods);

                        throw new InvalidOperationException(message.ToString());
                    }

                    module.Write(temporaryAssemblyPath);
                }

                PrepareSidecars(fullInputPath, outputPath, temporarySidecars);

                if (!runtimeAlreadyAvailable)
                {
                    string bundledRuntime = Path.Combine(
                        AppContext.BaseDirectory, "runtime", "net20", RuntimeFileName);
                    File.Copy(bundledRuntime, temporaryRuntimePath, false);
                    File.Move(temporaryRuntimePath, runtimePath);
                    installedFiles.Add(runtimePath);
                }

                for (int index = 0; index < temporarySidecars.Count; index++)
                {
                    string temporaryPath = temporarySidecars[index].Key;
                    string destinationPath = temporarySidecars[index].Value;
                    File.Move(temporaryPath, destinationPath);
                    installedFiles.Add(destinationPath);
                }

                File.Move(temporaryAssemblyPath, outputPath);
            }
            catch
            {
                DeleteIfExists(temporaryAssemblyPath);
                DeleteIfExists(temporaryRuntimePath);
                for (int index = 0; index < temporarySidecars.Count; index++)
                {
                    DeleteIfExists(temporarySidecars[index].Key);
                }

                for (int index = 0; index < installedFiles.Count; index++)
                {
                    DeleteIfExists(installedFiles[index]);
                }

                throw;
            }

            return new ProtectionResult(outputPath, convertedMethods, skippedMethods);
        }

        private static ModuleDefMD LoadManagedModule(string inputPath)
        {
            try
            {
                return ModuleDefMD.Load(inputPath);
            }
            catch (BadImageFormatException exception)
            {
                throw new InvalidOperationException(
                    "Input is not a managed .NET assembly. For .NET Core and modern .NET apps, pass the managed .dll instead of its native apphost .exe.",
                    exception);
            }
        }

        private static MethodInfo GetRuntimeEntryPoint()
        {
            return typeof(VirtualMachine).GetMethod(
                "Execute",
                new Type[] { typeof(byte[]), typeof(object[]) });
        }

        private static CilBody CreateRuntimeStub(
            ModuleDef module,
            MethodDef method,
            byte[] byteCode,
            IMethod runtimeEntryPoint)
        {
            CilBody body = new CilBody();
            body.MaxStack = 5;
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, byteCode.Length));
            body.Instructions.Add(Instruction.Create(OpCodes.Newarr, module.CorLibTypes.Byte.TypeDefOrRef));
            for (int index = 0; index < byteCode.Length; index++)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, index));
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)byteCode[index]));
                body.Instructions.Add(Instruction.Create(OpCodes.Stelem_I1));
            }

            int argumentCount = method.MethodSig.Params.Count;
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, argumentCount));
            body.Instructions.Add(Instruction.Create(OpCodes.Newarr, module.CorLibTypes.Object.TypeDefOrRef));
            for (int index = 0; index < argumentCount; index++)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, index));
                body.Instructions.Add(CreateLoadArgument(method, index));
                TypeSig argumentType = method.MethodSig.Params[index];
                if (argumentType.FullName != "System.String")
                {
                    body.Instructions.Add(Instruction.Create(OpCodes.Box, argumentType.ToTypeDefOrRef()));
                }

                body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            body.Instructions.Add(Instruction.Create(OpCodes.Call, runtimeEntryPoint));
            string returnType = method.MethodSig.RetType.FullName;
            if (returnType == "System.String")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Castclass, module.CorLibTypes.String.TypeDefOrRef));
            }
            else if (returnType == "System.Int64" || returnType == "System.UInt64")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, module.CorLibTypes.Int64.TypeDefOrRef));
            }
            else if (returnType == "System.IntPtr" || returnType == "System.UIntPtr")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, module.CorLibTypes.IntPtr.TypeDefOrRef));
            }
            else if (returnType == "System.Single")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, module.CorLibTypes.Double.TypeDefOrRef));
                body.Instructions.Add(Instruction.Create(OpCodes.Conv_R4));
            }
            else if (returnType == "System.Double")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, module.CorLibTypes.Double.TypeDefOrRef));
            }
            else if (returnType == "System.Decimal")
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.MethodSig.RetType.ToTypeDefOrRef()));
            }
            else
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, module.CorLibTypes.Int32.TypeDefOrRef));
            }

            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return body;
        }

        private static Instruction CreateLoadArgument(MethodDef method, int index)
        {
            switch (index)
            {
                case 0: return Instruction.Create(OpCodes.Ldarg_0);
                case 1: return Instruction.Create(OpCodes.Ldarg_1);
                case 2: return Instruction.Create(OpCodes.Ldarg_2);
                case 3: return Instruction.Create(OpCodes.Ldarg_3);
                default: return Instruction.Create(OpCodes.Ldarg, method.Parameters[index]);
            }
        }

        private static string FormatMethodName(TypeDef type, MethodDef method)
        {
            return type.FullName + "::" + method.Name;
        }

        private static void PrepareSidecars(
            string inputPath,
            string outputPath,
            List<KeyValuePair<string, string>> temporarySidecars)
        {
            string inputBase = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath));
            string outputBase = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath));
            string[] suffixes = { ".runtimeconfig.json", ".deps.json", ".config" };

            for (int index = 0; index < suffixes.Length; index++)
            {
                string suffix = suffixes[index];
                string source = inputBase + suffix;
                if (!File.Exists(source))
                {
                    continue;
                }

                string destination = outputBase + suffix;
                string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                temporarySidecars.Add(new KeyValuePair<string, string>(temporary, destination));
                if (suffix == ".deps.json")
                {
                    UpdateDependencyManifest(source, temporary, Path.GetFileName(inputPath), Path.GetFileName(outputPath));
                }
                else
                {
                    File.Copy(source, temporary, false);
                }
            }
        }

        private static void UpdateDependencyManifest(
            string sourcePath,
            string destinationPath,
            string originalAssemblyFile,
            string protectedAssemblyFile)
        {
            JsonObject root = JsonNode.Parse(File.ReadAllText(sourcePath)) as JsonObject;
            if (root == null)
            {
                throw new InvalidDataException("The .deps.json file is not a JSON object.");
            }

            JsonObject targets = root["targets"] as JsonObject;
            JsonObject libraries = root["libraries"] as JsonObject;
            if (targets == null || libraries == null)
            {
                File.Copy(sourcePath, destinationPath, false);
                return;
            }

            string applicationLibrary = null;
            foreach (KeyValuePair<string, JsonNode> target in targets)
            {
                JsonObject targetLibraries = target.Value as JsonObject;
                if (targetLibraries == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, JsonNode> library in targetLibraries)
                {
                    JsonObject libraryData = library.Value as JsonObject;
                    JsonObject runtimeFiles = libraryData == null ? null : libraryData["runtime"] as JsonObject;
                    JsonObject runtimeTargets = libraryData == null ? null : libraryData["runtimeTargets"] as JsonObject;
                    if ((runtimeFiles != null && runtimeFiles[originalAssemblyFile] != null) ||
                        (runtimeTargets != null && runtimeTargets[originalAssemblyFile] != null))
                    {
                        applicationLibrary = library.Key;
                    }

                    RenameRuntimeFile(runtimeFiles, originalAssemblyFile, protectedAssemblyFile);
                    RenameRuntimeFile(runtimeTargets, originalAssemblyFile, protectedAssemblyFile);
                }
            }

            if (applicationLibrary != null)
            {
                string runtimeVersion = typeof(VirtualMachine).Assembly.GetName().Version.ToString();
                string packageVersion = GetPackageVersion(runtimeVersion);
                string runtimeLibrary = RuntimeAssemblyName + "/" + packageVersion;
                foreach (KeyValuePair<string, JsonNode> target in targets)
                {
                    JsonObject targetLibraries = target.Value as JsonObject;
                    JsonObject appData = targetLibraries == null ? null : targetLibraries[applicationLibrary] as JsonObject;
                    if (appData == null)
                    {
                        continue;
                    }

                    JsonObject dependencies = appData["dependencies"] as JsonObject;
                    if (dependencies == null)
                    {
                        dependencies = new JsonObject();
                        appData["dependencies"] = dependencies;
                    }

                    dependencies[RuntimeAssemblyName] = packageVersion;
                    if (targetLibraries[runtimeLibrary] == null)
                    {
                        targetLibraries[runtimeLibrary] = new JsonObject
                        {
                            ["runtime"] = new JsonObject
                            {
                                [RuntimeFileName] = new JsonObject
                                {
                                    ["assemblyVersion"] = runtimeVersion,
                                    ["fileVersion"] = runtimeVersion
                                }
                            }
                        };
                    }
                }

                if (libraries[runtimeLibrary] == null)
                {
                    libraries[runtimeLibrary] = new JsonObject
                    {
                        ["type"] = "project",
                        ["serviceable"] = false,
                        ["sha512"] = ""
                    };
                }
            }

            File.WriteAllText(destinationPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void RenameRuntimeFile(JsonObject files, string originalName, string protectedName)
        {
            if (files == null || files[originalName] == null)
            {
                return;
            }

            JsonNode value = files[originalName].DeepClone();
            files.Remove(originalName);
            files[protectedName] = value;
        }

        private static string GetPackageVersion(string assemblyVersion)
        {
            Version version = new Version(assemblyVersion);
            return version.Major + "." + version.Minor + "." + Math.Max(0, version.Build);
        }

        private static void DeleteIfExists(string path)
        {
            if (path != null)
            {
                File.Delete(path);
            }
        }
    }
}
