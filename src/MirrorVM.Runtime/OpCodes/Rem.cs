// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Rem : BinaryArithmeticOpCode
    {
        public override string Name
        {
            get { return "Rem"; }
        }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.Remainder, left, right);
        }
    }
}
