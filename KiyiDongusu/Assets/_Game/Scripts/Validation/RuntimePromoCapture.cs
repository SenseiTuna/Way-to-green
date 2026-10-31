#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KiyiDongusu
{
    // Explicit isolated production capture. Never compiled into the delivered game.
    public sealed class RuntimePromoCapture : MonoBehaviour
    {
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--kiyi-promo")>=0;
        public static string Argument(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);if(i<0||i+1>=a.Length)throw new ArgumentException(key);return Path.GetFullPath(a[i+1]);}
        GameBootstrap host;System.Diagnostics.Process encoder;Texture2D frame;Stream pipe;Image hand;GameArt art;Vector2 pointer;bool held;int shots;string output;double nextAction;float clipTime;BeachLitterDrag drag;Vector2 dragStart,dragEnd;
        readonly System.Collections.Generic.List<string> events=new System.Collections.Generic.List<string>();
        public void Initialize(GameBootstrap game){host=game;StartCoroutine(Capture());}
        void Point(RectTransform r){pointer=r.TransformPoint(r.rect.center);}
        void Click(string name){var go=GameObject.Find(name);if(go==null)return;Point((RectTransform)go.transform);var b=go.GetComponent<Button>();if(b==null||!b.interactable)return;var e=new PointerEventData(EventSystem.current){position=pointer};var p=go.GetComponent<CoastPressable>();p?.OnPointerDown(e);b.onClick.Invoke();p?.OnPointerUp(e);events.Add(clipTime.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",click");}
        IEnumerator Capture()
        {
            output=Argument("--kiyi-test-output");Directory.CreateDirectory(output);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);
            Time.captureFramerate=30;Application.targetFrameRate=30;
            host.Game.State.musicVolume=0;host.Audio.Apply();host.Audio.WaveSource.Play();
            var cursor=UnityEngine.Object.FindAnyObjectByType<CoastHandCursor>();if(cursor!=null){cursor.enabled=false;cursor.gameObject.SetActive(false);}Cursor.visible=false;
            var canvas=new GameObject("CaptureCursor",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=1001;
            var h=new GameObject("CaptureHand",typeof(RectTransform),typeof(Image));h.transform.SetParent(canvas.transform,false);hand=h.GetComponent<Image>();hand.raycastTarget=false;hand.preserveAspect=true;hand.rectTransform.sizeDelta=new Vector2(76,76);hand.rectTransform.pivot=new Vector2(.65f,.84f);art=new GameArt();pointer=new Vector2(460,650);
            encoder=new System.Diagnostics.Process();encoder.StartInfo=new System.Diagnostics.ProcessStartInfo{FileName=Argument("--kiyi-ffmpeg"),Arguments="-y -f rawvideo -pixel_format rgb24 -video_size 1920x1080 -framerate 30 -i pipe:0 -vf vflip -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \""+Path.Combine(output,"gameplay-raw.mp4")+"\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};encoder.Start();encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;
            for(int f=0;f<2400;f++)
            {
                clipTime=f/30f;held=false;
                if(f==90||f==150||f==210)Click("DialogueNext");
                if(f>=240&&f<=480&&(f-240)%60==0)Click("OpeningPaper_"+((f-240)/60));
                if(f==500){var go=GameObject.Find("OpeningPaper_5");drag=go.GetComponent<BeachLitterDrag>();Point((RectTransform)go.transform);dragStart=pointer;dragEnd=host.View.NetRect.TransformPoint(host.View.NetRect.rect.center);drag.OnBeginDrag(new PointerEventData(EventSystem.current){position=pointer});}
                if(f>=500&&f<=540){held=true;pointer=Vector2.Lerp(dragStart,dragEnd,Mathf.SmoothStep(0,1,(f-500)/40f));drag.OnDrag(new PointerEventData(EventSystem.current){position=pointer});if(f==540){drag.OnEndDrag(new PointerEventData(EventSystem.current){position=pointer});events.Add("18.000,drop");}}
                if(f==550)host.Story.Skip();
                if(f>=570&&f<780&&f%12==0)Click("CenterAction");
                if(f==780){host.Story.SetEnabled(false);events.Add("26.000,speedup");}
                if(f>=780&&f<1980&&!(f>=1400&&f<=1470)&&!host.Game.State.complete)
                {
                    // Real economy actions, accelerated between the manual highlights.
                    for(int step=0;step<24;step++)
                    {
                        if(host.Game.State.elapsed>=nextAction){host.Game.CenterAction();nextAction=host.Game.State.elapsed+.333333;}
                        host.Game.Tick(.05);
                        if(host.Game.State.collectorRank<3)host.Game.Purchase(PurchaseKind.Collector);
                        for(int k=0;k<4;k++){if(host.Game.State.facilities[k].speed<2)host.Game.Purchase(PurchaseKind.Speed,k);if(host.Game.State.facilities[k].capacity<2)host.Game.Purchase(PurchaseKind.Capacity,k);}
                        if(host.Game.State.collectorRank>=3)for(int k=0;k<6;k++)host.Game.Purchase(PurchaseKind.SortingBin,k);
                    }
                    host.Game.Validate();
                    if(f%15==0)Click("CenterAction");
                }
                else if(f<780&&!host.Story.IntroBlocking)host.Game.Tick(1d/30);
                if(f==1400){drag=null;for(int attempt=0;attempt<600&&drag==null;attempt++){for(int k=0;k<6;k++)host.Game.Purchase(PurchaseKind.SortingBin,k);host.View.SendMessage("Refresh");foreach(var item in UnityEngine.Object.FindObjectsByType<BeachLitterDrag>(FindObjectsSortMode.None))if(item.gameObject.activeInHierarchy&&host.Game.OwnsBin(item.Material)){drag=item;break;}if(drag==null)host.Game.Tick(.1);}if(drag!=null){Point((RectTransform)drag.transform);dragStart=pointer;dragEnd=host.View.BinRect(drag.Material).TransformPoint(host.View.BinRect(drag.Material).rect.center);drag.OnBeginDrag(new PointerEventData(EventSystem.current){position=pointer});}}
                if(f>=1400&&f<=1470&&drag!=null){held=true;pointer=Vector2.Lerp(dragStart,dragEnd,Mathf.SmoothStep(0,1,(f-1400)/70f));drag.OnDrag(new PointerEventData(EventSystem.current){position=pointer});if(f==1470){drag.OnEndDrag(new PointerEventData(EventSystem.current){position=pointer});events.Add("49.000,sorted_drop");}}
                if(f==1980&&!host.Game.State.complete){for(int i=0;i<100000&&!host.Game.State.complete;i++){host.Game.CenterAction();host.Game.Tick(.05);}host.Game.Validate();events.Add("66.000,cut_to_completion");}
                if(f==2160)Click("ReviewShoreTasks");
                if(f==2280)Click("QuestClose");
                host.Story.Step(1f/30);host.View.SendMessage("Refresh");
                hand.sprite=held?art.HandPinch:art.HandOpen;hand.rectTransform.position=pointer;hand.enabled=f<1980;
                yield return new WaitForEndOfFrame();
                frame=ScreenCapture.CaptureScreenshotAsTexture();
                var pixels=frame.GetPixels32();var bytes=new byte[pixels.Length*3];for(int k=0;k<pixels.Length;k++){bytes[k*3]=pixels[k].r;bytes[k*3+1]=pixels[k].g;bytes[k*3+2]=pixels[k].b;}pipe.Write(bytes,0,bytes.Length);
                if(f==300||f==750||f==1350||f==1950||f==2070){File.WriteAllBytes(Path.Combine(output,"scene-"+f+".png"),frame.EncodeToPNG());shots++;}
                Destroy(frame);
            }
            pipe.Close();encoder.WaitForExit();host.Game.Validate();host.Saves.Save(host.Game);
            File.WriteAllLines(Path.Combine(output,"events.csv"),events);
            File.WriteAllText(Path.Combine(output,"capture-report.json"),"{\"frames\":2400,\"seconds\":80,\"fps\":30,\"width\":1920,\"height\":1080,\"encoderExit\":"+encoder.ExitCode+",\"processed\":"+host.Game.Processed+",\"complete\":"+host.Game.State.complete.ToString().ToLowerInvariant()+",\"isolated\":true,\"acceleratedSection\":true}");
            Application.Quit(encoder.ExitCode);
        }
    }
}
#endif
