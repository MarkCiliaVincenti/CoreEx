﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CoreEx.Wildcards
{
    /// <summary>
    /// Provides a standardised approach to parsing and validating wildcard text.
    /// </summary>
    public class Wildcard
    {
        /// <summary>
        /// Gets the standard multi (zero or more) wildcard character.
        /// </summary>
        public const char MultiWildcardCharacter = '*';

        /// <summary>
        /// Gets the standard single wildcard character.
        /// </summary>
        public const char SingleWildcardCharacter = '?';

        /// <summary>
        /// Gets the space ' ' character.
        /// </summary>
        public const char SpaceCharacter = ' ';

        /// <summary>
        /// Gets the <see cref="WildcardSelection.None"/> and <see cref="WildcardSelection.Single"/> <see cref="Wildcard"/>; i.e does not directly support any wildcard characters.
        /// </summary>
        public static Wildcard None { get; } = new Wildcard(WildcardSelection.None | WildcardSelection.Equal);

        /// <summary>
        /// Gets the <see cref="WildcardSelection.MultiBasic"/> <see cref="Wildcard"/> using only the <see cref="MultiWildcardCharacter"/>.
        /// </summary>
        public static Wildcard MultiBasic { get; } = new Wildcard(WildcardSelection.MultiBasic);

        /// <summary>
        /// Gets the <see cref="WildcardSelection.MultiAll"/> <see cref="Wildcard"/> using only the <see cref="MultiWildcardCharacter"/>.
        /// </summary>
        public static Wildcard MultiAll { get; } = new Wildcard(WildcardSelection.MultiAll);

        /// <summary>
        /// Gets the <see cref="WildcardSelection.BothAll"/> <see cref="Wildcard"/> using both the <see cref="MultiWildcardCharacter"/> and <see cref="SingleWildcardCharacter"/>.
        /// </summary>
        public static Wildcard BothAll { get; } = new Wildcard(WildcardSelection.BothAll);

        /// <summary>
        /// Gets or sets the default <see cref="Wildcard"/> settings (defaults to <see cref="WildcardSelection.MultiAll"/>.
        /// </summary>
        public static Wildcard Default { get; set; } = MultiBasic;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wildcard"/> class.
        /// </summary>
        /// <param name="supported">The supported <see cref="WildcardSelection"/>.</param>
        /// <param name="multiWildcard">The .NET multi (zero or more) wildcard character (defaults to <see cref="MultiWildcardCharacter"/>).</param>
        /// <param name="singleWildcard">The .NET single wildcard character (defaults to <see cref="SingleWildcardCharacter"/>).</param>
        /// <param name="charactersNotAllowed">The list of characters that are not allowed.</param>
        /// <param name="transform">The <see cref="StringTransform"/> option for the wildcard text.</param>
        /// <param name="spaceTreatment">The <see cref="WildcardSpaceTreatment"/> that defines the treatment of embedded space ' ' characters within the wildcard.</param>
        public Wildcard(WildcardSelection supported, char multiWildcard = MultiWildcardCharacter, char singleWildcard = SingleWildcardCharacter, char[]? charactersNotAllowed = null,
            WildcardSpaceTreatment spaceTreatment = WildcardSpaceTreatment.None, StringTransform transform = StringTransform.EmptyToNull)
        {
            if (supported == WildcardSelection.Undetermined || supported.HasFlag(WildcardSelection.InvalidCharacter))
                throw new ArgumentException("A Wildcard cannot be configured with Undetermined and/or InvalidCharacter supported selection(s).", nameof(supported));

            if (multiWildcard != char.MinValue && singleWildcard != char.MinValue && multiWildcard == singleWildcard)
                throw new ArgumentException("A Wildcard cannot be configured with the same character for the MultiWildcard and SingleWildcard.", nameof(multiWildcard));

            if (charactersNotAllowed != null && (charactersNotAllowed.Contains(multiWildcard) || charactersNotAllowed.Contains(singleWildcard)))
                throw new ArgumentException("A Wildcard cannot be configured with either the MultiWildcard or SingleWildcard in the CharactersNotAllowed list.", nameof(charactersNotAllowed));

            if (supported.HasFlag(WildcardSelection.MultiWildcard) && multiWildcard == char.MinValue)
                throw new ArgumentException("A Wildcard that supports MultiWildcard must also define the MultiWildcard character.");

            if (supported.HasFlag(WildcardSelection.SingleWildcard) && singleWildcard == char.MinValue)
                throw new ArgumentException("A Wildcard that supports SingleWildcard must also define the SingleWildcard character.");

            Supported = supported;
            MultiWildcard = multiWildcard;
            SingleWildcard = singleWildcard;
            CharactersNotAllowed = new ReadOnlyCollection<char>(charactersNotAllowed != null ? (char[])charactersNotAllowed.Clone() : []);
            SpaceTreatment = spaceTreatment;
            Transform = transform;
        }

        /// <summary>
        /// Gets the supported <see cref="WildcardSelection"/>.
        /// </summary>
        public WildcardSelection Supported { get; private set; }

        /// <summary>
        /// Gets the .NET multi (zero or more) wildcard character.
        /// </summary>
        /// <remarks>A value of <see cref="char.MinValue"/> indicates no multi wildcard support.</remarks>
        public char MultiWildcard { get; private set; }

        /// <summary>
        /// Gets the .NET single wildcard character.
        /// </summary>
        /// <remarks>A value of <see cref="char.MinValue"/> indicates no single wildcard support.</remarks>
        public char SingleWildcard { get; private set; }

        /// <summary>
        /// Gets the list of characters that are not allowed.
        /// </summary>
        public IReadOnlyList<char> CharactersNotAllowed { get; private set; }

        /// <summary>
        /// Gets the <see cref="StringTransform"/> option for the wildcard text.
        /// </summary>
        public StringTransform Transform { get; private set; }

        /// <summary>
        /// Gets the <see cref="WildcardSpaceTreatment"/> that defines the treatment of embedded space ' ' characters within the wildcard.
        /// </summary>
        public WildcardSpaceTreatment SpaceTreatment { get; private set; }

        /// <summary>
        /// Validates the wildcard text against what is <see cref="Supported"/> to ensure validity.
        /// </summary>
        /// <param name="text">The wildcard text.</param>
        /// <returns><c>true</c> indicates that the text is valid; otherwise, <c>false</c> for invalid.</returns>
        /// <remarks>Note that leading and trailing spaces are ignored.</remarks>
        public bool Validate(string? text) => !Parse(text).HasError;

        /// <summary>
        /// Validates the <paramref name="selection"/> against what is <see cref="Supported"/> to ensure validity.
        /// </summary>
        /// <param name="selection">The <see cref="WildcardSelection"/> to validate.</param>
        /// <returns><c>true</c> indicates that the selection is valid; otherwise, <c>false</c> for invalid.</returns>
        public bool Validate(WildcardSelection selection)
        {
            if ((selection.HasFlag(WildcardSelection.None) && !Supported.HasFlag(WildcardSelection.None)) ||
                (selection.HasFlag(WildcardSelection.Equal) && !Supported.HasFlag(WildcardSelection.Equal)) ||
                (selection.HasFlag(WildcardSelection.Single) && !Supported.HasFlag(WildcardSelection.Single)) ||
                (selection.HasFlag(WildcardSelection.StartsWith) && !Supported.HasFlag(WildcardSelection.StartsWith)) ||
                (selection.HasFlag(WildcardSelection.EndsWith) && !Supported.HasFlag(WildcardSelection.EndsWith)) ||
                (selection.HasFlag(WildcardSelection.Contains) && !Supported.HasFlag(WildcardSelection.Contains)) ||
                (selection.HasFlag(WildcardSelection.Embedded) && !Supported.HasFlag(WildcardSelection.Embedded)) ||
                (selection.HasFlag(WildcardSelection.MultiWildcard) && !Supported.HasFlag(WildcardSelection.MultiWildcard)) ||
                (selection.HasFlag(WildcardSelection.SingleWildcard) && !Supported.HasFlag(WildcardSelection.SingleWildcard)) ||
                (selection.HasFlag(WildcardSelection.AdjacentWildcards) && !Supported.HasFlag(WildcardSelection.AdjacentWildcards)) ||
                (selection.HasFlag(WildcardSelection.InvalidCharacter)))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Parses the wildcard text to ensure validitity returning a <see cref="WildcardResult"/>.
        /// </summary>
        /// <param name="text">The wildcard text.</param>
        /// <returns>The corresponding <see cref="WildcardResult"/>.</returns>
        public WildcardResult Parse(string? text)
        {
            text = Cleaner.Clean(text, StringTrim.Both, Transform);
            if (string.IsNullOrEmpty(text))
                return new WildcardResult(this) { Selection = WildcardSelection.None, Text = text };

            var sb = new StringBuilder();
            var wr = new WildcardResult(this) { Selection = WildcardSelection.Undetermined };

            if (CharactersNotAllowed != null && CharactersNotAllowed.Count > 0 && text.IndexOfAny([.. CharactersNotAllowed]) >= 0)
                wr.Selection |= WildcardSelection.InvalidCharacter;

            var hasMulti = SpaceTreatment == WildcardSpaceTreatment.MultiWildcardWhenOthers && Supported.HasFlag(WildcardSelection.MultiWildcard) && text.Contains(MultiWildcardCharacter, StringComparison.InvariantCulture);
            var hasTxt = false;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var isMulti = c == MultiWildcard;
                var isSingle = c == SingleWildcard;

                if (isMulti)
                {
                    wr.Selection |= WildcardSelection.MultiWildcard;

                    // Skip adjacent multi's as they are redundant.
                    for (int j = i + 1; j < text.Length; j++)
                    {
                        if (text[j] == MultiWildcard)
                        {
                            i = j;
                            continue;
                        }

                        break;
                    }
                }

                if (isSingle)
                    wr.Selection |= WildcardSelection.SingleWildcard;

                if (isMulti || isSingle)
                {
                    if (text.Length == 1)
                        wr.Selection |= WildcardSelection.Single;
                    else if (i == 0)
                        wr.Selection |= WildcardSelection.EndsWith;
                    else if (i == text.Length - 1)
                        wr.Selection |= WildcardSelection.StartsWith;
                    else
                    {
                        if (hasTxt || isSingle)
                            wr.Selection |= WildcardSelection.Embedded;
                        else
                            wr.Selection |= WildcardSelection.EndsWith;
                    }

                    if (i < text.Length - 1 && (text[i + 1] == MultiWildcard || text[i + 1] == SingleWildcard))
                        wr.Selection |= WildcardSelection.AdjacentWildcards;
                }
                else
                {
                    hasTxt = true;
                    if (c == SpaceCharacter && (SpaceTreatment == WildcardSpaceTreatment.Compress || SpaceTreatment == WildcardSpaceTreatment.MultiWildcardAlways || SpaceTreatment == WildcardSpaceTreatment.MultiWildcardWhenOthers))
                    {
                        // Compress adjacent spaces.
                        bool skipChar = SpaceTreatment != WildcardSpaceTreatment.Compress && text[i - 1] == MultiWildcardCharacter;
                        for (int j = i + 1; j < text.Length; j++)
                        {
                            if (text[j] == SpaceCharacter)
                            {
                                i = j;
                                continue;
                            }

                            break;
                        }

                        if (skipChar || (SpaceTreatment != WildcardSpaceTreatment.Compress && text[i + 1] == MultiWildcardCharacter))
                            continue;

                        if (SpaceTreatment == WildcardSpaceTreatment.MultiWildcardAlways || (SpaceTreatment == WildcardSpaceTreatment.MultiWildcardWhenOthers && hasMulti))
                        {
                            c = MultiWildcardCharacter;
                            wr.Selection |= WildcardSelection.MultiWildcard;
                            wr.Selection |= WildcardSelection.Embedded;
                        }
                    }
                }

                sb.Append(c);
            }

            if (!hasTxt && wr.Selection == (WildcardSelection.StartsWith | WildcardSelection.MultiWildcard))
            {
                wr.Selection |= WildcardSelection.Single;
                wr.Selection ^= WildcardSelection.StartsWith;
            }

            if (hasTxt && wr.Selection.HasFlag(WildcardSelection.StartsWith) && wr.Selection.HasFlag(WildcardSelection.EndsWith) && !wr.Selection.HasFlag(WildcardSelection.Embedded))
            {
                wr.Selection |= WildcardSelection.Contains;
                wr.Selection ^= WildcardSelection.StartsWith;
                wr.Selection ^= WildcardSelection.EndsWith;
            }

            if (wr.Selection == WildcardSelection.Undetermined)
                wr.Selection |= WildcardSelection.Equal;

            wr.Text = sb.Length == 0 ? null : sb.ToString();
            return wr;
        }
    }
}