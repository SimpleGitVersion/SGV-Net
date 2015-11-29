using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Defines which set of possible versions must be used to consider a version tag as a valid one.
    /// Default is 'Restricted': Restricted mode prevents a non-patch version to be produced whenever
    /// a greater version exists in the repository. 'AllSuccessors' mode considers all valid successors: this can be used
    /// on "Long Term Support" branches since this allows a 4.3.0 to be released even if a 5.0.0 version exists. 
    /// </summary>
    public enum PossibleVersionsMode
    {
        /// <summary>
        /// Defaults to <see cref="Restricted"/>.
        /// </summary>
        Default = 0,
        /// <summary>
        /// All possible versions are allowed.
        /// This is the default that allows the release of a non-patch version (ie. 2.1.0) even
        /// when a greater version exists in the repository (ie. 3.0.0).
        /// </summary>
        AllSuccessors = 1,
        /// <summary>
        /// Restricted mode prevents a non-patch version to be produced whenever
        /// a greater version exists in the repository.
        /// </summary>
        Restricted = 2
    }

    internal static class PossibleVersionsModeExtension
    {
        public static bool IsStrict( this PossibleVersionsMode @this )
        {
            return @this == PossibleVersionsMode.Default || @this == PossibleVersionsMode.Restricted;
        }
    }
}
