// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;
using System.Text;

namespace MirrorVM.Protector
{
    public sealed class ProtectionResult
    {
        internal ProtectionResult(string outputPath, IList<string> convertedMethods, IList<string> skippedMethods)
        {
            OutputPath = outputPath;
            ConvertedMethods = convertedMethods;
            SkippedMethods = skippedMethods;
        }

        public string OutputPath { get; private set; }

        public IList<string> ConvertedMethods { get; private set; }

        public IList<string> SkippedMethods { get; private set; }
    }

    internal static class MethodListFormatter
    {
        public static void AppendIndented(StringBuilder message, IList<string> methods)
        {
            for (int index = 0; index < methods.Count; index++)
            {
                message.AppendLine();
                message.Append("  ");
                message.Append(methods[index]);
            }
        }
    }
}
