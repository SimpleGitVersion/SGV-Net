using System;

namespace CSemVer
{
    /// <summary>
    /// Summarizes the different version kind.
    /// </summary>
    [Flags]
    public enum CSVersionKind
    {
        /// <summary>
        /// Not a release tag.
        /// </summary>
        None = 0,

        /// <summary>
        /// The looks like a version but is syntaxically incorrect.
        /// </summary>
        Malformed = 1,

        /// <summary>
        /// This version is 'Major.Minor.Patch' only.
        /// </summary>
        OfficialRelease = 2,

        /// <summary>
        /// This version is 'Major.Minor.Patch-prerelease[.Number[.Fix]]'.
        /// </summary>
        PreRelease = 4,

        /// <summary>
        /// This version is marked with +Invalid.
        /// </summary>
        MarkedInvalid = 8
    }
}
