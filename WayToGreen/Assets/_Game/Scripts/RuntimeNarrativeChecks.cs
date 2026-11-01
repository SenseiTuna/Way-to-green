#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;
namespace KiyiDongusu
{
    public sealed partial class RuntimeRecoveryChecks
    {
        void TextFits(string name){var t=GameObject.Find(name).GetComponent<TextMeshProUGUI>();t.ForceMeshUpdate();Assert(!t.isTextTruncated,name+" truncated");Assert(t.preferredHeight<=t.rectTransform.rect.height+2,name+" overflows: "+t.preferredHeight+" / "+t.rectTransform.rect.height);}
        IEnumerator Stories()
        {
            Button("RetrySave").onClick.Invoke();yield return null;Button("Resume").onClick.Invoke();yield return null;
            // Disk-obstruction errors above are intentional. Retain their log, clear only the QA HUD.
            Debug.developerConsoleVisible=false;
            host.SetDisplay(1280,720,false);yield return new WaitForSecondsRealtime(.5f);
            double elapsed=host.Game.State.elapsed;int collected=host.Game.Collected;
            yield return new WaitForSecondsRealtime(.3f);
            Test("New shore intro blocks production and manual collection",()=>{host.CenterAction();Assert(host.Story.IntroBlocking&&host.View.DialogueVisible&&host.Game.State.elapsed==elapsed&&host.Game.Collected==collected,"Intro gate missing");TextFits("DialogueBody");});yield return Shot("mira-intro-en-720");
            Test("Mira is a wide lower-screen mascot and covered controls cannot receive clicks",()=>{var band=GameObject.Find("MiraDialogue").GetComponent<RectTransform>();var portrait=GameObject.Find("MiraPortrait").GetComponent<RectTransform>();Assert(Mathf.Abs(band.rect.width-host.View.FrameRect.rect.width+64)<1&&portrait.rect.height>=450,"Mascot too small or band not responsive");Assert(band.GetComponent<UnityEngine.UI.Image>().raycastTarget&&portrait.GetComponent<UnityEngine.UI.Image>().raycastTarget,"Covered gameplay can receive pointer clicks");});
            Button("DialogueNext").onClick.Invoke();yield return null;
            Test("Intro fact names specific cleanup year and persists active page",()=>{Assert(host.Game.State.story.page==1,"Wrong page");TextFits("DialogueBody");TextFits("DialogueSource");Assert(host.TrySaveNow(),"Page save failed");var restored=host.Saves.Load();Assert(restored.story.activeId==0&&restored.story.page==1,"Saved page lost");Assert(GameObject.Find("DialogueBody").GetComponent<TextMeshProUGUI>().text.Contains("2024"),"Fact year missing");});yield return Shot("mira-fact-en-720");
            host.SetLanguage("tr");yield return null;host.Story.Step(0);yield return null;
            Test("Turkish fact and attribution fit",()=>{TextFits("DialogueBody");TextFits("DialogueSource");});yield return Shot("mira-fact-tr-720");
            Button("DialogueSkip").onClick.Invoke();host.SetLanguage("en");
            Test("Six introductory paper pieces reveal the first pile once",()=>{Assert(host.Game.State.pileRemaining==0&&OpeningProgress.Remaining(host.Game.State)==6,"Pile arrived early");for(int slot=0;slot<6;slot++){host.Game.Tick(.25);Button("OpeningPaper_"+slot).onClick.Invoke();}Assert(host.Game.State.opening.complete&&host.Game.State.pileMaterial==0&&host.Game.State.pileRemaining>0&&NarrativeProgress.Done(host.Game,0),"Paper opening did not transition");host.Game.Validate();});yield return Shot("first-paper-pile");
            var pileVisual=GameObject.Find("CenterAction").GetComponent<CoastMaterialPile>();
            Test("Full paper pile contains only paper artwork",()=>Assert(pileVisual.Material==0&&pileVisual.VisiblePieces==24,"Wrong pile artwork"));
            Vector3 initialPileCenter=pileVisual.VisualCenter;
            foreach(int remaining in new[]{18,12,6}){while(host.Game.State.pileRemaining>remaining)host.Game.Supply.CollectPileUnit();yield return null;int expected=remaining;Test("Visible material pile decreases at "+(remaining*100/24)+" percent",()=>Assert(pileVisual.VisiblePieces==expected&&Vector3.Distance(initialPileCenter,pileVisual.VisualCenter)<.2f,"Pile shrank off-centre"));}
            yield return Shot("paper-pile-quarter");
            Test("Skip prevents intro replay and collection counts retroactively",()=>{for(int i=0;i<50;i++){host.Game.Tick(.5);host.CenterAction();}Assert(NarrativeProgress.Done(host.Game,0)&&host.Game.Collected>=20,"Collection task ignored");Assert(!host.Game.State.story.pending.Contains(0),"Skipped intro repeated");host.Game.Validate();});
            host.Game.State.story.cooldown=0;host.Story.Step(0);yield return null;
            double before=host.Game.State.elapsed;yield return new WaitForSecondsRealtime(.25f);
            Test("Later tutorial permits background production",()=>Assert(host.View.DialogueVisible&&!host.Story.IntroBlocking&&host.Game.State.elapsed>before,"Later dialogue paused simulation"));yield return Shot("mira-first-reward");
            Test("Free net drag collects raw stock with no container and no bonus",()=>{var token=GameObject.Find("WasteToken_0").GetComponent<BeachLitterDrag>();var target=host.View.NetRect;var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center))};int collected=host.Game.Collected,tagged=host.Game.NetDelivered;long money=host.Game.State.money;token.OnBeginDrag(pointer);token.OnEndDrag(pointer);Assert(host.Game.Collected==collected+1&&host.Game.NetDelivered==tagged&&host.Game.State.money==money&&host.Game.State.purchasedBinsMask==0,"Free net failed or gave bonus");});yield return new WaitForSecondsRealtime(.25f);
            Test("Unpurchased container rejects a real drag without consuming its litter",()=>{
                var token=GameObject.Find("WasteToken_0").GetComponent<BeachLitterDrag>();var target=host.View.BinRect(0);var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center))};int count=host.Game.Collected;token.OnBeginDrag(pointer);token.OnEndDrag(pointer);Assert(host.Game.Collected==count&&!host.Game.OwnsBin(0),"Unbought container accepted litter");
            });
            while(host.Game.State.money<host.Game.Cost(PurchaseKind.SortingBin,0)&&host.Game.State.elapsed<300){host.Game.Tick(.5);host.CenterAction();}yield return null;
            Test("Container purchase UI charges only once under rapid repeated clicks",()=>{long money=host.Game.State.money,price=host.Game.Cost(PurchaseKind.SortingBin,0);Assert(money>=price,"Paper fixture has insufficient processing credits");for(int i=0;i<100;i++)Button("BuyBin_0").onClick.Invoke();Assert(host.Game.OwnsBin(0)&&host.Game.State.money==money-price,"Container charged incorrectly");});yield return new WaitForSecondsRealtime(.25f);
            Test("Purchased matching container accepts drag and defers bonus until processing",()=>{
                var token=GameObject.Find("WasteToken_0").GetComponent<BeachLitterDrag>();var target=host.View.BinRect(0);var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center))};int count=host.Game.Collected,tagged=host.Game.NetDelivered;long money=host.Game.State.money;token.OnBeginDrag(pointer);token.OnEndDrag(pointer);Assert(host.Game.Collected==count+1&&host.Game.NetDelivered==tagged+1&&host.Game.State.money==money,"Drop failed or paid early");host.Game.Validate();
            });host.Story.Skip();host.Game.Tick(.8);yield return Shot("paper-container-purchased");host.Game.State.story.cooldown=0;host.Story.Step(0);yield return null;
            Button("QuestLog").onClick.Invoke();yield return null;before=host.Game.State.elapsed;yield return new WaitForSecondsRealtime(.2f);
            Test("Quest log hides NPC, permits production and blocks manual input",()=>{int n=host.Game.Collected;host.CenterAction();Assert(!host.View.DialogueVisible&&!host.Paused&&host.Game.State.elapsed>before&&host.Game.Collected==n,"Quest modal/input conflict");TextFits("QuestDetails");});yield return Shot("quests-active-en");
            Button("QuestCompletedTab").onClick.Invoke();yield return null;
            Test("Completed task opens sourced note without currency reward",()=>{long credits=host.Game.State.money;Button("QuestRow_0").onClick.Invoke();Assert(host.Game.State.money==credits,"Quest minted currency");});yield return null;
            Test("Completed environmental note fits and source is available",()=>{TextFits("QuestDetails");TextFits("QuestSource");Assert(Button("QuestSourceLink").interactable,"Source link missing");});yield return Shot("quests-completed-en");
            host.SetLanguage("tr");host.View.ShowQuestLog(true,0);yield return null;
            Test("Turkish quest notes fit at 720p",()=>{TextFits("QuestDetails");TextFits("QuestSource");});yield return Shot("quests-completed-tr-720");
            Button("QuestClose").onClick.Invoke();host.View.TogglePause();yield return null;
            Button("MiraGuide").onClick.Invoke();yield return null;
            Test("Guide toggle saves independently from quest progress",()=>{Assert(!host.Game.State.story.enabled&&host.Game.State.story.activeId<0,"Guide not disabled");Assert(host.TrySaveNow()&&!host.Saves.Load().story.enabled&&NarrativeProgress.Done(host.Game,0),"Toggle/quest persistence failed");});yield return Shot("mira-settings-tr");
            Button("Resume").onClick.Invoke();host.Story.SetEnabled(true);host.SetLanguage("en");host.SetDisplay(1920,1080,false);yield return new WaitForSecondsRealtime(.6f);host.View.ShowQuestLog(true,0);yield return null;
            Test("Quest note fits at 1080p and save remains consistent",()=>{TextFits("QuestDetails");host.Game.Validate();Assert(host.TrySaveNow(),"Final story save failed");});yield return Shot("quests-completed-en-1080");Button("QuestClose").onClick.Invoke();host.View.TogglePause();
            Test("Every English and Turkish dialogue page and task note fits its real text box",()=>{
                var content=NarrativeContent.Load();foreach(string language in new[]{"en","tr"}){host.SetLanguage(language);foreach(var node in content.nodes)for(int page=0;page<node.pages.Length;page++){host.View.RenderDialogue(node.id,page,1);TextFits("DialogueBody");TextFits("DialogueSource");}host.View.HideDialogue();host.View.ShowQuestLog(true,0);var label=GameObject.Find("QuestDetails").GetComponent<TextMeshProUGUI>();foreach(var quest in content.quests){var fact=content.Fact(quest.fact);label.text=NarrativeContent.Resolve(language=="en"?quest.descriptionEn:quest.descriptionTr,host.Game)+"\n"+NarrativeContent.Resolve(language=="en"?fact.en:fact.tr,host.Game);TextFits("QuestDetails");}}host.SetLanguage("en");host.View.TogglePause();
            });
            Test("Real legacy JSON file migrates once in Windows without changing any economy field",()=>{
                string json=JsonUtility.ToJson(host.Game.State);json=json.Substring(0,json.LastIndexOf(",\"story\":",StringComparison.Ordinal))+"}";var raw=JsonUtility.FromJson<GameState>(json);Assert(raw.story.version==0,"Missing story not detected");string expected=JsonUtility.ToJson(raw);expected=expected.Substring(0,expected.LastIndexOf(",\"story\":",StringComparison.Ordinal));string directory=Path.Combine(output,"legacy-file");Directory.CreateDirectory(directory);string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json)));File.WriteAllText(Path.Combine(directory,"beach-v1.json"),JsonUtility.ToJson(new SaveEnvelope{payload=json,sha256=hash}));var saves=new SaveService(directory,host.Game.Definitions,false);var loaded=saves.Load();Assert(loaded!=null&&!saves.NeedsRecovery,"Legacy file rejected");string actual=JsonUtility.ToJson(loaded);actual=actual.Substring(0,actual.LastIndexOf(",\"story\":",StringComparison.Ordinal));Assert(actual==expected&&loaded.story.pending.Count==1&&loaded.story.pending[0]==9,"Legacy progress changed or tutorials repeated");var game=new GameSession(host.Game.Definitions,false,loaded);Assert(saves.Save(game),"Migrated save failed");Assert(JsonUtility.ToJson(saves.Load())==JsonUtility.ToJson(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(game.State))),"Second load changed migrated state");
            });
        }
    }
}
#endif
