using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioLinkAdapter : UdonSharpBehaviour
    {
#if !AUDIOLINK
        [Header("AudioLink is not present in the project. AudioLink is required for this plugin.")] 
        public string getAudioLinkHere = "https://github.com/llealloo/vrc-udon-audio-link/releases/latest";
        private bool debug = true;
        private string debugLabel;

        void Start() {
            debugLabel = $"<Missing AudioLink>/{name}";
            err("AudioLink is not present in the project. AudioLink is required for this plugin.");
            log("Get it here: https://github.com/llealloo/vrc-udon-audio-link/releases/latest");
        }
#else
        public TVManagerV2 tv;
        public VRCAudioLink.AudioLink audioLinkInstance;
        public string speakerName = "AudioLink";

        [Tooltip("Optionally specify world music to pause while the TV is playing. Will resume world music a given number of seconds after TV stops playing.")]
        public AudioSource worldMusic;

        [Tooltip("How long to wait after the TV has finished before resuming the world music.")]
        public float worldMusicResumeDelay = 20f;

        [Tooltip("How long does the world music take to fade in after the delay has completed.")]
        public float worldMusicFadeInTime = 4f;

        [Tooltip("While the TV is muted or paused, allow the world music to continue playing?")]
        public bool worldMusicResumeDuringSilence = false;

        private float worldMusicVolume;
        private float worldMusicFadeAmount;
        private AudioSource activeSpeaker;
        private AudioSource nextSpeaker;
        private int OUT_VIDEOPLAYER;
        private float OUT_VOLUME;
        private bool mute;
        private bool worldMusicActive = true;
        private bool hasWorldMusic = false;
        private bool init = false;
        private bool debug = true;
        private string debugLabel;

        private void initialize()
        {
            if (init) return;
            hasWorldMusic = worldMusic != null;
            if (hasWorldMusic)
            {
                audioLinkInstance.audioSource = worldMusic;
                worldMusicVolume = worldMusic.volume;
            }

            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (tv == null)
            {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the audio link adapter knows what TV to connect to.");
                return;
            }

            debugLabel = $"{tv.gameObject.name}/{name}";
            tv._RegisterUdonSharpEventReceiver(this);
            init = true;
        }

        private void Start()
        {
            initialize();
        }

        private void Update() => _InternalUpdate();

        public void _InternalUpdate()
        {
            if (hasWorldMusic)
            {
                if (worldMusic.isPlaying && worldMusic.volume < worldMusicVolume)
                {
                    if (worldMusicFadeInTime == 0f)
                    {
                        worldMusic.volume = worldMusicVolume;
                    }
                    else
                    {
                        worldMusicFadeAmount += Time.deltaTime;
                        worldMusic.volume = Mathf.SmoothStep(0f, worldMusicVolume, worldMusicFadeAmount / worldMusicFadeInTime);
                    }
                }
            }
        }

        public void _TvMediaStart()
        {
            resumeTVAudio();
            if (nextSpeaker != activeSpeaker)
            {
                activeSpeaker = nextSpeaker;
                audioLinkInstance.audioSource = nextSpeaker;
            }
        }

        public void _TvMediaEnd() => resumeWorldMusic();
        public void _TvStop() => resumeWorldMusic();

        public void _TvPause()
        {
            if (worldMusicResumeDuringSilence)
            {
                if (activeSpeaker != null) activeSpeaker.mute = mute = true;
                resumeWorldMusic();
            }
        }

        public void _TvPlay()
        {
            if (activeSpeaker != null) activeSpeaker.mute = mute = false;
            resumeTVAudio();
        }

        public void _TvMute()
        {
            if (activeSpeaker != null) activeSpeaker.mute = mute = true;
            if (worldMusicResumeDuringSilence) resumeWorldMusic();
        }

        public void _TvUnMute()
        {
            if (activeSpeaker != null) activeSpeaker.mute = mute = false;
            resumeTVAudio();
        }

        public void _TvVolumeChange()
        {
            if (activeSpeaker != null) activeSpeaker.mute = mute || OUT_VOLUME == 0;
        }

        private void resumeWorldMusic()
        {
            if (hasWorldMusic && !worldMusicActive)
            {
                worldMusicActive = true;
                SendCustomEventDelayedSeconds(nameof(_ActivateWorldMusic), worldMusicResumeDelay);
            }
        }

        private void resumeTVAudio()
        {
            if (hasWorldMusic && worldMusicActive && activeSpeaker != null)
            {
                worldMusic.volume = 0f;
                worldMusicFadeAmount = 0f;
                worldMusic.Pause();
                worldMusicActive = false;
                audioLinkInstance.audioSource = activeSpeaker;
            }
        }

        public void _ActivateWorldMusic()
        {
            if (hasWorldMusic && worldMusicActive)
            {
                log("Resuming world music...");
                audioLinkInstance.audioSource = worldMusic;
                worldMusic.UnPause();
            }
        }

        public void _TvVideoPlayerChange()
        {
            var manager = tv.videoManagers[OUT_VIDEOPLAYER];
            var managerName = manager.gameObject.name;
            log($"Updating AudioLink for {managerName}");
            var speakers = manager.speakers;
            AudioSource fallback = null;
            nextSpeaker = null;
            foreach (var speaker in speakers)
            {
                if (speaker == null) continue;
                if (speaker.gameObject.name == speakerName)
                {
                    nextSpeaker = speaker;
                    break;
                }

                if (fallback == null) fallback = speaker;
            }

            if (nextSpeaker == null)
            {
                log($"No speaker named {speakerName} found, using first available speaker.");
                nextSpeaker = fallback;
            }
            if (nextSpeaker == null) warn($"No available audio source was found connected to the {managerName} video manager.");
            else log($"Valid source found {nextSpeaker.gameObject.name}");
        }

#endif

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }

        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }

        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }
    }
}