using System;
using UnityEngine;

namespace DragonBattle.Audio
{
    [Serializable]
    public class SoundCue
    {
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 3f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        private int lastIndex = -1;

        public bool HasClips => clips != null && clips.Length > 0;

        public void Play(Vector3 position, float pitchScale = 1f, float volumeScale = 1f)
        {
            AudioClip clip = PickClip();
            if (clip != null)
            {
                SoundPlayer.PlayOneShot(clip, position, volume * volumeScale, RandomPitch() * pitchScale);
            }
        }

        public void Play2D(float pitchScale = 1f)
        {
            AudioClip clip = PickClip();
            if (clip != null)
            {
                SoundPlayer.PlayOneShot2D(clip, volume, RandomPitch() * pitchScale);
            }
        }

        private float RandomPitch()
        {
            return UnityEngine.Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
        }

        private AudioClip PickClip()
        {
            if (!HasClips)
            {
                return null;
            }
            if (clips.Length == 1)
            {
                return clips[0];
            }

            int index = UnityEngine.Random.Range(0, clips.Length - 1);
            if (index >= lastIndex && lastIndex >= 0)
            {
                index++;
            }
            lastIndex = index;
            return clips[index];
        }
    }
}
