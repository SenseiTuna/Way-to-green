using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KiyiDongusu
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public bool corePrototype = true;
        public GameSession Game { get; private set; }
        public SaveService Saves { get; private set; }
        public GameView View { get; private set; }
        public GameFeedback Feedback { get; private set; }
        public CoastAudio Audio { get; private set; }
        public bool Paused { get; set; }
        public bool HasLoadedSave { get; private set; }
        public bool ValidationRun { get; private set; }
        bool needsSave;
        double saveTimer;
        double retrySaveAfter;
        void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            try
            {
                var definitions = GameDefinitions.Load();
                string directory = Application.isEditor ? System.IO.Path.Combine(Application.persistentDataPath, "EditorPreview") : Application.persistentDataPath;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                ValidationRun = RuntimeSmokeRunner.Requested;
                if (ValidationRun) { directory = System.IO.Path.Combine(RuntimeSmokeRunner.OutputDirectory, "isolated-save-" + Guid.NewGuid().ToString("N")); corePrototype = false; }
                if (RuntimeRecoveryChecks.Requested) { directory = RuntimeRecoveryChecks.Prepare(definitions); corePrototype = false; }
#endif
                Saves = new SaveService(directory, definitions, corePrototype);
                var restored = Saves.Load();
                HasLoadedSave = restored != null;
                SetSession(new GameSession(definitions, corePrototype, restored));
                GameText.Language=Game.State.language;
                Feedback = gameObject.AddComponent<GameFeedback>();
                Feedback.Initialize(this);
                var soundscape=new GameObject("CoastalSoundscape");soundscape.transform.SetParent(transform,false);
                Audio=soundscape.AddComponent<CoastAudio>();Audio.Initialize(this);
                View = gameObject.AddComponent<GameView>();
                View.Initialize(this);
                Paused = HasLoadedSave || Saves.NeedsRecovery;
                View.ShowInitial(HasLoadedSave, Saves.NeedsRecovery);
                if(!Application.isEditor&&!ValidationRun&&Game.State.displayWidth>0)SetDisplay(Game.State.displayWidth,Game.State.displayHeight,Game.State.fullscreen);
                Application.wantsToQuit += TryPrepareQuit;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (ValidationRun) gameObject.AddComponent<RuntimeSmokeRunner>().Initialize(this);
                if (RuntimeRecoveryChecks.Requested) gameObject.AddComponent<RuntimeRecoveryChecks>().Initialize(this);
#endif
                Debug.Log("KIYI_READY version=" + Application.version + " profile=" + (corePrototype ? "core" : "full") + " save=" + Saves.Path);
            }
            catch (Exception error) { Debug.LogException(error); enabled = false; }
        }
        void SetSession(GameSession session)
        {
            Game = session;
            Game.Changed += OnChanged;
            Game.ImportantChange += () => needsSave = true;
        }
        void OnChanged() { if (View != null) View.MarkDirty(); }
        public void NewGameConfirmed()
        {
            var preferences=Game.State;
            bool muted = Game.State.muted, reduced = Game.State.reduceMotion;
            Saves.ConfirmNewGame();
            Game.Changed -= OnChanged;
            SetSession(new GameSession(Game.Definitions, corePrototype));
            Game.State.muted = muted; Game.State.reduceMotion = reduced;
            Game.State.language=preferences.language;Game.State.masterVolume=preferences.masterVolume;Game.State.musicVolume=preferences.musicVolume;Game.State.effectsVolume=preferences.effectsVolume;Game.State.ambienceVolume=preferences.ambienceVolume;
            Game.State.displayWidth=preferences.displayWidth;Game.State.displayHeight=preferences.displayHeight;Game.State.fullscreen=preferences.fullscreen;
            Feedback.BindSession(Game);
            Audio.Apply();
            Paused = false; needsSave = true; saveTimer = 0;
            View.MarkDirty();
        }
        void Update()
        {
            if (Game == null) return;
            if (ValidationRun) return;
            if (Input.GetKeyDown(KeyCode.Escape)) View.TogglePause();
            if (!Paused && !Game.State.complete)
            {
                // OS sleep/resume gaps do not become production; ordinary frames retain their time.
                double dt = Time.unscaledDeltaTime;
                if (dt <= 5) { Game.Tick(dt); saveTimer += dt; }
                if (Input.GetKeyDown(KeyCode.Space) || (Input.GetKeyDown(KeyCode.Return) && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null))) CenterAction();
            }
            if ((needsSave || saveTimer >= 10) && Saves.CanWrite && Time.realtimeSinceStartupAsDouble >= retrySaveAfter)
            {
                TrySaveNow();
            }
        }
        public void CenterAction()
        {
            if (Paused || Game.State.complete) return;
            int before = Game.Collected, material = Game.State.pileMaterial;
            bool active = false; foreach (var m in Game.State.materials) active |= m.batch > 0;
            if (!Game.CenterAction()) return;
            if (Game.Collected > before) Feedback.Collection(material, Game.Collected - before);
            else if (Game.State.pileRemaining == 0 && active) Feedback.Assist();
        }
        public void TokenAction(int material, int generation)
        {
            if (!Paused && Game.TokenAction(material, generation)) Feedback.Collection(material, 1);
        }
        public void Purchase(PurchaseKind kind, int facility)
        {
            if (!Paused && Game.Purchase(kind, facility))
            {
                Feedback.Purchase(kind, facility);
                Debug.Log("KIYI_PURCHASE " + kind + " rank=" + Game.Rank(kind, facility) + " money=" + Game.State.money);
            }
        }
        public bool NetAction(int material,int generation)
        {
            if(Paused||!Game.NetDropAction(material,generation))return false;
            Feedback.NetDelivery();return true;
        }
        public void SetLanguage(string language){Game.State.language=language=="tr"?"tr":"en";GameText.Language=Game.State.language;needsSave=true;View.MarkDirty();}
        public void SetVolume(int channel,float value)
        {
            value=Mathf.Clamp01(value);var s=Game.State;
            if(channel==0)s.masterVolume=value;else if(channel==1)s.musicVolume=value;else if(channel==2)s.effectsVolume=value;else s.ambienceVolume=value;
            Feedback.ApplyMute();Audio.Apply();needsSave=true;retrySaveAfter=Time.realtimeSinceStartupAsDouble+.35;View.MarkDirty();
        }
        public void SetDisplay(int width,int height,bool full)
        {
            Game.State.displayWidth=Math.Max(800,width);Game.State.displayHeight=Math.Max(600,height);Game.State.fullscreen=full;
            if(!Application.isEditor)Screen.SetResolution(Game.State.displayWidth,Game.State.displayHeight,full?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
            needsSave=true;View.MarkDirty();
        }
        public void CycleResolution()
        {
            int[] widths={1280,1600,1920},heights={720,900,1080};int current=Game.State.displayWidth>0?Game.State.displayWidth:Screen.width;
            int next=0;for(int i=0;i<widths.Length;i++)if(widths[i]==current){next=(i+1)%widths.Length;break;}
            if(widths[next]>Screen.currentResolution.width)next=0;
            SetDisplay(widths[next],heights[next],Game.State.fullscreen);
        }
        public void SetMuted(bool muted) { Game.State.muted = muted; Feedback.ApplyMute(); Audio.Apply(); needsSave = true; View.MarkDirty(); }
        public void SetReducedMotion(bool reduced) { Game.State.reduceMotion = reduced; needsSave = true; View.MarkDirty(); }
        public bool TrySaveNow()
        {
            bool saved = Saves.Save(Game);
            needsSave = !saved;
            retrySaveAfter = saved ? 0 : Time.realtimeSinceStartupAsDouble + 2;
            if (saved) saveTimer = 0;
            View.MarkDirty();
            return saved;
        }
        public bool TryPrepareQuit()
        {
            // Recovery files remain untouched when the user leaves without choosing recovery/new game.
            if (Game == null || Saves == null || !Saves.CanWrite) return true;
            if (TrySaveNow()) return true;
            Paused = true; View.ShowSaveFailure(); return false;
        }
        public bool TryRecover()
        {
            try { Saves.ConfirmRecovery(); return true; }
            catch (Exception error) { Debug.LogWarning("Kayıt geri yüklenemedi: " + error.Message); View.ShowRecoveryFailure(); return false; }
        }
        void OnDestroy() { Application.wantsToQuit -= TryPrepareQuit; }
    }
}
