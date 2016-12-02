namespace Miruken.Infrastructure
{
    using System;

    public static class StringExtensions
    {
        public static bool TryParseInt(this string str, out int i)
        {
            try
            {
                i = int.Parse(str);
                return true;
            }
            catch (Exception)
            {
                i = 0;
                return false;
            }
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            return s == null || s.Trim().Length == 0;
        }
    }
}
