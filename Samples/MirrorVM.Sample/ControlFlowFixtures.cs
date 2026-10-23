// SPDX-License-Identifier: AGPL-3.0-or-later
namespace MirrorVM.Sample
{
    public static class ControlFlowFixtures
    {
        public static int Choose(int value)
        {
            if (value < 0) return -1;
            if (value == 0) return 0;
            return 1;
        }

        public static int SumTo(int limit)
        {
            int sum = 0;
            for (int value = 1; value <= limit; value++)
            {
                sum += value;
            }

            return sum;
        }

        public static int SwitchChoice(int value)
        {
            switch (value)
            {
                case 0: return 10;
                case 1: return 20;
                case 2: return 30;
                case 3: return 40;
                default: return -1;
            }
        }

        public static int LongSwitchBranch(int value)
        {
            if (value >= 0)
            {
                switch (value)
                {
                    case 0: return 0;
                    case 1: return 1;
                    case 2: return 2;
                    case 3: return 3;
                    case 4: return 4;
                    case 5: return 5;
                    case 6: return 6;
                    case 7: return 7;
                    case 8: return 8;
                    case 9: return 9;
                    case 10: return 10;
                    case 11: return 11;
                    case 12: return 12;
                    case 13: return 13;
                    case 14: return 14;
                    case 15: return 15;
                    case 16: return 16;
                    case 17: return 17;
                    case 18: return 18;
                    case 19: return 19;
                    case 20: return 20;
                    case 21: return 21;
                    case 22: return 22;
                    case 23: return 23;
                    case 24: return 24;
                    case 25: return 25;
                    case 26: return 26;
                    case 27: return 27;
                    case 28: return 28;
                    case 29: return 29;
                    case 30: return 30;
                    case 31: return 31;
                    case 32: return 32;
                    case 33: return 33;
                    case 34: return 34;
                    case 35: return 35;
                    case 36: return 36;
                    case 37: return 37;
                    case 38: return 38;
                    case 39: return 39;
                }
            }

            return -1;
        }

        public static bool UnsignedAtLeast(uint value, uint bound)
        {
            return value >= bound;
        }

        public static int FloatLessThan(float value, float bound)
        {
            if (value < bound) return 1;
            return 0;
        }

        public static int NestedChoice(int value)
        {
            int result;
            if (value < 0)
            {
                result = -value;
            }
            else if (value == 0)
            {
                result = 10;
            }
            else
            {
                result = value + 1;
            }

            return result;
        }

        public static bool IsNull(string value)
        {
            return value == null;
        }

        public static string UseOrFallback(string value)
        {
            if (value == null) return "fallback";
            return value;
        }

        public static string LoneSurrogate()
        {
            return "\uD800";
        }

        public static string ChooseNullOrValue(bool chooseValue, string value)
        {
            return chooseValue ? value : null;
        }

        public static void VoidReturn()
        {
        }
    }
}
