using System;
using UnityEngine;
using UnityEngine.Events;

namespace MissNibiru.Narrative
{
    [Serializable]
    public sealed class NarrativeEventResponse
    {
        [SerializeField]
        private NarrativeEvent gameplayEvent;

        [SerializeField]
        private UnityEvent<string> response = new UnityEvent<string>();

        public NarrativeEvent GameplayEvent => gameplayEvent;
        public UnityEvent<string> Response => response;
    }

    [AddComponentMenu("Miss Nibiru/Narrative/Event Listener")]
    public sealed class NarrativeEventListener : MonoBehaviour
    {
        [SerializeField]
        private NarrativeEventResponse[] responses =
            Array.Empty<NarrativeEventResponse>();

        private void OnEnable()
        {
            foreach (NarrativeEventResponse binding in
                     responses ?? Array.Empty<NarrativeEventResponse>())
            {
                if (binding?.GameplayEvent != null)
                    binding.GameplayEvent.Raised += binding.Response.Invoke;
            }
        }

        private void OnDisable()
        {
            foreach (NarrativeEventResponse binding in
                     responses ?? Array.Empty<NarrativeEventResponse>())
            {
                if (binding?.GameplayEvent != null)
                    binding.GameplayEvent.Raised -= binding.Response.Invoke;
            }
        }
    }
}
