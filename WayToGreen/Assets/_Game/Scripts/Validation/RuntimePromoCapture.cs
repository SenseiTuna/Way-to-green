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
        float lastClick=-10; int sortedBefore; bool sortedVerified;
        void Event(string kind){events.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F3},{1},{2:F1},{3:F1}",clipTime,kind,pointer.x,1080-pointer.y));}
        void BeginDrag(string name,bool bin){var go=GameObject.Find(name);if(go==null)throw new InvalidOperationException("Missing capture object "+name);drag=go.GetComponent<BeachLitterDrag>();Point((RectTransform)go.transform);dragStart=pointer;var target=bin?host.View.BinRect(drag.Material):host.View.NetRect;dragEnd=target.TransformPoint(target.rect.center);sortedBefore=host.Game.NetDelivered;drag.OnBeginDrag(new PointerEventData(EventSystem.current){position=pointer});if(!drag.Dragging)throw new InvalidOperationException("Capture drag did not begin");Event("grab");}
        void MoveDrag(int f,int from,int to,bool bin){held=true;pointer=Vector2.Lerp(dragStart,dragEnd,Mathf.SmoothStep(0,1,(f-from)/(float)(to-from)));drag.OnDrag(new PointerEventData(EventSystem.current){position=pointer});if(f==to){drag.OnEndDrag(new PointerEventData(EventSystem.current){position=pointer});Event(bin?"sorted_drop":"drop");if(bin){sortedVerified=host.Game.NetDelivered==sortedBefore+1;if(!sortedVerified)throw new InvalidOperationException("Container delivery did not credit sorting ledger");}}}
        void Advance(int steps,bool upgrade){for(int step=0;step<steps&&!host.Game.State.complete;step++){if(host.Game.State.elapsed>=nextAction){host.Game.CenterAction();nextAction=host.Game.State.elapsed+.333333;}host.Game.Tick(.05);if(upgrade){if(host.Game.State.collectorRank<3)host.Game.Purchase(PurchaseKind.Collector);for(int k=0;k<4;k++){if(host.Game.State.facilities[k].speed<2)host.Game.Purchase(PurchaseKind.Speed,k);if(host.Game.State.facilities[k].capacity<2)host.Game.Purchase(PurchaseKind.Capacity,k);}if(host.Game.State.collectorRank>=3)for(int k=0;k<6;k++)host.Game.Purchase(PurchaseKind.SortingBin,k);}}host.Game.Validate();}
        public void Initialize(GameBootstrap game){host=game;StartCoroutine(Capture());}
        void Point(RectTransform r){pointer=r.TransformPoint(r.rect.center);}
        void Click(string name){var go=GameObject.Find(name);if(go==null)return;Point((RectTransform)go.transform);var b=go.GetComponent<Button>();if(b==null||!b.interactable)return;var e=new PointerEventData(EventSystem.current){position=pointer};var p=go.GetComponent<CoastPressable>();p?.OnPointerDown(e);b.onClick.Invoke();p?.OnPointerUp(e);lastClick=clipTime;Event("click");}
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
                if(f==30||f==90||f==150)Click("DialogueNext");
                if(f==180)BeginDrag("OpeningPaper_0",false);
                if(f>=180&&f<=270)MoveDrag(f,180,270,false);
                if(f==315)BeginDrag("OpeningPaper_1",false);
                if(f>=315&&f<=390)MoveDrag(f,315,390,false);
                if(f>=420&&f<=510&&(f-420)%30==0)Click("OpeningPaper_"+(2+(f-420)/30));
                if(f==525){host.Story.Skip();host.Story.SetEnabled(false);}
                if(f>=555&&f<780&&f%24==0)Click("CenterAction");
                // The explanatory sections run at real game speed. Only the explicit
                // edit cuts and the later progress montage skip simulation time.
                if(f==1020){for(int i=0;i<20000&&host.Game.State.money<host.Game.Cost(PurchaseKind.SortingBin,0);i++)Advance(1,false);Event("cut_later");host.View.SendMessage("Refresh");}
                if(f==1050)Click("BuyBin_0");
                if(f==1110)BeginDrag("WasteToken_0",true);
                if(f>=1110&&f<=1200)MoveDrag(f,1110,1200,true);
                if(f==1260){for(int i=0;i<40000&&!host.Game.State.materials[3].unlocked;i++)Advance(1,false);Event("cut_later");host.View.SendMessage("Refresh");}
                if(f>=1275&&f<1440&&f%24==0)Click("CenterAction");
                if(f==1470){host.Game.Purchase(PurchaseKind.Collector);Event("collector_purchase");}
                if(f>=1560&&f<1980){Advance(60,true);if(f%30==0)Click("CenterAction");}
                else if(!host.Story.IntroBlocking)host.Game.Tick(1d/30);
                if(f==1980&&!host.Game.State.complete){Advance(100000,true);Event("cut_to_completion");}
                if(f==2160)Click("ReviewShoreTasks");
                if(f==2280)Click("QuestClose");
                host.Story.Step(1f/30);host.View.SendMessage("Refresh");
                hand.sprite=held||clipTime-lastClick<.16f?art.HandPinch:art.HandOpen;hand.rectTransform.position=pointer;hand.enabled=f<1980;
                yield return new WaitForEndOfFrame();
                frame=ScreenCapture.CaptureScreenshotAsTexture();
                var pixels=frame.GetPixels32();var bytes=new byte[pixels.Length*3];for(int k=0;k<pixels.Length;k++){bytes[k*3]=pixels[k].r;bytes[k*3+1]=pixels[k].g;bytes[k*3+2]=pixels[k].b;}pipe.Write(bytes,0,bytes.Length);
                if(f==300||f==750||f==1350||f==1950||f==2070){File.WriteAllBytes(Path.Combine(output,"scene-"+f+".png"),frame.EncodeToPNG());shots++;}
                Destroy(frame);
            }
            pipe.Close();encoder.WaitForExit();host.Game.Validate();host.Saves.Save(host.Game);
            File.WriteAllLines(Path.Combine(output,"events.csv"),events);
            File.WriteAllText(Path.Combine(output,"capture-report.json"),"{\"frames\":2400,\"seconds\":80,\"fps\":30,\"width\":1920,\"height\":1080,\"encoderExit\":"+encoder.ExitCode+",\"processed\":"+host.Game.Processed+",\"complete\":"+host.Game.State.complete.ToString().ToLowerInvariant()+",\"sortedDeliveryVerified\":"+sortedVerified.ToString().ToLowerInvariant()+",\"isolated\":true,\"acceleratedSection\":true}");
            Application.Quit(encoder.ExitCode);
        }
    }
}
#endif
