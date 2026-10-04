// SPDX-License-Identifier: AGPL-3.0-or-later
using System;

namespace MirrorVM.Sample
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine(Add(40, 2));
            Console.WriteLine(AddUnsigned(40U, 2U));
            Console.WriteLine(Subtract(44, 2));
            Console.WriteLine(AddThenSubtract(40, 4, 2));
            Console.WriteLine(MyString());

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
