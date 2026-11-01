#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
namespace KiyiDongusu
{
    public sealed partial class RuntimeRecoveryChecks
    {
        IEnumerator Typography()
        {
            Button("Resume").onClick.Invoke();host.Paused=true;yield return null;
            int collected=host.Game.Collected;long money=host.Game.State.money;
            Test("SDF fonts contain English, Turkish and display punctuation",()=>{
                foreach(string name in new[]{"Lato-Regular SDF","Lato-Bold SDF"})
                {
                    var font=Resources.Load<TMP_FontAsset>("Typography/"+name);
                    Assert(font!=null&&font.material.shader.isSupported,"Missing or unsupported SDF font");
                    Assert(font.HasCharacters("Coastal Loop ÇçĞğİıÖöŞşÜü×…"),"Missing display glyphs in "+name);
                }
            });
            int[] widths={1280,1920,2560},heights={720,1080,1440};
            for(int i=0;i<widths.Length;i++)
            {
                int width=widths[i],height=heights[i];host.SetDisplay(width,height,false);yield return new WaitForSecondsRealtime(.6f);
                Test("Actual Windows typography at "+width+"x"+height,()=>{
                    Assert(Screen.width==width&&Screen.height==height,"Actual render target differs: "+Screen.width+"x"+Screen.height);
                    var labels=UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Where(t=>t.enabled&&t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)).ToArray();
                    Assert(labels.Length>20,"Gameplay UI missing");
                    foreach(var label in labels){label.ForceMeshUpdate();Assert(!label.isTextTruncated,"Truncated gameplay text: "+label.name+" / "+label.text);}
                });
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"typography-gameplay-"+height+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.2f);
            }
            host.SetLanguage("tr");yield return new WaitForSecondsRealtime(.2f);
            Test("Turkish gameplay labels fit at 1440p",()=>{
                foreach(var label in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                    if(label.enabled&&label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text)){label.ForceMeshUpdate();Assert(!label.isTextTruncated,"Truncated Turkish text: "+label.name);}
            });
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"typography-tr-1440.png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.2f);
            host.View.TogglePause();yield return new WaitForSecondsRealtime(.2f);
            Test("Turkish settings labels fit at 1440p",()=>{
                foreach(var label in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                    if(label.enabled&&label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text)){label.ForceMeshUpdate();Assert(!label.isTextTruncated,"Truncated Turkish setting: "+label.name);}
            });
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"typography-settings-tr-1440.png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.2f);
            host.SetLanguage("en");host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.5f);
            Test("Typography and presentation do not change finite economy",()=>Assert(host.Game.Collected==collected&&host.Game.State.money==money,"Presentation changed economy"));
        }
    }
}
#endif
