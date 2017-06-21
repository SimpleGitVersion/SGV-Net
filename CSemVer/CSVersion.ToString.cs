using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CSemVer
{
    public sealed partial class CSVersion
    {
        /// <summary>
        /// Gets the string version in <see cref="CSVersionFormat.Normalized"/> format ('v' + <see cref="CSVersionFormat.SemVerWithMarker"/>).
        /// </summary>
        /// <returns>Formated string (or <see cref="ParseErrorMessage"/> if any).</returns>
        public override string ToString()
        {
            return ToString( CSVersionFormat.Normalized );
        }

        /// <summary>
        /// Gets this version in a <see cref="CSVersionFormat.FileVersion"/> format.
        /// </summary>
        /// <param name="isCIBuild">True to indicate a CI build: the revision part (last part) is odd.</param>
        /// <returns>The Major.Minor.Build.Revision number where each part are between 0 and 65535.</returns>
        public string ToStringFileVersion( bool isCIBuild )
        {
            SOrderedVersion v = _orderedVersion;
            v.Number <<= 1;
            if( isCIBuild ) v.Revision |= 1;
            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision );
        }

        /// <summary>
        /// Gets the string version in the given format.
        /// </summary>
        /// <param name="f">Format to use.</param>
        /// <param name="buildInfo">Not null to generate a post-release version.</param>
        /// <param name="usePreReleaseNameFromTag">True to use <see cref="PreReleaseNameFromTag"/> instead of standardized <see cref="PreReleaseName"/>.</param>
        /// <returns>Formated string (or <see cref="ParseErrorMessage"/> if any).</returns>
        public string ToString( CSVersionFormat f, CIBuildDescriptor buildInfo = null, bool usePreReleaseNameFromTag = false )
        {
            if( ParseErrorMessage != null ) return ParseErrorMessage;
            if( buildInfo != null && !buildInfo.IsValid ) throw new ArgumentException( "buildInfo must be valid." );
            if( f == CSVersionFormat.FileVersion )
            {
                return ToStringFileVersion( buildInfo != null );
            }

            string prName = usePreReleaseNameFromTag ? PreReleaseNameFromTag : PreReleaseName;
            switch( f )
            {
                case CSVersionFormat.NugetPackageV2:
                    {
                        // For NuGetV2, we are obliged to use the initial otherwise the special part for a pre release fix is too long for CI-Build LastReleasedBased.
                        if( usePreReleaseNameFromTag ) throw new ArgumentException( "VersionFormat.NugetPackageV2 can not use PreReleaseNameFromTag." );
                        prName = PreReleaseNameIdx >= 0 ? _standardNames[PreReleaseNameIdx][0].ToString() : String.Empty;

                        string suffix = IsMarkedInvalid ? Marker : null;
                        bool isCIBuild = buildInfo != null;
                        if( isCIBuild && !buildInfo.IsValidForNuGetV2 ) throw new ArgumentException( "buildInfo must be valid for NuGetV2 format." );
                        if( isCIBuild )
                        {
                            suffix = buildInfo.ToStringForNuGetV2() + suffix;
                        }
                        if( IsPreRelease )
                        {
                            if( IsPreReleasePatch )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-{5:00}-{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleasePatch, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-{5:00}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleasePatch, suffix );
                            }
                            if( PreReleaseNumber > 0 )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-00-{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                            }
                            if( isCIBuild )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}00-00-{4}", Major, Minor, Patch, prName, suffix );
                            }
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4}", Major, Minor, Patch, prName, suffix );
                        }
                        if( isCIBuild )
                        {
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-C{3}", Major, Minor, Patch+1, suffix );
                        }
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}", Major, Minor, Patch, suffix );
                    }
                case CSVersionFormat.SemVer:
                case CSVersionFormat.SemVerWithMarker:
                    {
                        string suffix = f == CSVersionFormat.SemVerWithMarker ? Marker : string.Empty;
                        bool isCIBuild = buildInfo != null;
                        if( isCIBuild )
                        {
                            suffix = buildInfo.ToString() + suffix;
                        }
                        if( IsPreRelease )
                        {
                            if( IsPreReleasePatch )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}.{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleasePatch, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleasePatch, suffix );
                            }
                            if( PreReleaseNumber > 0 )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.0.{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                            }
                            if( isCIBuild )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.0.0.{4}", Major, Minor, Patch, prName, suffix );
                            }
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4}", Major, Minor, Patch, prName, suffix );
                        }
                        if( isCIBuild )
                        {
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}--{3}", Major, Minor, Patch+1, suffix );
                        }
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}", Major, Minor, Patch, suffix );
                    }
                default:
                    {
                        Debug.Assert( f == CSVersionFormat.Normalized );
                        if( IsPreRelease )
                        {
                            if( IsPreReleasePatch )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "v{0}.{1}.{2}-{3}.{4}.{5}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleasePatch, Marker );
                            }
                            if( PreReleaseNumber > 0 )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "v{0}.{1}.{2}-{3}.{4}{5}", Major, Minor, Patch, prName, PreReleaseNumber, Marker );
                            }
                            return string.Format( CultureInfo.InvariantCulture, "v{0}.{1}.{2}-{3}{4}", Major, Minor, Patch, prName, Marker );
                        }
                        return string.Format( CultureInfo.InvariantCulture, "v{0}.{1}.{2}{3}", Major, Minor, Patch, Marker );
                    }
            }
        }

        /// <summary>
        /// Gets the standard Informational version string.
        /// If <see cref="IsValid"/> is false this throws an <see cref="InvalidOperationException"/>: 
        /// the constant <see cref="InvalidInformationalVersion"/> should be used when IsValid is false.
        /// </summary>
        /// <param name="commitSha">The SHA1 of the commit (40 hex digits).</param>
        /// <param name="commitDateUtc">The commit date in UTC.</param>
        /// <returns>The informational version.</returns>
        public string GetInformationalVersion( string commitSha, DateTime commitDateUtc )
        {
            if( !IsValid ) throw new InvalidOperationException( "IsValid must be true. Use CSVersion.InvalidInformationalVersion when IsValid is false." );
            if( string.IsNullOrWhiteSpace( commitSha ) || !commitSha.All( IsHexDigit ) ) throw new ArgumentException( "Must be a 40 hex digits string.", nameof( commitSha ) );
            if( commitDateUtc.Kind != DateTimeKind.Utc ) throw new ArgumentException( "Must be a UTC date.", nameof( commitDateUtc ) );
            var semVer = ToString( CSVersionFormat.SemVer );
            var nugetVer = ToString( CSVersionFormat.NugetPackageV2 );
            return $"{semVer} ({nugetVer}) - SHA1: {commitSha} - CommitDate: {commitDateUtc.ToString( "u" )}";
        }

        static bool IsHexDigit( char c ) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}


