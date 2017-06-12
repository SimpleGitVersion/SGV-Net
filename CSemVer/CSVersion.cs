using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// CSemVer version follows [v|V]Major.Minor.Patch[-PreReleaseName.PreReleaseNumber[.PreReleaseFix]] pattern.
    /// This is a semantic version, this is the version associated to a commit 
    /// in the repository.
    /// </summary>
    public sealed partial class CSVersion : IEquatable<CSVersion>, IComparable<CSVersion>
    {
        /// <summary>
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0.
        /// </summary>
        public readonly int Major;

        /// <summary>
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0.
        /// </summary>
        public readonly int Minor;

        /// <summary>
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0.
        /// </summary>
        public readonly int Patch;

        /// <summary>
        /// When <see cref="IsValid"/> is true, necessarily not null: empty string for a release.
        /// This is the pre release name directly extracted from the text. This field does not participate to equality or comparison: 
        /// the actual, standardized, pre release name field is <see cref="PreReleaseName"/>.
        /// </summary>
        public readonly string PreReleaseNameFromTag;

        /// <summary>
        /// Gets the standard pre release name among <see cref="StandardPreReleaseNames"/>.
        /// <see cref="string.Empty"/> when this is not a pre release version.
        /// </summary>
        public string PreReleaseName => IsPreRelease ? _standardNames[PreReleaseNameIdx] : string.Empty;

        /// <summary>
        /// Gets whether this is a pre release.
        /// </summary>
        public bool IsPreRelease => PreReleaseNameIdx >= 0;

        /// <summary>
        /// When <see cref="IsPreRelease"/> is true, the this is between 0 ('alpha') and <see cref="MaxPreReleaseNameIdx"/> ('rc')
        /// otherwise this is -1.
        /// </summary>
        public readonly int PreReleaseNameIdx;

        /// <summary>
        /// Gets whether the <see cref="PreReleaseNameFromTag"/> is a standard one (always false when <see cref="IsPreRelease"/> is false).
        /// </summary>
        public bool IsPreReleaseNameStandard => IsPreRelease && (PreReleaseNameIdx != MaxPreReleaseNameIdx - 1 || StringComparer.OrdinalIgnoreCase.Equals(PreReleaseNameFromTag, _standardNames[MaxPreReleaseNameIdx - 1]));

        /// <summary>
        /// Meaningful only if <see cref="IsPreRelease"/> is true (0 when not in prerelease). Between 0 and <see cref="MaxPreReleaseNumber"/>. 
        /// </summary>
        public readonly int PreReleaseNumber;

        /// <summary>
        /// When <see cref="IsPreReleasePatch"/>, a number between 1 and <see cref="MaxPreReleaseFix"/>, otherwise 0. 
        /// </summary>
        public readonly int PreReleasePatch;

        /// <summary>
        /// Gets whether this is a pre release patch (<see cref="IsPreRelease"/> is necessarily true): <see cref="PreReleasePatch"/> number is greater than 0.
        /// </summary>
        public bool IsPreReleasePatch => PreReleasePatch > 0;

        /// <summary>
        /// Gets whether this is a patch: either <see cref="Patch"/> or <see cref="PreReleasePatch"/> are greater than 0.
        /// </summary>
        public bool IsPatch => PreReleasePatch > 0 || Patch > 0;

        /// <summary>
        /// Gets the "+invalid" marker.
        /// Normalized in lowercase and <see cref="string.Empty"/> when <see cref="IsMarkedInvalid"/> is false.
        /// </summary>
        public readonly string Marker;

        /// <summary>
        /// Gets whether this <see cref="CSVersion"/> is valid.
        /// When false, <see cref="IsMalformed"/> may be true if the <see cref="OriginalTagText"/> somehow looks 
        /// like a version.
        /// </summary>
        public bool IsValid => PreReleaseNameFromTag != null;

        /// <summary>
        /// Gets whether this <see cref="CSVersion"/> is marked with +invalid.
        /// This is the strongest form for a version: a +invalid marked version MUST annihilate any same version
        /// when the both appear on a commit.
        /// </summary>
        public bool IsMarkedInvalid => Kind.IsMarkedInvalid();

        /// <summary>
        /// Gets the strength of this version: an invalid version has a strength of 0. 
        /// For valid version, the same version in terms of <see cref="OrderedVersion"/> can be expressed with: 
        /// a <see cref="IsPreReleaseNameStandard"/> (stronger than a non standard 'prerelease' one), 
        /// and ultimately, a <see cref="IsMarkedInvalid"/> wins.
        /// </summary>
        public readonly int DefinitionStrength;

        /// <summary>
        /// The kind of version. 
        /// </summary>
        public readonly CSVersionKind Kind;

        /// <summary>
        /// Gets whether this <see cref="CSVersion"/> looks like a release tag but is not syntaxically valid: 
        /// see <see cref="ParseErrorMessage"/> for more information.
        /// </summary>
        public bool IsMalformed => (Kind & CSVersionKind.Malformed) != 0;

        /// <summary>
        /// An error message that describes the error if <see cref="IsValid"/> is false. Null otherwise.
        /// </summary>
        public readonly string ParseErrorMessage;

        /// <summary>
        /// The original text.
        /// Null when this release tag has been built from an ordered version number (new <see cref="CSVersion(long)"/>).
        /// </summary>
        public readonly string OriginalTagText;

        /// <summary>
        /// Gets the empty array singleton.
        /// </summary>
        public static readonly CSVersion[] EmptyArray = new CSVersion[0];

        /// <summary>
        /// Private full constructor. Used by <see cref="TryParse(string, bool)"/> and methods like <see cref="GetDirectSuccessors(bool, CSVersion)"/>.
        /// </summary>
        /// <param name="tag">Original text version. Can be null: the <see cref="CSVersionFormat.Normalized"/> is automatically used to compute <see cref="OriginalTagText"/>.</param>
        /// <param name="major">Major (between 0 and 99999).</param>
        /// <param name="minor">Minor (between 0 and 99999).</param>
        /// <param name="patch">Patch (between 0 and 9999).</param>
        /// <param name="preReleaseName">Not null (empty for release). Can be any string [a-z]*.</param>
        /// <param name="preReleaseNameIdx">The index in StandardPreReleaseNames.</param>
        /// <param name="preReleaseNumber">Number between 0 (for release or first prerelease) and 99.</param>
        /// <param name="preReleaseFix">Number between 0 (not a fix, first actual fix starts at 1) and 99.</param>
        /// <param name="kind">One of the <see cref="CSVersionKind"/> value. Must be coherent with the other parameters.</param>
        CSVersion(string tag, int major, int minor, int patch, string preReleaseName, int preReleaseNameIdx, int preReleaseNumber, int preReleaseFix, CSVersionKind kind)
        {
            Debug.Assert(_standardNames.Length == MaxPreReleaseNameIdx + 1);
            Debug.Assert(major >= 0 && major <= MaxMajor);
            Debug.Assert(minor >= 0 && minor <= MaxMinor);
            Debug.Assert(patch >= 0 && patch <= MaxPatch);
            Debug.Assert(preReleaseName != null);
            Debug.Assert(Regex.IsMatch(preReleaseName, "[a-z]*", RegexOptions.CultureInvariant));
            Debug.Assert(preReleaseNameIdx >= -1);
            Debug.Assert((preReleaseName.Length == 0 && preReleaseNameIdx == -1)
                            ||
                          (preReleaseName.Length > 0 && preReleaseNameIdx >= 0 && preReleaseNameIdx <= MaxPreReleaseNameIdx));
            Debug.Assert(preReleaseNumber >= 0 && preReleaseNumber <= MaxPreReleaseNumber);
            Debug.Assert(PreReleasePatch >= 0 && PreReleasePatch <= MaxPreReleaseNumber);
            Debug.Assert(kind != CSVersionKind.Malformed);
            Major = major;
            Minor = minor;
            Patch = patch;
            PreReleaseNameFromTag = preReleaseName;
            PreReleaseNameIdx = preReleaseNameIdx;
            PreReleaseNumber = preReleaseNumber;
            PreReleasePatch = preReleaseFix;
            Kind = kind;
            Marker = kind.ToStringMarker();
            OriginalTagText = tag ?? ToString();
            //
            Debug.Assert(((Kind & CSVersionKind.PreRelease) != 0) == IsPreRelease);
            _orderedVersion = new SOrderedVersion() { Number = ComputeOrderedVersion(major, minor, patch, preReleaseNameIdx, preReleaseNumber, preReleaseFix) };
            DefinitionStrength = ComputeDefinitionStrength();
        }

        /// <summary>
        /// Creates a clone of this tag, except that it is marked with "+invalid".
        /// This tag must be valid (<see cref="IsValid"/> is true), otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <returns>The "+valid" tag.</returns>
        public CSVersion MarkInvalid()
        {
            return IsMarkedInvalid ? this : new CSVersion(null, Major, Minor, Patch, PreReleaseName, PreReleaseNameIdx, PreReleaseNumber, PreReleasePatch, Kind | CSVersionKind.MarkedInvalid);
        }

        /// <summary>
        /// Computes the next possible ordered versions, from the closest one to the biggest possible bump.
        /// If <see cref="IsValid"/> is false, the list is empty.
        /// </summary>
        /// <param name="patchesOnly">True to obtain only patches to this version. False to generate the full list of valid successors (up to 43 successors).</param>
        /// <returns>Next possible versions.</returns>
        public IEnumerable<CSVersion> GetDirectSuccessors(bool patchesOnly = false)
        {
            Debug.Assert(_standardNames[0] == "alpha");
            if (IsValid)
            {
                if (IsPreRelease)
                {
                    int nextFix = PreReleasePatch + 1;
                    if (nextFix <= CSVersion.MaxPreReleaseFix)
                    {
                        yield return new CSVersion(null, Major, Minor, Patch, PreReleaseName, PreReleaseNameIdx, PreReleaseNumber, nextFix, CSVersionKind.PreRelease);
                    }
                    if (!patchesOnly)
                    {
                        int nextPrereleaseNumber = PreReleaseNumber + 1;
                        if (nextPrereleaseNumber <= CSVersion.MaxPreReleaseNumber)
                        {
                            yield return new CSVersion(null, Major, Minor, Patch, PreReleaseName, PreReleaseNameIdx, nextPrereleaseNumber, 0, CSVersionKind.PreRelease);
                        }
                        int nextPrereleaseNameIdx = PreReleaseNameIdx + 1;
                        if (nextPrereleaseNameIdx <= CSVersion.MaxPreReleaseNameIdx)
                        {
                            yield return new CSVersion(null, Major, Minor, Patch, _standardNames[nextPrereleaseNameIdx], nextPrereleaseNameIdx, 0, 0, CSVersionKind.PreRelease);
                            while (++nextPrereleaseNameIdx <= CSVersion.MaxPreReleaseNameIdx)
                            {
                                yield return new CSVersion(null, Major, Minor, Patch, _standardNames[nextPrereleaseNameIdx], nextPrereleaseNameIdx, 0, 0, CSVersionKind.PreRelease);
                            }
                        }
                        yield return new CSVersion(null, Major, Minor, Patch, string.Empty, -1, 0, 0, CSVersionKind.OfficialRelease);
                    }
                }
                else
                {
                    // A pre release version can not reach the next patch.
                    int nextPatch = Patch + 1;
                    if (nextPatch <= MaxPatch)
                    {
                        for (int i = 0; i <= MaxPreReleaseNameIdx; ++i)
                        {
                            yield return new CSVersion(null, Major, Minor, nextPatch, _standardNames[i], i, 0, 0, CSVersionKind.PreRelease);
                        }
                        yield return new CSVersion(null, Major, Minor, nextPatch, string.Empty, -1, 0, 0, CSVersionKind.OfficialRelease);
                    }
                }
                if (!patchesOnly)
                {
                    int nextMinor = Minor + 1;
                    if (nextMinor <= MaxMinor)
                    {
                        yield return new CSVersion(null, Major, nextMinor, 0, "alpha", 0, 0, 0, CSVersionKind.PreRelease);
                        if (!patchesOnly)
                        {
                            for (int i = 1; i <= MaxPreReleaseNameIdx; ++i)
                            {
                                yield return new CSVersion(null, Major, nextMinor, 0, _standardNames[i], i, 0, 0, CSVersionKind.PreRelease);
                            }
                        }
                        yield return new CSVersion(null, Major, nextMinor, 0, string.Empty, -1, 0, 0, CSVersionKind.OfficialRelease);
                    }
                    int nextMajor = Major + 1;
                    if (nextMajor <= MaxMajor)
                    {
                        yield return new CSVersion(null, nextMajor, 0, 0, "alpha", 0, 0, 0, CSVersionKind.PreRelease);
                        if (!patchesOnly)
                        {
                            for (int i = 1; i <= MaxPreReleaseNameIdx; ++i)
                            {
                                yield return new CSVersion(null, nextMajor, 0, 0, _standardNames[i], i, 0, 0, CSVersionKind.PreRelease);
                            }
                        }
                        yield return new CSVersion(null, nextMajor, 0, 0, string.Empty, -1, 0, 0, CSVersionKind.OfficialRelease);
                    }
                }
            }
        }

        /// <summary>
        /// Computes whether the given version belongs to the set or predecessors.
        /// </summary>
        /// <param name="previous">Previous version. Can be null.</param>
        /// <returns>True if previous is actually a direct predecessor.</returns>
        public bool IsDirectPredecessor(CSVersion previous)
        {
            if (!IsValid) return false;
            long num = _orderedVersion.Number;
            if (previous == null) return FirstPossibleVersions.Contains(this);
            if (previous._orderedVersion.Number >= num) return false;
            if (previous._orderedVersion.Number == num - 1L) return true;

            // Major bump greater than 1: previous can not be a direct predecessor.
            if (Major > previous.Major + 1) return false;
            // Major bump of 1: if we are the first major (Major.0.0) or one of its first prerelases (Major.0.0-alpha or Major.0.0-rc), this is fine.
            if (Major != previous.Major)
            {
                return Minor == 0 && Patch == 0 && PreReleaseNumber == 0 && PreReleasePatch == 0;
            }
            Debug.Assert(Major == previous.Major);
            // Minor bump greater than 1: previous can not be a direct predecessor.
            if (Minor > previous.Minor + 1) return false;
            // Minor bump of 1: if we are the first minor (Major.Minor.0) or one of its first prerelases (Major.Minor.0-alpha or Major.Minor.0-rc), this is fine.
            if (Minor != previous.Minor)
            {
                return Patch == 0 && PreReleaseNumber == 0 && PreReleasePatch == 0;
            }
            Debug.Assert(Major == previous.Major && Minor == previous.Minor);
            // Patch bump greater than 1: previous can not be a direct predecessor.
            if (Patch > previous.Patch + 1) return false;
            // Patch bump of 1:
            // - if previous  is a prelease, it can not be a direct predecessor (4.3.2 nor any 4.3.2-* pre releases can be reached from any 4.3.1-* versions).
            // - if we are the first minor (Major.Minor.Patch) or one of its first prerelases (Major.Minor.Patch-alpha or Major.Minor.Patch-rc), this is fine:
            //   a 4.3.1 can give bearth to 4.3.2 or 4.3.2-alpha or -rc.
            if (Patch != previous.Patch)
            {
                if (previous.IsPreRelease) return false;
                return PreReleaseNumber == 0 && PreReleasePatch == 0;
            }
            Debug.Assert(Major == previous.Major && Minor == previous.Minor && Patch == previous.Patch);
            Debug.Assert(previous.IsPreRelease, "if previous was not a prerelease, this and previous would be equal.");
            // If this is not a prerelease, it is fine: one can always bump from a prerelease version to its release version.
            if (!IsPreRelease) return true;
            Debug.Assert(IsPreRelease && previous.IsPreRelease, "Both are now necessarily pre releases.");
            Debug.Assert(PreReleaseNameIdx >= previous.PreReleaseNameIdx, "This pre release name is grater or the same as the previous one (otherwise previous would be greater than this: this has been handled at the beginning of this function).");
            // If we are a fix, there is one alternative:
            //  1 - the previous is the one just before us.
            //  2 - the previous is not the one just before us.
            // Case 1 has been handled at the top of this function (oredered version + 1): if this is a fix, previous can not be a direct predecessor here.
            if (PreReleasePatch > 0) return false;
            // This is not a fix.
            // If this is a numbered prerelease, the previous must have the same PreReleaseName.
            if (PreReleaseNumber > 0)
            {
                if (previous.PreReleaseNameIdx == PreReleaseNameIdx)
                {
                    Debug.Assert(PreReleaseNumber > previous.PreReleaseNumber, "Otherwise previous would be greater than this: this has been handled at the beginning of this function.");
                    return true;
                }
                return false;
            }
            // This is not a fix nor a numbered release: by design, this is a direct predecesor (bump from a prerelease name to a greater one).
            return true;
        }

        /// <summary>
        /// This static version handles null <paramref name="version"/> (the next versions are always <see cref="FirstPossibleVersions"/>).
        /// If the version is not valid or it it is <see cref="VeryLastVersion"/>, the list is empty.
        /// </summary>
        /// <param name="version">Any version (can be null).</param>
        /// <param name="patchesOnly">True to obtain only patches to the version. False to generate the full list of valid successors (up to 43 successors).</param>
        /// <returns>The direct successors.</returns>
        public static IEnumerable<CSVersion> GetDirectSuccessors(bool patchesOnly, CSVersion version = null)
        {
            if (version == null)
            {
                return FirstPossibleVersions;
            }
            return version.GetDirectSuccessors(patchesOnly);
        }
    }
}