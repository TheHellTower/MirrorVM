// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class AddOvfUn : BinaryArithmeticOpCode
    {
        public override string Name { get { return "AddOvfUn"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.AddCheckedUnsigned, left, right);
        }
    }
}
