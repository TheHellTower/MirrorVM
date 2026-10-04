// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Add : BinaryArithmeticOpCode
    {
        public override string Name
        {
            get { return "Add"; }
        }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.Add, left, right);
        }
    }
}
