// SPDX-License-Identifier: AGPL-3.0-or-later
using System;
using MirrorVM.Protector;

namespace MirrorVM
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: MirrorVM.exe <managed-assembly.exe-or-dll>");
                return 2;
            }

            try
            {
                ProtectionResult result = AssemblyVirtualizer.Protect(args[0]);
                Console.WriteLine("Output: " + result.OutputPath);
                Console.WriteLine("Virtualized methods: " + result.ConvertedMethods.Count);
                for (int index = 0; index < result.ConvertedMethods.Count; index++)
                {
                    Console.WriteLine("  " + result.ConvertedMethods[index]);
                }

                Console.WriteLine("Skipped methods: " + result.SkippedMethods.Count);
                int detailsCount = Math.Min(8, result.SkippedMethods.Count);
                for (int index = 0; index < detailsCount; index++)
                {
                    Console.WriteLine("  " + result.SkippedMethods[index]);
                }

                if (result.SkippedMethods.Count > detailsCount)
                {
                    Console.WriteLine("  ... and " + (result.SkippedMethods.Count - detailsCount) + " more");
                }

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("MirrorVM: " + exception.Message);
                return 1;
            }
        }
    }
}
