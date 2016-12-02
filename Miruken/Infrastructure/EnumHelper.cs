namespace Miruken.Infrastructure
{
    using System;

    public static class EnumHelper
    {
        public static bool TryParse<T>(string text, T defaultVal, out T val)
        {
            try
            {
                var e = (T)Enum.Parse(typeof(T), text, true);
                if (Enum.IsDefined(typeof(T), e))
                {
                    val = e;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            val = defaultVal;
            return false;
        }
    }
}
