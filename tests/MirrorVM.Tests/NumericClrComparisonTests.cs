// SPDX-License-Identifier: AGPL-3.0-or-later
using MirrorVM.Sample;
using Xunit;

namespace MirrorVM.Tests
{
    public sealed class NumericClrComparisonTests
    {
        [Fact]
        public void NumericOpcodesMatchClrEmittedCil()
        {
            NumericClrChecks.Run();
        }
    }
}
