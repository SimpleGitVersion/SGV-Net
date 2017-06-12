namespace CSemVer
{
    /// <summary>
    /// Implements <see cref="CSVersionKind"/> enum extension methods.
    /// </summary>
    public static class CSVersionKindExtensions
    {
        /// <summary>
        /// Returns true if this tag is marked with <see cref="CSVersionKind.MarkedInvalid"/>.
        /// </summary>
        /// <param name="this"></param>
        /// <returns>True if MarkedInvalid.</returns>
        public static bool IsMarkedInvalid(this CSVersionKind @this)
        {
            return (@this & CSVersionKind.MarkedInvalid) != 0;
        }

        /// <summary>
        /// Obtains the marker as a string. <see cref="string.Empty"/> if this is nor marked.
        /// </summary>
        /// <param name="this">This <see cref="CSVersionKind"/>.</param>
        /// <param name="prefixPlus">Optionally removes the '+' build meta separator.</param>
        /// <returns>A string with the marker if any.</returns>
        public static string ToStringMarker(this CSVersionKind @this, bool prefixPlus = true)
        {
            if ((@this & CSVersionKind.MarkedInvalid) != 0)
            {
                return prefixPlus ? "+invalid" : "invalid";
            }
            return string.Empty;
        }


    }

}
