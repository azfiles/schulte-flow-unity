using UnityEngine;

namespace SchulteFlow
{
    public sealed class Feedback : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip[] notes;
        private AudioClip miss, finish;
        private Profile profile;
        public void Initialize(Profile data)
        {
            profile=data;source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            notes=new AudioClip[6];int[] semitones={0,2,4,7,9,12};
            for(int i=0;i<notes.Length;i++)notes[i]=Tone("Step "+i,440*Mathf.Pow(2,semitones[i]/12f),.17f,false);
            miss=Tone("Soft miss",164.8f,.13f,false);finish=Tone("Complete chord",523.25f,.65f,true);
        }
        public void Tap(bool wrong,int combo)
        {
            Play(wrong?miss:notes[Mathf.Clamp(combo-1,0,5)],wrong?.22f:.32f);Haptic(wrong);
        }
        public void Count(){Play(notes[0],.15f);}
        public void Complete(){Play(finish,.35f);}
        private void Play(AudioClip clip,float volume){if(profile.sound && !AudioListener.pause)source.PlayOneShot(clip,volume);}
        private void Haptic(bool wrong)
        {
            if(!profile.haptics)return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                    activity.Call("runOnUiThread",new AndroidJavaRunnable(()=>
                    {
                        using(var p=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using(var a=p.GetStatic<AndroidJavaObject>("currentActivity"))
                        using(var window=a.Call<AndroidJavaObject>("getWindow"))
                        using(var view=window.Call<AndroidJavaObject>("getDecorView"))
                            view.Call<bool>("performHapticFeedback",wrong?0:3);
                    }));
            }
            catch(System.Exception){Debug.LogWarning("System haptic feedback is unavailable on this device.");}
#endif
        }
        private static AudioClip Tone(string name,float frequency,float duration,bool chord)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*duration)];
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,envelope=Mathf.Min(1,t/.009f)*Mathf.Pow(1-t/duration,2.5f);
                float wave=Mathf.Sin(2*Mathf.PI*frequency*t)+.17f*Mathf.Sin(4*Mathf.PI*frequency*t);
                if(chord)wave=(wave+Mathf.Sin(2*Mathf.PI*frequency*1.25f*t)+Mathf.Sin(2*Mathf.PI*frequency*1.5f*t))*.4f;
                samples[i]=wave*envelope*.6f;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDestroy(){if(notes!=null)foreach(var note in notes)Destroy(note);if(miss!=null)Destroy(miss);if(finish!=null)Destroy(finish);}
    }
}
