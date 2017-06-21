using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    public partial class CSVersion
    {
        /// <summary>
        /// The invalid standard informational version is "0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z".
        /// <para>
        /// These default, invalid, values may be set in a csproj:
        /// <code>
        ///     &lt;Version&gt;0.0.0-0&lt;/Version&gt;
        ///     &lt;AssemblyVersion&gt;0.0.0&lt;/AssemblyVersion&gt;
        ///     &lt;FileVersion&gt;0.0.0.0&lt;/FileVersion&gt;
        ///     &lt;InformationalVersion&gt;0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z&lt;/InformationalVersion&gt;
        /// </code>
        /// </para>
        /// </summary>
        static public readonly string InvalidInformationalVersion = "0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z";

        /// <summary>
        /// The invalid assembly version is "0.0".
        /// </summary>
        static public readonly string InvalidAssemblyVersion = "0.0.0";

        /// <summary>
        /// The invalid file version is "0.0.0.0".
        /// </summary>
        static public readonly string InvalidFileVersion = "0.0.0.0";

        /// <summary>
        /// The invalid SemVer version is "0.0.0-0".
        /// This is a syntaxically valid SemVer version.
        /// </summary>
        static public readonly string InvalidSemanticVersion = "0.0.0-0";

        /// <summary>
        /// The invalid commit SHA1 is "0000000000000000000000000000000000000000".
        /// </summary>
        static public readonly string InvalidCommitSha = "0000000000000000000000000000000000000000";

        /// <summary>
        /// The invalid commit SHA1 is <see cref="DateTime.MinValue"/> in <see cref="DateTimeKind.Utc"/>.
        /// </summary>
        static public readonly DateTime InvalidCommitDate = DateTime.SpecifyKind( DateTime.MinValue, DateTimeKind.Utc );

        /// <summary>
        /// The invalid NuGetV2 version is "0.0.0-0" (this is the same as the <see cref="InvalidSemanticVersion"/>).
        /// This is a syntaxically valid NuGetV2 version.
        /// </summary>
        static public readonly string InvalidNuGetV2Version = "0.0.0-0";


    }
}
