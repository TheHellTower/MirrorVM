// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class AddOvf : BinaryArithmeticOpCode
    {
        public override string Name { get { return "AddOvf"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.AddChecked, left, right);
        }
    }
}
