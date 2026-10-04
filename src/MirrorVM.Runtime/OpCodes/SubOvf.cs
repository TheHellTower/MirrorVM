// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class SubOvf : BinaryArithmeticOpCode
    {
        public override string Name { get { return "SubOvf"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.SubtractChecked, left, right);
        }
    }
}
