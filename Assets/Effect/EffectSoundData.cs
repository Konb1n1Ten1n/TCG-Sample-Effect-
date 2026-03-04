using UnityEngine;

namespace DuelKingdom.Effect
{
    [CreateAssetMenu(fileName = "EffectSoundData", menuName = "DuelKingdom/Effect Sound Data")]
    public class EffectSoundData : ScriptableObject
    {
        [System.Serializable]
        public class SoundEntry
        {
            public EffectSEType seType;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public SoundEntry[] sounds;
    }
}