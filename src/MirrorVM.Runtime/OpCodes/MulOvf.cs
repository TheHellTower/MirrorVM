// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class MulOvf : BinaryArithmeticOpCode
    {
        public override string Name { get { return "MulOvf"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.MultiplyChecked, left, right);
        }
    }
}
