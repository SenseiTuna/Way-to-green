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
            yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.15f);
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
            host.SetReducedMotion(false);host.Paused=false;var pileButton=Button("CenterAction");var press=pileButton.GetComponent<CoastPressable>();var pile=pileButton.GetComponent<CoastMaterialPile>();press.OnPointerExit(pointer);yield return new WaitForSecondsRealtime(.2f);Vector3 fixedCentre=pile.VisualCenter;
            press.OnPointerEnter(pointer);yield return new WaitForSecondsRealtime(.2f);
            Test("Pile hover grows around its visual centre without sliding",()=>Assert(Vector3.Distance(pile.VisualCenter,fixedCentre)<.3f,"Hover shifted pile centre"));
            press.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(.15f);
            Test("Pile press feedback stays centred",()=>Assert(Vector3.Distance(pile.VisualCenter,fixedCentre)<.3f,"Press shifted pile centre"));
            press.OnPointerUp(pointer);press.OnPointerExit(pointer);yield return new WaitForSecondsRealtime(.2f);host.Paused=true;
            Test("Pile release returns to the same centre",()=>Assert(Vector3.Distance(pile.VisualCenter,fixedCentre)<.3f,"Release drifted"));
            Test("Larger container row and its purchase button are centred",()=>{var bin=host.View.BinRect(0);var row=(RectTransform)bin.parent;Assert(bin.rect.width>=210&&bin.rect.height>=230,"Container hit area too small");Assert(Mathf.Abs(row.InverseTransformPoint(bin.TransformPoint(bin.rect.center)).x-row.rect.center.x)<1,"Single container not centred");var icon=bin.Find("ContainerArtwork").GetComponent<RectTransform>();Assert(icon.rect.height>=170,"Container artwork not enlarged");});
            Test("Actual container image mesh aligns with its purchase button",()=>{Canvas.ForceUpdateCanvases();for(int material=0;material<host.Game.State.materials.Length;material++){var bin=host.View.BinRect(material);var image=bin.Find("ContainerArtwork").GetComponent<Image>();if(!image.gameObject.activeInHierarchy)continue;var mesh=image.canvasRenderer.GetMesh();var drawn=image.transform.TransformPoint(mesh.bounds.center);var button=(RectTransform)bin.Find("BuyBin_"+material);Assert(Mathf.Abs(drawn.x-button.TransformPoint(button.rect.center).x)<.5f,"Rendered container is left-aligned inside its image rect");}});
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
            // Late-game UI fixture: move existing finite reserves into processing, never mint units.
            for(int material=0;material<3;material++){var stock=host.Game.State.materials[material];stock.raw+=stock.reserve;stock.reserve=0;}
            host.Game.Tick(2200);host.Game.State.purchasedBinsMask=0;host.Game.Validate();host.View.MarkDirty();
            int[] lateWidths={1280,1920,2560},lateHeights={720,1080,1440};
            for(int screen=0;screen<lateWidths.Length;screen++)
            {
                host.SetDisplay(lateWidths[screen],lateHeights[screen],false);yield return new WaitForSecondsRealtime(.4f);
                Test("All six rendered containers and separate labels align at "+lateWidths[screen],()=>{
                    Canvas.ForceUpdateCanvases();float left=float.MaxValue,right=float.MinValue;var row=(RectTransform)host.View.BinRect(0).parent;
                    for(int material=0;material<6;material++){var bin=host.View.BinRect(material);Assert(bin.gameObject.activeInHierarchy,"Late-game container hidden");var image=bin.Find("ContainerArtwork").GetComponent<Image>();var mesh=image.canvasRenderer.GetMesh();var button=(RectTransform)bin.Find("BuyBin_"+material);Assert(Mathf.Abs(image.transform.TransformPoint(mesh.bounds.center).x-button.TransformPoint(button.rect.center).x)<.5f,"Actual image and price centre differ");TextFits("BinName_"+material);TextFits("BuyBin_"+material+"Text");left=Mathf.Min(left,row.InverseTransformPoint(bin.TransformPoint(new Vector3(bin.rect.xMin,0,0))).x);right=Mathf.Max(right,row.InverseTransformPoint(bin.TransformPoint(new Vector3(bin.rect.xMax,0,0))).x);}
                    Assert(Mathf.Abs((left+right)*.5f-row.rect.center.x)<1,"Six-container group not centred");
                });yield return Shot("containers-six-"+lateHeights[screen]);
            }
            host.SetLanguage("tr");yield return new WaitForSecondsRealtime(.2f);Test("All six Turkish container names and prices fit",()=>{for(int material=0;material<6;material++){TextFits("BinName_"+material);TextFits("BuyBin_"+material+"Text");}});yield return Shot("containers-six-tr-1440");
            host.SetLanguage("en");host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.6f);host.View.TogglePause();yield return null;
        }
    }
}
#endif
