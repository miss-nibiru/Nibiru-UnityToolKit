using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public enum TweeAudioRole
    {
        Voice,
        Music,
        SoundEffect
    }

    [Serializable]
    public sealed class TweeSpeakerMapping
    {
        [SerializeField, Tooltip("Imported speaker name.")]
        private string displayName = "Speaker";

        [SerializeField, Tooltip("Colours for this speaker.")]
        private string[] colours = Array.Empty<string>();

        [SerializeField, Tooltip("Optional existing character.")]
        private NarrativeCharacter character;

        [SerializeField, Tooltip("Optional default emotion.")]
        private NarrativeEmotion emotion;

        [SerializeField, Tooltip("Portrait placement.")]
        private NarrativePortraitSide portraitSide =
            NarrativePortraitSide.Left;

        public string DisplayName => displayName ?? string.Empty;
        public IReadOnlyList<string> Colours =>
            colours ?? Array.Empty<string>();
        public NarrativeCharacter Character => character;
        public NarrativeEmotion Emotion => emotion;
        public NarrativePortraitSide PortraitSide => portraitSide;

        public bool Matches(string colour)
        {
            string normalized = TweeImportProfile.NormalizeColour(colour);

            if (string.IsNullOrEmpty(normalized) || colours == null)
                return false;

            foreach (string candidate in colours)
            {
                if (TweeImportProfile.NormalizeColour(candidate) == normalized)
                    return true;
            }

            return false;
        }

        public void Configure(
            string visibleName,
            string[] mappedColours,
            NarrativeCharacter mappedCharacter = null,
            NarrativeEmotion mappedEmotion = null,
            NarrativePortraitSide side = NarrativePortraitSide.Left)
        {
            displayName = visibleName ?? string.Empty;
            colours = mappedColours ?? Array.Empty<string>();
            character = mappedCharacter;
            emotion = mappedEmotion;
            portraitSide = side;
            Normalize();
        }

        public void SetCharacter(NarrativeCharacter value)
        {
            character = value;
        }

        private void Normalize()
        {
            colours ??= Array.Empty<string>();

            for (int i = 0; i < colours.Length; i++)
                colours[i] = TweeImportProfile.NormalizeColour(colours[i]);
        }
    }

    [Serializable]
    public sealed class TweeAudioMapping
    {
        [SerializeField, Tooltip("Twee audio key.")]
        private string key = string.Empty;

        [SerializeField, Tooltip("Unity audio clip.")]
        private AudioClip clip;

        [SerializeField, Tooltip("How this clip plays.")]
        private TweeAudioRole role = TweeAudioRole.Voice;

        public string Key => key ?? string.Empty;
        public AudioClip Clip => clip;
        public TweeAudioRole Role => role;

        public void Configure(
            string audioKey,
            AudioClip mappedClip,
            TweeAudioRole audioRole)
        {
            key = (audioKey ?? string.Empty).Trim();
            clip = mappedClip;
            role = audioRole;
        }
    }

    [CreateAssetMenu(
        fileName = "TweeImportProfile",
        menuName = "Miss Nibiru/Narrative/Twee Import Profile")]
    public sealed class TweeImportProfile : ScriptableObject
    {
        [SerializeField]
        private string id = "twee_import";

        [SerializeField]
        private string displayName = "Twee Import Profile";

        [SerializeField, Tooltip("Create missing speakers.")]
        private bool createPlaceholderCharacters = true;

        [SerializeField, Tooltip("Import uncoloured narration.")]
        private bool includeNarration = true;

        [SerializeField, Min(1), Tooltip("Words allowed per line.")]
        private int defaultWordLimit = 80;

        [SerializeField]
        private TweeSpeakerMapping[] speakers =
            Array.Empty<TweeSpeakerMapping>();

        [SerializeField]
        private TweeAudioMapping[] audio =
            Array.Empty<TweeAudioMapping>();

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public bool CreatePlaceholderCharacters =>
            createPlaceholderCharacters;
        public bool IncludeNarration => includeNarration;
        public int DefaultWordLimit => Mathf.Max(1, defaultWordLimit);
        public IReadOnlyList<TweeSpeakerMapping> Speakers =>
            speakers ?? Array.Empty<TweeSpeakerMapping>();
        public IReadOnlyList<TweeAudioMapping> Audio =>
            audio ?? Array.Empty<TweeAudioMapping>();

        public TweeSpeakerMapping FindSpeaker(string colour)
        {
            if (speakers == null)
                return null;

            foreach (TweeSpeakerMapping mapping in speakers)
            {
                if (mapping != null && mapping.Matches(colour))
                    return mapping;
            }

            return null;
        }

        public TweeAudioMapping FindAudio(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || audio == null)
                return null;

            foreach (TweeAudioMapping mapping in audio)
            {
                if (mapping != null && string.Equals(
                        mapping.Key,
                        key.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return mapping;
                }
            }

            return null;
        }

        public void Configure(
            string stableId,
            string visibleName,
            TweeSpeakerMapping[] speakerMappings = null,
            TweeAudioMapping[] audioMappings = null)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
            speakers = speakerMappings ?? Array.Empty<TweeSpeakerMapping>();
            audio = audioMappings ?? Array.Empty<TweeAudioMapping>();
        }

        public static string NormalizeColour(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();

            if (normalized.Length == 6)
                normalized = "#" + normalized;

            return normalized;
        }

        private void OnValidate()
        {
            id = CleanId(id);
            defaultWordLimit = Mathf.Max(1, defaultWordLimit);
            speakers ??= Array.Empty<TweeSpeakerMapping>();
            audio ??= Array.Empty<TweeAudioMapping>();
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "twee_import"
                : value.Trim().ToLowerInvariant()
                    .Replace(' ', '_')
                    .Replace('-', '_');
        }
    }
}
