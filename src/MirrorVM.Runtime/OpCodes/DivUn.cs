// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class DivUn : BinaryArithmeticOpCode
    {
        public override string Name { get { return "DivUn"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.DivideUnsigned, left, right);
        }
    }
}
