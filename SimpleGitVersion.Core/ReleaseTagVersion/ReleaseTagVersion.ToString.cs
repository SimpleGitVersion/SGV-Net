using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public sealed partial class ReleaseTagVersion
    {

        /// <summary>
        /// Gets the string version in <see cref="ReleaseTagFormat.Normalized"/> format.
        /// </summary>
        /// <returns>Formated string (or <see cref="ParseErrorMessage"/> if any).</returns>
        public override string ToString()
        {
            return ToString( ReleaseTagFormat.Normalized );
        }

        /// <summary>
        /// Gets the string version in the given format.
        /// </summary>
        /// <param name="f">Format to use.</param>
        /// <param name="buildInfo">Not null to generate a </param>
        /// <param name="usePreReleaseNameFromTag">True to use <see cref="PreReleaseNameFromTag"/> instead of standardized <see cref="PreReleaseName"/>.</param>
        /// <returns>Formated string (or <see cref="ParseErrorMessage"/> if any).</returns>
        public string ToString( ReleaseTagFormat f, CIBuildDescriptor buildInfo = null, bool usePreReleaseNameFromTag = false )
        {
            if( ParseErrorMessage != null ) return ParseErrorMessage;

            if( f == ReleaseTagFormat.DottedOrderedVersion )
            {
                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", OrderedVersionMajor, OrderedVersionMinor, OrderedVersionBuild, OrderedVersionRevision );
            }

            string prName = usePreReleaseNameFromTag ? PreReleaseNameFromTag : PreReleaseName;
            switch( f )
            {
                case ReleaseTagFormat.NugetPackageV2:
                    {
                        string suffix = IsMarkedInvalid ? Marker : null;
                        bool isCIBuild = buildInfo != null && buildInfo.IsApplicable;
                        if( isCIBuild )
                        {
                            suffix = buildInfo.ToStringPadded( '-' ) + suffix;
                        }
                        if( IsPreRelease )
                        {
                            if( IsPreReleaseFix )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}-{4:00}-{5:00}-{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleaseFix, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}-{4:00}-{5:00}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleaseFix, suffix );
                            }
                            if( PreReleaseNumber > 0 )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}-{4:00}-00-{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}-{4:00}{5}", Major, Minor, Patch, prName, PreReleaseNumber, suffix );
                            }
                            if( isCIBuild )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}-00-00-{4}", Major, Minor, Patch, prName, suffix );
                            }
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4}", Major, Minor, Patch, prName, suffix );
                        }
                        if( isCIBuild )
                        {
                            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}--{3}", Major, Minor, Patch+1, suffix );
                        }
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}", Major, Minor, Patch, suffix );
                    }
                case ReleaseTagFormat.SemVer:
                case ReleaseTagFormat.SemVerWithMarker:
                    {
                        string suffix = f == ReleaseTagFormat.SemVerWithMarker ? Marker : string.Empty;
                        bool isCIBuild = buildInfo != null && buildInfo.IsApplicable;
                        if( isCIBuild )
                        {
                            suffix = buildInfo.ToString() + suffix;
                        }
                        if( IsPreRelease )
                        {
                            if( IsPreReleaseFix )
                            {
                                if( isCIBuild )
                                {
                                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}.{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleaseFix, suffix );
                                }
                                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleaseFix, suffix );
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
                        Debug.Assert( f == ReleaseTagFormat.Normalized );
                        if( IsPreRelease )
                        {
                            if( IsPreReleaseFix )
                            {
                                return string.Format( CultureInfo.InvariantCulture, "v{0}.{1}.{2}-{3}.{4}.{5}{6}", Major, Minor, Patch, prName, PreReleaseNumber, PreReleaseFix, Marker );
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

    }
}
