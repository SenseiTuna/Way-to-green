using System;
using UnityEngine;
using UnityEngine.UI;
using Text=TMPro.TextMeshProUGUI;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        void ClearOverlay(string title,string explanation,bool tall=false)
        {
            HideDialogue();questLogOpen=false;questRows.Clear();
            buttons.RemoveAll(b=>b==null||b.transform.IsChildOf(overlay));words.RemoveAll(w=>w.text==null||w.text.transform.IsChildOf(overlay));
            for(int i=overlay.childCount-1;i>=0;i--)Destroy(overlay.GetChild(i).gameObject);
            overlay.gameObject.SetActive(true);host.Paused=true;MarkDirty();
            Panel("DialogSurface",overlay,330,tall?100:200,1260,tall?880:620,CoastTheme.SolidPaper,true);
            Label("OverlayTitle",overlay,390,tall?125:220,1120,90,title,44,null,true);
            Label("OverlayExplanation",overlay,390,tall?202:320,1120,tall?50:170,explanation,26);
        }
        void Resume(){questLogOpen=false;overlay.gameObject.SetActive(false);host.Paused=false;MarkDirty();}
        public void ShowInitial(bool loaded,bool recovery)
        {
            if(recovery)
            {
                ClearOverlay(T("Your save needs attention","Kayıt kontrolü gerekiyor"),host.Saves.LastMessage);
                if(loaded)Button("RestoreBackup",overlay,390,540,550,80,T("Restore valid backup","Geçerli yedeği geri yükle"),()=>{if(host.TryRecover())Resume();},out _);
                Button("NewGame",overlay,970,540,550,80,T("New game…","Yeni oyun…"),ConfirmNewGame,out _,true);
            }
            else if(loaded)
            {
                ClearOverlay(T("Welcome back to the shore","Sahile tekrar hoş geldin"),T("Your progress is here. No production was added while the game was closed.","İlerlemen korundu. Oyun kapalıyken üretim eklenmedi."));
                Button("Continue",overlay,390,540,550,80,T("Continue","Devam et"),Resume,out _);
                Button("NewGame",overlay,970,540,550,80,T("New game…","Yeni oyun…"),ConfirmNewGame,out _,true);
            }
        }
        public void TogglePause(){if(host.Saves.NeedsRecovery||G.State.complete)return;if(overlay.gameObject.activeSelf){Resume();return;}ShowPause();}
        void Volume(string id,string title,float y,double value,Action<float> change)
        {
            Label(id+"Label",overlay,390,y,270,42,title,25);
            var surface=Panel(id,overlay,700,y+6,650,42,CoastTheme.Clear);surface.GetComponent<Image>().raycastTarget=true;var slider=surface.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            Panel("Track",surface,0,17,650,8,CoastTheme.Track,true);
            var fillArea=Panel("FillArea",surface,0,17,650,8,CoastTheme.Clear);
            var fill=Panel("Fill",fillArea,0,0,0,0,CoastTheme.Action,true);fill.sizeDelta=Vector2.zero;slider.fillRect=fill;
            var handle=Panel("Handle",surface,0,3,26,-6,CoastTheme.Action,true);handle.GetComponent<Image>().raycastTarget=true;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.direction=Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify((float)value);var text=Label(id+"Value",overlay,1390,y,130,42,Mathf.RoundToInt((float)value*100)+"%",24);
            surface.gameObject.AddComponent<CoastPressable>().Initialize(host);
            slider.onValueChanged.AddListener(v=>{change(v);text.text=Mathf.RoundToInt(v*100)+"%";});
        }
        void ShowPause()
        {
            ClearOverlay(T("A little breathing room","Kısa bir mola"),T("The beach is paused while you adjust your settings.","Ayarlar açıkken sahil duraklatılır."),true);
            var s=G.State;
            Volume("MasterVolume",T("Master volume","Ana ses"),270,s.masterVolume,v=>host.SetVolume(0,v));
            Volume("MusicVolume",T("Music","Müzik"),328,s.musicVolume,v=>host.SetVolume(1,v));
            Volume("EffectsVolume",T("Click and effects","Tıklama ve efektler"),386,s.effectsVolume,v=>host.SetVolume(2,v));
            Volume("AmbienceVolume",T("Ocean ambience","Dalga ambiyansı"),444,s.ambienceVolume,v=>host.SetVolume(3,v));
            Button("Mute",overlay,390,516,350,62,s.muted?T("Sound off","Ses kapalı"):T("Sound on","Ses açık"),()=>{host.SetMuted(!s.muted);ShowPause();},out _,true);
            Button("Language",overlay,770,516,350,62,T("Language: English","Dil: Türkçe"),()=>{host.SetLanguage(s.language=="en"?"tr":"en");ShowPause();},out _,true);
            Button("ReducedMotion",overlay,1150,516,370,62,s.reduceMotion?T("Motion: reduced","Hareket: azaltılmış"):T("Motion: full","Hareket: açık"),()=>{host.SetReducedMotion(!s.reduceMotion);ShowPause();},out _,true);
            Button("Resolution",overlay,390,610,730,62,T("Resolution: ","Çözünürlük: ")+(s.displayWidth>0?s.displayWidth:Screen.width)+" × "+(s.displayHeight>0?s.displayHeight:Screen.height),()=>{host.CycleResolution();ShowPause();},out _,true);
            Button("Fullscreen",overlay,1150,610,370,62,s.fullscreen?T("Fullscreen","Tam ekran"):T("Windowed","Pencere"),()=>{host.SetDisplay(s.displayWidth>0?s.displayWidth:Screen.width,s.displayHeight>0?s.displayHeight:Screen.height,!s.fullscreen);ShowPause();},out _,true);
            Button("MiraGuide",overlay,390,684,1130,44,s.story.enabled?T("Mira guide: on","Mira rehberi: açık"):T("Mira guide: off","Mira rehberi: kapalı"),()=>{host.Story.SetEnabled(!s.story.enabled);ShowPause();},out _,true);
            Button("Resume",overlay,390,732,1130,76,T("Back to the beach","Sahile dön"),Resume,out _);
            Button("New",overlay,390,846,550,62,T("New game…","Yeni oyun…"),ConfirmNewGame,out _,true);
            Button("Quit",overlay,970,846,550,62,T("Save and quit","Kaydet ve çık"),()=>Application.Quit(),out _,true);
        }
        public void ShowSaveFailure()
        {
            ClearOverlay(T("Your progress could not be saved","İlerleme kaydedilemedi"),T("The game stayed open to protect your progress. Check free storage and write permissions, then retry. Your previous save is untouched.","İlerlemeyi korumak için oyun açık kaldı. Boş alanı ve yazma iznini kontrol edip yeniden dene. Önceki kayıt korundu."));
            Button("RetrySave",overlay,390,540,550,80,T("Retry save","Yeniden kaydet"),()=>{if(host.TrySaveNow())ShowPause();else ShowSaveFailure();},out _);
            Button("BackToGame",overlay,970,540,550,80,T("Back to game","Oyuna dön"),Resume,out _,true);
        }
        public void ShowRecoveryFailure(){ShowInitial(host.HasLoadedSave,true);overlay.Find("OverlayExplanation").GetComponent<Text>().text=T("The backup could not be restored. Files are preserved. Check storage and write permissions before retrying.","Yedek geri yüklenemedi. Dosyalar korunuyor; boş alan ve yazma iznini kontrol et.");}
        void ConfirmNewGame()
        {
            ClearOverlay(T("Start a fresh shore?","Sahile yeniden başlansın mı?"),T("This replaces your current progress. Your old save is preserved locally. Sound, language and display settings are kept.","Mevcut ilerlemenin yerine yeni oyun başlar. Eski kayıt yerelde korunur. Ses, dil ve ekran ayarları kalır."));
            Button("ConfirmNew",overlay,390,540,550,80,T("Yes, start new game","Evet, yeni oyun başlat"),()=>{host.NewGameConfirmed();resultShown=false;Resume();},out _);
            Button("CancelNew",overlay,970,540,550,80,T("Keep my progress","İlerlememi koru"),()=>{overlay.gameObject.SetActive(false);if(host.Saves.NeedsRecovery)ShowInitial(host.HasLoadedSave,true);else if(G.State.complete)ShowResult();else Resume();},out _,true);
        }
        void ShowResult()
        {
            ClearOverlay(T("A shore worth coming back to","Tertemiz bir sahil"),G.TotalUnits+T(" pieces collected and recycled.\n"," parça toplandı ve işlendi.\n")+T("Earned ","Kazanılan ")+Money(G.State.earned)+T(" credits. Time "," kredi. Süre ")+TimeSpan.FromSeconds(G.State.elapsed).ToString(@"mm\:ss"),true);
            overlay.Find("OverlayExplanation").GetComponent<RectTransform>().sizeDelta=new Vector2(880,100);
            var farewell=Panel("MiraFarewell",overlay,1360,168,160,155,CoastTheme.White).GetComponent<Image>();farewell.sprite=art.Mira[2];farewell.preserveAspect=true;
            var picture=Panel("ShoreTransformation",overlay,390,330,1130,340,CoastTheme.White);
            var photo=new GameObject("BeachPreview",typeof(RectTransform),typeof(RawImage));photo.transform.SetParent(picture,false);var photoRect=(RectTransform)photo.transform;photoRect.anchorMin=Vector2.zero;photoRect.anchorMax=Vector2.one;photoRect.offsetMin=photoRect.offsetMax=Vector2.zero;var photoImage=photo.GetComponent<RawImage>();photoImage.texture=art.Beach.texture;photoImage.uvRect=new Rect(0,.29f,1,.45f);photoImage.raycastTarget=false;
            Panel("AfterCaptionPlate",picture,20,18,270,50,CoastTheme.SolidPaper,true);Label("AfterCaption",picture,36,18,238,50,T("Shore cleared","Sahil temizlendi"),25,null,true);
            var before=Panel("FirstPileLayer",picture,0,0,1130,340,CoastTheme.Clear);var group=before.gameObject.AddComponent<CanvasGroup>();
            var pile=Panel("FirstPileIllustration",before,360,100,500,225,CoastTheme.White).GetComponent<Image>();pile.sprite=art.Pile;pile.preserveAspect=true;
            for(int i=0;i<3;i++){var piece=Panel("FirstLoose"+i,before,130+i*270,120+i%2*90,65,60,CoastTheme.White).GetComponent<Image>();piece.sprite=art.Waste[i];piece.preserveAspect=true;}
            Panel("BeforeCaptionPlate",before,20,18,270,50,CoastTheme.SolidPaper,true);Label("BeforeCaption",before,36,18,238,50,T("The first pile","İlk yığın"),25,null,true);
            picture.gameObject.AddComponent<CoastShoreReveal>().Initialize(host,group);
            Label("RevealExplanation",overlay,390,686,1130,44,T("Mira: Every piece recycled. Thank you for bringing this shore back.","Mira: Her parça geri kazanıldı. Bu sahile yeniden hayat verdin."),25);
            Button("New",overlay,390,752,550,76,T("New game…","Yeni oyun…"),ConfirmNewGame,out _);
            Button("Exit",overlay,970,752,550,76,T("Save and quit","Kaydet ve çık"),()=>Application.Quit(),out _,true);
            Button("ReviewShoreTasks",overlay,390,846,1130,54,T("Read Mira's notes / 8 tasks completed","Mira'nın notlarını oku / 8 görev tamamlandı"),()=>ShowQuestLog(true,7),out _,true);
            Label("Version",overlay,390,914,1130,38,T("Prototype ","Prototip ")+"V"+Application.version,22);
        }
    }
}
