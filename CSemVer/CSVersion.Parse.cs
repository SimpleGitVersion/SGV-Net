using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CSemVer
{
    public sealed partial class CSVersion
    {
        /// <summary>
        /// Initializes a new invalid Version from a failed parsing.
        /// </summary>
        /// <param name="tag">The syntaxically invalid tag.</param>
        /// <param name="isMalformed">True if it looks like a tag but is actually not one. False if the text does not look like a tag.</param>
        /// <param name="errorMessage">Required error message.</param>
        CSVersion(string tag, bool isMalformed, string errorMessage)
        {
            Debug.Assert(tag != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(errorMessage));
            OriginalTagText = tag;
            Kind = CSVersionKind.None;
            if (isMalformed)
            {
                Kind = CSVersionKind.Malformed;
                DefinitionStrength = 1;
            }
            ParseErrorMessage = isMalformed ? string.Format("Tag '{0}': {1}", tag, errorMessage) : errorMessage;
            PreReleaseNameIdx = -1;
            PreReleasePatch = 0;
        }

        const string _noTagParseErrorMessage = "Not a release tag.";
        static Regex _regexStrict = new Regex(@"^v?(?<1>0|[1-9][0-9]*)\.(?<2>0|[1-9][0-9]*)\.(?<3>0|[1-9][0-9]*)(-(?<4>[a-z]+)(\.(?<5>0|[1-9][0-9]?)(\.(?<6>[1-9][0-9]?))?)?)?(\+(?<7>Invalid)?)?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        static Regex _regexApprox = new Regex(@"^(v|V)?(?<1>0|[1-9][0-9]*)\.(?<2>0|[1-9][0-9]*)(\.(?<3>0|[1-9][0-9]*))?(?<4>.*)?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Attempts to parse a string like "4.0.0", "1.0-5-alpha.0", "1.0-5-rc.12.87".
        /// Initial 'v' (or 'V') is optional (GitHub convention).
        /// Numbers can not start with a 0 (except if it is 0).
        /// The pre release name (alpha, beta, gamma, ..., rc) must be any number of a-z (all lower case, no digit nor underscore).
        /// The pre release name can be followed by ".0" or a greater number (not greater than <see cref="MaxPreReleaseNumber"/>). 
        /// Returns a Version where <see cref="CSVersion.IsValid"/> is false if the string is not valid: <see cref="ParseErrorMessage"/>
        /// gives more information.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="analyseInvalidTag">
        /// True to analyse an invalid string for a more precise error: 
        /// if the tag looks like a release tag, the <see cref="ParseErrorMessage"/> will describe the issue.
        /// </param>
        /// <returns>Resulting version (can be invalid).</returns>
        public static CSVersion TryParse(string s, bool analyseInvalidTag = false)
        {
            if (s == null) throw new ArgumentNullException();
            Match m = _regexStrict.Match(s);
            if (!m.Success)
            {
                if (analyseInvalidTag)
                {
                    m = _regexApprox.Match(s);
                    if (m.Success) return new CSVersion(s, true, SyntaxErrorHelper(s, m));
                }
                return new CSVersion(s, false, _noTagParseErrorMessage);
            }
            string sMajor = m.Groups[1].Value;
            string sMinor = m.Groups[2].Value;
            string sPatch = m.Groups[3].Value;

            int major, minor, patch;
            if (!Int32.TryParse(sMajor, out major) || major > MaxMajor) return new CSVersion(s, true, string.Format("Incorrect Major version. Must not be greater than {0}.", MaxMajor));
            if (!Int32.TryParse(sMinor, out minor) || minor > MaxMinor) return new CSVersion(s, true, string.Format("Incorrect Minor version. Must not be greater than {0}.", MaxMinor));
            if (!Int32.TryParse(sPatch, out patch) || patch > MaxPatch) return new CSVersion(s, true, string.Format("Incorrect Patch version. Must not be greater than {0}.", MaxPatch));

            string sPRName = m.Groups[4].Value;
            string sPRNum = m.Groups[5].Value;
            string sPRFix = m.Groups[6].Value;
            string sBuldMetaData = m.Groups[7].Value;

            int prNameIdx = GetPreReleaseNameIdx(sPRName);
            int prNum = 0;
            int prFix = 0;
            if (prNameIdx >= 0)
            {
                if (sPRFix.Length > 0) prFix = Int32.Parse(sPRFix);
                if (sPRNum.Length > 0) prNum = Int32.Parse(sPRNum);
                if (prFix == 0 && prNum == 0 && sPRNum.Length > 0) return new CSVersion(s, true, string.Format("Incorrect '.0' Release Number version. 0 can appear only to fix the first pre release (ie. '.0.F' where F is between 1 and {0}).", MaxPreReleaseFix));
            }
            CSVersionKind kind = prNameIdx >= 0 ? CSVersionKind.PreRelease : CSVersionKind.OfficialRelease;
            if (sBuldMetaData.Length > 0) kind |= CSVersionKind.MarkedInvalid;
            return new CSVersion(s, major, minor, patch, sPRName, prNameIdx, prNum, prFix, kind);
        }

        static Regex _regexApproxSuffix = new Regex(@"^(-(?<1>.*?))?(\+(?<2>.*))?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        static string SyntaxErrorHelper(string s, Match mApproximate)
        {
            if (mApproximate.Groups[3].Length == 0) return "There must be at least 3 numbers (Major.Minor.Patch).";
            string buildMetaData = mApproximate.Groups[4].Value;
            if (buildMetaData.Length > 0)
            {
                Match mSuffix = _regexApproxSuffix.Match(buildMetaData);
                if (!mSuffix.Success) return "Major.Minor.Patch must be followed by a '-' and a pre release name (ie. 'v1.0.2-alpha') and/or a '+invalid', '+valid' or '+published' build meta data.";
                string prerelease = mSuffix.Groups[1].Value;
                string fragment = mSuffix.Groups[2].Value;
                if (prerelease.Length > 0)
                {
                    string[] dotParts = prerelease.Split('.');
                    if (!Regex.IsMatch(dotParts[0], "^[a-z]+$", RegexOptions.CultureInvariant))
                    {
                        return "Pre release name must be only alpha (a-z) and should be: " + string.Join(", ", _standardNames);
                    }
                    if (dotParts.Length > 1)
                    {
                        int prNum, prFix;
                        if (!Int32.TryParse(dotParts[1], out prNum) || prNum < 0 || prNum > MaxPreReleaseNumber) return string.Format("Pre Release Number must be between 1 and {0}.", MaxPreReleaseNumber);
                        if (dotParts.Length > 2)
                        {
                            if (!Int32.TryParse(dotParts[2], out prFix) || prFix < 1 || prFix > MaxPreReleaseFix) return string.Format("Fix Number must be between 1 and {0}.", MaxPreReleaseFix);
                        }
                        else if (prNum == 0) return string.Format("Incorrect '.0' release Number version. 0 can appear only to fix the first pre release (ie. '.0.XX' where XX is between 1 and {0}).", MaxPreReleaseFix);
                    }
                    if (dotParts.Length > 3) return "Too much parts: there can be at most two trailing numbers like in '-alpha.1.2'.";
                }
                if (fragment.Length > 0)
                {
                    if (!fragment.Equals("invalid", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Invalid build meta data: can only be '+invalid'.";
                    }
                }
            }
            return "Invalid tag. Valid examples are: '1.0.0', '1.0.0-beta', '1.0.0-beta.5', '1.0.0-rc.5.12', '3.0.12+invalid'";
        }

        /// <summary>
        /// Computes the pre release name index ('alpha' is 0, 'rc' is <see cref="MaxPreReleaseNameIdx"/>).
        /// This is -1 if the pre release name is empty (an empty pre release name defines a release).
        /// The lookup into <see cref="StandardPreReleaseNames"/> is case sensitive.
        /// Any unmatched pre release name is <see cref="MaxPreReleaseNameIdx"/> - 1 ('prerelease', the last one before 'rc').
        /// </summary>
        /// <param name="preReleaseName">Pre release name.</param>
        /// <returns>Index between -1 (release) and MaxPreReleaseNameIdx.</returns>
        public static int GetPreReleaseNameIdx(string preReleaseName)
        {
            if (preReleaseName == null) throw new ArgumentNullException();
            if (preReleaseName.Length == 0) return -1;
            int prNameIdx = Array.IndexOf(_standardNames, preReleaseName);
            if (prNameIdx < 0) prNameIdx = MaxPreReleaseNameIdx - 1;
            return prNameIdx;
        }

        /// <summary>
        /// Standard TryParse pattern that returns a boolean rather than the resulting <see cref="CSVersion"/>. See <see cref="TryParse(string,bool)"/>.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="v">Resulting version.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse(string s, out CSVersion v)
        {
            v = TryParse(s, analyseInvalidTag: false);
            return v.IsValid;
        }

    }
}