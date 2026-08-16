using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeAudioProfile",
        menuName = "Miss Nibiru/Narrative/Audio Profile")]
    public sealed class NarrativeAudioProfile : ScriptableObject
    {
        [SerializeField]
        private AudioClip typingSound;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.35f;

        [SerializeField, Range(0.5f, 2f)]
        private float minimumPitch = 0.95f;

        [SerializeField, Range(0.5f, 2f)]
        private float maximumPitch = 1.05f;

        [SerializeField, Min(1)]
        private int charactersPerSound = 2;

        public AudioClip TypingSound => typingSound;
        public float Volume => volume;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public int CharactersPerSound =>
            Mathf.Max(1, charactersPerSound);

        private void OnValidate()
        {
            if (minimumPitch > maximumPitch)
            {
                float previousMinimum = minimumPitch;
                minimumPitch = maximumPitch;
                maximumPitch = previousMinimum;
            }

            charactersPerSound = Mathf.Max(1, charactersPerSound);
        }
    }
}
