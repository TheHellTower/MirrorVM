// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Sub : BinaryArithmeticOpCode
    {
        public override string Name
        {
            get { return "Sub"; }
        }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.Subtract, left, right);
        }
    }
}
