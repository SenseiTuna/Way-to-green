using UnityEngine;
namespace KiyiDongusu
{
    public sealed class CoastAudio : MonoBehaviour
    {
        GameBootstrap host; AudioSource music,waves; float nextWave,dialogueGain=1;
        public void SetDialogueGain(float value){if(Mathf.Approximately(dialogueGain,value))return;dialogueGain=value;Apply();}
        public AudioSource MusicSource=>music;
        public AudioSource WaveSource=>waves;
        public void Initialize(GameBootstrap game)
        {
            host=game; music=gameObject.AddComponent<AudioSource>(); waves=gameObject.AddComponent<AudioSource>();
            music.playOnAwake=waves.playOnAwake=false; music.spatialBlend=waves.spatialBlend=0;
            music.clip=Resources.Load<AudioClip>("Audio/Tideglass"); music.loop=true;
            waves.clip=Resources.Load<AudioClip>("Audio/ShoreWave"); nextWave=Time.unscaledTime+7;
            Apply(); if(music.clip!=null) music.Play();
        }
        public void Apply()
        {
            var s=host.Game.State; bool silent=s.muted||host.ValidationRun;
            music.volume=silent?0:(float)(s.masterVolume*s.musicVolume)*dialogueGain;
            waves.volume=silent?0:(float)(s.masterVolume*s.ambienceVolume)*.6f;
        }
        void Update()
        {
            if(Time.unscaledTime<nextWave)return;
            nextWave=Time.unscaledTime+24;
            if(waves.clip!=null)waves.Play();
        }
    }
}
