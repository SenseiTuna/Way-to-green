#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed partial class RuntimeRecoveryChecks
    {
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.15f);
        }
        IEnumerator Presentation()
        {
            Button("Resume").onClick.Invoke();host.Paused=true;yield return null;
            var hint=UnityEngine.Object.FindAnyObjectByType<CoastUpgradeTooltip>(FindObjectsInactive.Include);
            var entry=Button("Speed_0").GetComponent<UpgradeHover>();var pointer=new PointerEventData(EventSystem.current);
            host.Paused=false;
            Test("Upgrade hover explains current, next and price without buying",()=>{
                long spent=host.Game.State.spent;entry.OnPointerEnter(pointer);Assert(hint.Visible&&hint.Description.Contains("Current:")&&hint.Description.Contains("Next:")&&hint.Description.Contains("Cost:"),"Hint incomplete");Assert(host.Game.State.spent==spent,"Hover purchased upgrade");
            });
            yield return new WaitForSecondsRealtime(.15f);yield return Shot("tooltip-speed-en");entry.OnPointerExit(pointer);
            var locked=Button("Capacity_3").GetComponent<UpgradeHover>();
            Test("Disabled locked upgrade still explains free unlock",()=>{locked.OnPointerEnter(pointer);Assert(hint.Visible&&hint.Description.Contains("Free facility unlock:"),"Locked reason missing");});
            yield return Shot("tooltip-locked-en");locked.OnPointerExit(pointer);
            host.SetLanguage("tr");entry.OnPointerEnter(pointer);yield return new WaitForSecondsRealtime(.1f);yield return Shot("tooltip-speed-tr");entry.OnPointerExit(pointer);host.SetLanguage("en");
            host.Paused=true;
            var token=GameObject.Find("WasteToken_0").GetComponent<BeachLitterDrag>();var frame=host.View.FrameRect;
            int collected=host.Game.Collected;pointer.position=frame.TransformPoint(frame.rect.center+new Vector2(0,325));host.Paused=false;token.OnBeginDrag(pointer);
            yield return new WaitForSecondsRealtime(.1f);float far=token.DisplayedScale;
            pointer.position=frame.TransformPoint(frame.rect.center+new Vector2(0,-245));token.OnDrag(pointer);yield return new WaitForSecondsRealtime(.1f);
            Test("Dragged litter grows toward foreground while hit target stays usable",()=>{
                Assert(token.DisplayedScale>far+.4f,"Depth scale not applied");Assert(((RectTransform)token.transform).rect.width>=110,"Hit target shrank");
            });yield return Shot("perspective-near");pointer.position=Vector2.zero;token.OnEndDrag(pointer);host.Paused=true;
            Test("Visual perspective and outside drop preserve source",()=>Assert(host.Game.Collected==collected,"Visual move collected material"));
            host.SetReducedMotion(false);host.Paused=false;host.Game.Tick(.25);host.CenterAction();host.Game.Tick(.05);
            var backdrop=UnityEngine.Object.FindAnyObjectByType<CoastBackdrop>();var gear=GameObject.Find("FacilityGear_0").GetComponent<CoastMachineGear>();float clock=backdrop.WaveTime,angle=gear.Angle;
            yield return new WaitForSecondsRealtime(.18f);
            Test("Water and working facility actually animate in Windows",()=>Assert(backdrop.WaveTime>clock&&Mathf.Abs(Mathf.DeltaAngle(angle,gear.Angle))>1,"Wave clock or working gear did not move"));
            yield return Shot("motion-on");host.SetReducedMotion(true);yield return null;float frozen=backdrop.WaveTime;
            yield return new WaitForSecondsRealtime(.15f);
            Test("Reduced motion freezes waves and resets mechanical movement",()=>Assert(backdrop.WaveTime==frozen&&Mathf.Abs(Mathf.DeltaAngle(gear.Angle,0))<.01,"Reduced motion not respected"));host.Paused=true;
            int[] widths={1920,2560},heights={1200,1080};
            for(int i=0;i<widths.Length;i++)
            {
                int width=widths[i],height=heights[i];host.SetDisplay(width,height,false);yield return new WaitForSecondsRealtime(.6f);
                Test("Full canvas fills "+width+"x"+height+" without letterbox panels",()=>{
                    Assert(Screen.width==width&&Screen.height==height,"Render dimensions differ");var corners=new Vector3[4];frame.GetWorldCorners(corners);
                    Assert(Mathf.Abs(corners[0].x)<1&&Mathf.Abs(corners[0].y)<1&&Mathf.Abs(corners[2].x-width)<1&&Mathf.Abs(corners[2].y-height)<1,"Frame does not fill screen");
                    ((RectTransform)backdrop.transform).GetWorldCorners(corners);Assert(Mathf.Abs(corners[0].x)<1&&Mathf.Abs(corners[2].x-width)<1,"Beach does not cover side margins");
                    Assert(backdrop.GetComponent<RawImage>().material.shader.isSupported,"Water shader unsupported");
                });yield return Shot("viewport-"+width+"x"+height);
            }
            int nativeWidth=Display.main.systemWidth,nativeHeight=Display.main.systemHeight;host.SetDisplay(nativeWidth,nativeHeight,true);yield return new WaitForSecondsRealtime(.8f);
            Test("Native fullscreen preserves display aspect "+nativeWidth+"x"+nativeHeight,()=>Assert(Screen.fullScreenMode==FullScreenMode.FullScreenWindow&&Screen.width==host.Game.State.displayWidth&&Screen.height==host.Game.State.displayHeight,"Fullscreen actual="+Screen.width+"x"+Screen.height+" mode="+Screen.fullScreenMode+" expected="+host.Game.State.displayWidth+"x"+host.Game.State.displayHeight));yield return Shot("fullscreen-native");
            host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.6f);host.View.TogglePause();yield return null;
        }
    }
}
#endif
