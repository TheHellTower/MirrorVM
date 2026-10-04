// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Mul : BinaryArithmeticOpCode
    {
        public override string Name
        {
            get { return "Mul"; }
        }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.Multiply, left, right);
        }
    }
}
