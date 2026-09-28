namespace Earshot.Core.Model
{
    public static class TextCheck
    {
        /// <summary>
        /// A localised string is shown only if it is non-blank and carries no unresolved-token marks.
        /// Valheim renders a missing key as "[key]", so '[' or ']' means a broken token, and '$'
        /// means one that was never localised.
        /// </summary>
        public static bool IsClean(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return text.IndexOf('[') < 0 && text.IndexOf(']') < 0 && text.IndexOf('$') < 0;
        }
    }
}
