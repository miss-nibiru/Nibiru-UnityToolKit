using System;
using UnityEngine;

namespace MissNibiru.Waves.Tracking
{
    [DisallowMultipleComponent]
    public sealed class WaveSpawnedObject : MonoBehaviour
    {
        public event Action<WaveSpawnedObject> Released;

        public bool IsReleased { get; private set; }

        public void Release()
        {
            if (IsReleased)
                return;

            IsReleased = true;
            Released?.Invoke(this);
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}