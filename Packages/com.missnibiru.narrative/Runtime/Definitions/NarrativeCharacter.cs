using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeCharacter",
        menuName = "Miss Nibiru/Narrative/Character")]
    public sealed class NarrativeCharacter : ScriptableObject
    {
        [SerializeField]
        private string id = "character";

        [SerializeField]
        private string displayName = "Character";

        [SerializeField]
        private Color nameColour = Color.white;

        [SerializeField]
        private Sprite defaultPortrait;

        [SerializeField]
        private NarrativeEmotionPortrait[] emotionPortraits =
            Array.Empty<NarrativeEmotionPortrait>();

        [SerializeField]
        private NarrativeAudioProfile voiceProfile;

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public Color NameColour => nameColour;
        public Sprite DefaultPortrait => defaultPortrait;
        public IReadOnlyList<NarrativeEmotionPortrait> EmotionPortraits =>
            emotionPortraits ?? Array.Empty<NarrativeEmotionPortrait>();
        public NarrativeAudioProfile VoiceProfile => voiceProfile;

        public Sprite GetPortrait(NarrativeEmotion emotion)
        {
            if (emotion != null && emotionPortraits != null)
            {
                foreach (NarrativeEmotionPortrait mapping
                         in emotionPortraits)
                {
                    if (mapping != null &&
                        mapping.Emotion == emotion &&
                        mapping.Portrait != null)
                    {
                        return mapping.Portrait;
                    }
                }
            }

            return defaultPortrait;
        }

        public bool SupportsEmotion(NarrativeEmotion emotion)
        {
            if (emotion == null)
                return true;

            if (emotionPortraits == null)
                return false;

            foreach (NarrativeEmotionPortrait mapping
                     in emotionPortraits)
            {
                if (mapping != null && mapping.Emotion == emotion)
                    return true;
            }

            return false;
        }

        public void Configure(string stableId, string visibleName)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
        }

        public void Configure(
            string stableId,
            string visibleName,
            Color displayColour)
        {
            Configure(stableId, visibleName);
            nameColour = displayColour;
        }

        private void OnValidate()
        {
            id = CleanId(id);
            emotionPortraits ??= Array.Empty<NarrativeEmotionPortrait>();
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
