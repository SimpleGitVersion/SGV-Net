using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// Parses a standard informational version in order to extract the <see cref="CSVersion"/>, 
    /// the <see cref="CommitSha"/> and the <see cref="CommitDate"/> if possible.
    /// </summary>
    public class InformationalVersion
    {
        static Regex _r = new Regex( @"^(?<1>.*?) \((?<2>.*?)\) - SHA1: (?<3>.*?) - CommitDate: (?<4>.*?)$" );

        /// <summary>
        /// The invalid <see cref="InformationalVersion"/>.
        /// </summary>
        static public InformationalVersion Invalid = new InformationalVersion();

        /// <summary>
        /// Initializes a new <see cref="InformationalVersion"/>.
        /// </summary>
        /// <param name="informationalVersion">Informational version. Can be null.</param>
        public InformationalVersion( string informationalVersion )
        {
            if( (OriginalInformationalVersion = informationalVersion) != null )
            {
                Match m = _r.Match( informationalVersion );
                if( m.Success )
                {
                    SemVersion = m.Groups[1].Value;
                    NuGetVersion = m.Groups[2].Value;
                    CommitSha = m.Groups[3].Value;
                    CommitDate = DateTime.Parse( m.Groups[4].Value );
                    CSVersion v = CSVersion.TryParse( SemVersion );
                    if( v.IsValid ) Version = v;
                }
            }
        }

        InformationalVersion()
        {
            OriginalInformationalVersion = CSVersion.InvalidInformationalVersion;
            SemVersion = CSVersion.InvalidSemanticVersion;
            NuGetVersion = CSVersion.InvalidNuGetV2Version;
            CommitSha = CSVersion.InvalidCommitSha;
            CommitDate = CSVersion.InvalidCommitDate;
        }

        /// <summary>
        /// Gets the original informational  (can be null).
        /// </summary>
        public string OriginalInformationalVersion { get; }

        /// <summary>
        /// Gets the semantic version string extracted from <see cref="OriginalInformationalVersion"/>. 
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string SemVersion { get; }

        /// <summary>
        /// Gets the successfully parsed <see cref="SemVersion"/> or null if parse failed
        /// or the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public CSVersion Version { get; }

        /// <summary>
        /// Gets the NuGet version extracted from the <see cref="OriginalInformationalVersion"/>.
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string NuGetVersion { get; }

        /// <summary>
        /// Gets the SHA1 extracted from the <see cref="OriginalInformationalVersion"/>.
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string CommitSha { get; }

        /// <summary>
        /// Gets the commit date  extracted from the <see cref="InformationalVersion"/>.
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public DateTime CommitDate { get; }

        /// <summary>
        /// Overridden to return OriginalInformationalVersion or "[null OriginalInformationalVersion]".
        /// </summary>
        /// <returns></returns>
        public override string ToString() => OriginalInformationalVersion ?? "[null OriginalInformationalVersion]";

    }

}
