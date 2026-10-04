// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Generic;

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
}
