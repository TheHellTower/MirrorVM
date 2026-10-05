// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MirrorVM.Tests")]

namespace MirrorVM.Sample
{
    internal static class Program
    {
        private static void Main()
        {
            NumericClrChecks.Run();
            Console.WriteLine(IntPtr.Size);
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Console.WriteLine(Add(40, 2));
            Console.WriteLine(AddUnsigned(40U, 2U));
            Console.WriteLine(Subtract(44, 2));
            Console.WriteLine(AddThenSubtract(40, 4, 2));
            Console.WriteLine(MyString());

            Console.WriteLine(NumericFixtures.BooleanIdentity(true));
            Console.WriteLine(NumericFixtures.SByteIdentity(-101));
            Console.WriteLine(NumericFixtures.ByteIdentity(251));
            Console.WriteLine(NumericFixtures.Int16Identity(-30001));
            Console.WriteLine(NumericFixtures.UInt16Identity(60001));
            Console.WriteLine(NumericFixtures.CharIdentity('\uD7FF') == '\uD7FF');
            Console.WriteLine(NumericFixtures.Int32Identity(int.MinValue + 12));
            Console.WriteLine(NumericFixtures.UInt32Identity(uint.MaxValue));
            Console.WriteLine(NumericFixtures.Int64Identity(long.MinValue + 12));
            Console.WriteLine(NumericFixtures.UInt64Identity(ulong.MaxValue));
            Console.WriteLine(NumericFixtures.IntPtrIdentity(new IntPtr(42)).ToInt64());
            Console.WriteLine(NumericFixtures.UIntPtrIdentity(new UIntPtr(42U)).ToUInt64());
            Console.WriteLine(NumericFixtures.SingleIdentity(1.25f));
            Console.WriteLine(NumericFixtures.DoubleIdentity(2.5d));
            Console.WriteLine(NumericFixtures.DecimalIdentity(123.45m));
            Console.WriteLine(NumericFixtures.Int64Constant());
            Console.WriteLine(NumericFixtures.SingleConstant());
            Console.WriteLine(NumericFixtures.DoubleConstant());
            Console.WriteLine(NumericFixtures.AddInt64(40L, 2L));
            Console.WriteLine(NumericFixtures.AddUInt64Checked(40UL, 2UL));
            Console.WriteLine(NumericFixtures.DivideUInt64(ulong.MaxValue, 2UL));
            Console.WriteLine(NumericFixtures.AddSingle(0.25f, 0.5f));
            Console.WriteLine(NumericFixtures.AddDouble(0.25d, 0.5d));
            Console.WriteLine(NumericFixtures.SinglePrecision(16777216f, 1f, 16777216f));
            Console.WriteLine(NumericFixtures.ConvertSByte(257));
            Console.WriteLine(NumericFixtures.CheckedUInt32(42L));
            Console.WriteLine(NumericFixtures.ConvertUInt64(42U));
            Console.WriteLine(NumericFixtures.ConvertSingle(42L));
            Console.WriteLine(NumericFixtures.ConvertUInt64ToDouble(ulong.MaxValue) > 1d);
            Console.WriteLine(NumericFixtures.And(240, 51));
            Console.WriteLine(NumericFixtures.Or(240, 51));
            Console.WriteLine(NumericFixtures.Xor(240, 51));
            Console.WriteLine(NumericFixtures.Not(0));
            Console.WriteLine(NumericFixtures.ShiftLeft(1, 4));
            Console.WriteLine(NumericFixtures.ShiftRight(-16, 2));
            Console.WriteLine(NumericFixtures.ShiftRightUnsigned(0x80000000U, 31));
            Console.WriteLine(NumericFixtures.Equal(42, 42));
            Console.WriteLine(NumericFixtures.GreaterThan(42, 41));
            Console.WriteLine(NumericFixtures.GreaterThanUnsigned(uint.MaxValue, 1U));
            Console.WriteLine(NumericFixtures.LessThan(41, 42));
            Console.WriteLine(NumericFixtures.LessThanUnsigned(1U, uint.MaxValue));

            try
            {
                NumericFixtures.AddUInt64Checked(ulong.MaxValue, 1UL);
                Console.WriteLine("checked:missing-overflow");
            }
            catch (OverflowException)
            {
                Console.WriteLine("checked:overflow");
            }

            Console.ReadLine();
        }

        private static int Add(int left, int right)
        {
            return left + right;
        }

        private static uint AddUnsigned(uint left, uint right)
        {
            return checked(left + right);
        }

        private static int Subtract(int left, int right)
        {
            return left - right;
        }

        private static int AddThenSubtract(int left, int right, int adjustment)
        {
            return left + right - adjustment;
        }

        private static string MyString()
        {
            return "Test";
        }

        private static int UnsupportedBranch(int value)
        {
            if (value > 0)
            {
                return value;
            }

            return 0;
        }

        private static int UnsupportedCall(int left, int right)
        {
            return Add(left, right);
        }
    }
}
