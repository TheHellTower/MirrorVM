// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.OpCodes
{
    public sealed class RemUn : BinaryArithmeticOpCode
    {
        public override string Name { get { return "RemUn"; } }

        protected override object Calculate(object left, object right)
        {
            return NumericArithmetic.Apply(ArithmeticOperation.RemainderUnsigned, left, right);
        }
    }
}
