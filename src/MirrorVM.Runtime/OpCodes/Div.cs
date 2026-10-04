// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class Div : BinaryArithmeticOpCode
    {
        public override string Name
        {
            get { return "Div"; }
        }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.Divide, left, right);
        }
    }
}
