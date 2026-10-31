using UnityEngine;
namespace KiyiDongusu
{
    public sealed class NarrativeDirector:MonoBehaviour
    {
        GameBootstrap host;bool suppressed;int lastPage=-1,lastId=-1,lastQuests;float entrance;
        public bool IntroBlocking=>!suppressed&&host.Game.State.story.enabled&&host.Game.State.story.activeId==0;
        public void Initialize(GameBootstrap game,bool quiet){host=game;suppressed=quiet;Bind();Step(0);}
        public void Bind(){lastId=lastPage=-1;entrance=0;lastQuests=host.Game.State.story.questMask;host.View.HideDialogue();host.Audio.SetDialogueGain(1);}
        public void Step(float dt)
        {
            if(suppressed){host.View.HideDialogue();return;}
            var s=host.Game.State.story;
            if(lastQuests!=s.questMask)
            {
                int added=s.questMask&~lastQuests;lastQuests=s.questMask;
                if(added!=0&&!host.Game.State.complete)host.Feedback.NarrativeNotice(GameText.T("Mira: another shore task completed!","Mira: bir sahil görevi daha tamamlandı!"));
            }
            bool busy=host.Paused||host.View.ModalOpen||host.View.AnyLitterDragging||host.Game.State.complete||!host.Saves.CanWrite;
            if(busy){host.View.HideDialogue();host.Audio.SetDialogueGain(1);return;}
            if(!s.enabled){host.View.HideDialogue();host.Audio.SetDialogueGain(1);return;}
            if(s.activeId<0){s.cooldown=System.Math.Max(0,s.cooldown-dt);if(NarrativeProgress.Begin(host.Game)){host.MarkNarrativeChanged();entrance=0;}}
            if(s.activeId<0){host.View.HideDialogue();host.Audio.SetDialogueGain(1);return;}
            s.activeSeconds+=dt;
            if(s.activeId>=10&&s.activeSeconds>=4){Skip();return;}
            if(lastId!=s.activeId||lastPage!=s.page){lastId=s.activeId;lastPage=s.page;entrance=0;host.Feedback.Tap();}
            entrance+=dt;host.View.RenderDialogue(s.activeId,s.page,host.Game.State.reduceMotion?1:Mathf.Clamp01(entrance/.22f));host.Audio.SetDialogueGain(.72f);
        }
        public void Advance(){NarrativeProgress.Advance(host.Game);host.MarkNarrativeChanged();Step(0);}
        public void Skip(){NarrativeProgress.Dismiss(host.Game);host.MarkNarrativeChanged();host.View.HideDialogue();host.Audio.SetDialogueGain(1);}
        public void SetEnabled(bool value){host.Game.State.story.enabled=value;NarrativeProgress.Sync(host.Game);host.MarkNarrativeChanged();if(!value){host.View.HideDialogue();host.Audio.SetDialogueGain(1);}}
    }
}
