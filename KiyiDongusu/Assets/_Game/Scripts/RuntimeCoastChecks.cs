#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text=TMPro.TextMeshProUGUI;
namespace KiyiDongusu
{
    public sealed partial class RuntimeRecoveryChecks
    {
        IEnumerator Features()
        {
            Button("RetrySave").onClick.Invoke();yield return null;Debug.developerConsoleVisible=false;
            Test("English default and four independent volume UI controls",()=>{
                Assert(host.Game.State.language=="en"&&Button("Language").GetComponentInChildren<Text>().text=="Language: English","English default missing");
                string[] names={"MasterVolume","MusicVolume","EffectsVolume","AmbienceVolume"};
                for(int i=0;i<4;i++)GameObject.Find(names[i]).GetComponent<Slider>().value=.2f+i*.1f;
                var s=host.Game.State;Assert(Math.Abs(s.masterVolume-.2)<.0001&&Math.Abs(s.musicVolume-.3)<.0001&&Math.Abs(s.effectsVolume-.4)<.0001&&Math.Abs(s.ambienceVolume-.5)<.0001,"Channels not independent");
                Assert(host.Audio.MusicSource.isPlaying&&Math.Abs(host.Audio.MusicSource.volume-.06)<.001,"Music not playing at selected level");
                Assert(Math.Abs(host.Audio.WaveSource.volume-.06)<.001,"Wave volume not applied");
            });
            Test("Mute controls music, ambience and click sources",()=>{
                Button("Mute").onClick.Invoke();Assert(host.Audio.MusicSource.volume==0&&host.Audio.WaveSource.volume==0,"Soundscape not muted");
                foreach(var source in host.GetComponents<AudioSource>())Assert(source.mute||source.volume==0,"Click source not muted");
            });yield return null;
            Test("Native sliders fit their tracks and expose focus feedback",()=>{
                var slider=GameObject.Find("MasterVolume").GetComponent<Slider>();Canvas.ForceUpdateCanvases();
                Assert(slider.fillRect.rect.height<=9&&slider.handleRect.rect.height<=42,"Slider geometry overflow");
                Assert(slider.GetComponent<CoastPressable>()!=null,"Missing slider focus feedback");
            });
            Button("Mute").onClick.Invoke();yield return null;
            Test("Language UI switches to Turkish",()=>{Button("Language").onClick.Invoke();Assert(host.Game.State.language=="tr"&&GameText.Language=="tr","Language not applied");});yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings-tr.png"));
            Button("Language").onClick.Invoke();yield return null;
            host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.5f);
            Test("Resolution UI applies 1600 by 900",()=>Button("Resolution").onClick.Invoke());yield return new WaitForSecondsRealtime(.7f);
            Test("Actual Windows render target matches resolution setting",()=>Assert(Screen.width==1600&&Screen.height==900,"Resolution not applied: "+Screen.width+"x"+Screen.height));
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings-en-900.png"));
            host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.5f);
            Test("Windowed resolution restored to 720p",()=>Assert(Screen.width==1280&&Screen.height==720,"720p not applied"));
            Button("Fullscreen").onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
            Test("Fullscreen UI switches real Windows display mode",()=>Assert(Screen.fullScreenMode==FullScreenMode.FullScreenWindow&&host.Game.State.fullscreen,"Fullscreen not applied"));
            Button("Fullscreen").onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
            Test("Fullscreen UI returns to windowed mode",()=>Assert(Screen.fullScreenMode==FullScreenMode.Windowed&&!host.Game.State.fullscreen,"Windowed not restored"));
            Button("Resume").onClick.Invoke();yield return null;host.Paused=true;
            var token=GameObject.Find("WasteToken_0").GetComponent<BeachLitterDrag>();
            Vector2 net=RectTransformUtility.WorldToScreenPoint(null,host.View.NetRect.TransformPoint(host.View.NetRect.rect.center));
            var e=new PointerEventData(EventSystem.current){position=Vector2.zero};
            Test("Drop outside net restores litter without changing finite source",()=>{
                int collected=host.Game.Collected;host.Paused=false;token.OnBeginDrag(e);token.OnEndDrag(e);host.Paused=true;
                Assert(host.Game.Collected==collected&&!token.Dragging,"Outside drop changed inventory");
            });
            Test("Actual drag callbacks transfer one unit and defer extra credit",()=>{
                long money=host.Game.State.money;int collected=host.Game.Collected;e.position=net;host.Paused=false;token.OnBeginDrag(e);token.OnDrag(e);token.OnEndDrag(e);host.Paused=true;
                Assert(host.Game.Collected==collected+1&&host.Game.State.materials[0].netRaw==1&&host.Game.State.money==money,"Drop failed or paid early");
                host.Game.Tick(1);Assert(host.Game.State.materials[0].netProcessed==1&&host.Game.State.money==money+150,"Processing did not pay base plus bonus once");host.Game.Validate();
            });
            yield return new WaitForSecondsRealtime(.2f);
            Test("Stale drag rejected when collector takes held token",()=>{
                int count=host.Game.NetDelivered;host.Paused=false;token.OnBeginDrag(e);host.Game.Supply.CollectToken(0,host.Game.State.materials[0].tokenGeneration);host.Game.Supply.RefreshTokens();token.OnEndDrag(e);host.Paused=true;
                Assert(host.Game.NetDelivered==count,"Stale drag minted bonus");host.Game.Validate();
            });
            Test("Net and all preferences round trip in actual Player save",()=>{
                Assert(host.TrySaveNow(),"Save failed");var loaded=host.Saves.Load();
                string canonical=JsonUtility.ToJson(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(host.Game.State)));
                Assert(JsonUtility.ToJson(loaded)==canonical,"Preferences or ledger changed");
            });
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"gameplay-720.png"));
            yield return new WaitForEndOfFrame();yield return null;
            host.View.TogglePause();yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings-en-720.png"));
            EventSystem.current.SetSelectedGameObject(Button("Language").gameObject);yield return new WaitForSecondsRealtime(.1f);
            Test("Keyboard focus on pale button uses dark high contrast ink",()=>{
                var outline=Button("Language").GetComponent<Outline>();Assert(outline.enabled&&outline.effectColor==CoastTheme.Ink,"Pale focus contrast missing");
            });
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings-focus-720.png"));
            yield return new WaitForSecondsRealtime(3);
            Test("Periodic ocean source actually plays imported wave clip",()=>Assert(host.Audio.WaveSource.isPlaying&&host.Audio.WaveSource.clip.length>8,"Periodic wave not playing"));
        }
    }
}
#endif
