// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class SubOvfUn : BinaryArithmeticOpCode
    {
        public override string Name { get { return "SubOvfUn"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.SubtractCheckedUnsigned, left, right);
        }
    }
}
