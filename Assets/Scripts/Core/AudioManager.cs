using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Plays named sound effects from a small pool of audio sources, plus one looping ambience.
    /// Clip names match the generated files in Assets/Audio/Generated.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public struct NamedClip
        {
            public string name;
            public AudioClip clip;
        }

        public static AudioManager Instance { get; private set; }

        [SerializeField] NamedClip[] clips;
        [SerializeField] int voices = 12;
        [SerializeField] float ambienceVolume = 0.25f;

        readonly Dictionary<string, AudioClip> lookup = new Dictionary<string, AudioClip>();
        AudioSource[] pool;
        AudioSource ambience;
        int next;

        void Awake()
        {
            Instance = this;
            foreach (var c in clips) if (c.clip != null) lookup[c.name] = c.clip;

            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.volume = ambienceVolume;
            if (lookup.TryGetValue("ambience_loop", out var amb))
            {
                ambience.clip = amb;
                ambience.Play();
            }
        }

        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (Instance == null || !Instance.lookup.TryGetValue(name, out var clip)) return;
            var src = Instance.pool[Instance.next];
            Instance.next = (Instance.next + 1) % Instance.pool.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>Plays with a small random pitch change so repeats don't sound identical.</summary>
        public static void PlayVaried(string name, float volume = 1f, float spread = 0.08f) =>
            Play(name, volume, 1f + UnityEngine.Random.Range(-spread, spread));

        public static void SetMasterVolume(float volume) => AudioListener.volume = Mathf.Clamp01(volume);
    }
}
