// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Neg : UnaryArithmeticOpCode
    {
        public override string Name { get { return "Neg"; } }

        protected override object Calculate(object value)
        {
            return NumericArithmetic.Negate(value);
        }
    }
}
