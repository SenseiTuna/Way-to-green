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
        public bool Paused { get; set; }
        public bool HasLoadedSave { get; private set; }
        public bool ValidationRun { get; private set; }
        bool needsSave;
        double saveTimer;
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
#endif
                Saves = new SaveService(directory, definitions, corePrototype);
                var restored = Saves.Load();
                HasLoadedSave = restored != null;
                SetSession(new GameSession(definitions, corePrototype, restored));
                Feedback = gameObject.AddComponent<GameFeedback>();
                Feedback.Initialize(this);
                View = gameObject.AddComponent<GameView>();
                View.Initialize(this);
                Paused = HasLoadedSave || Saves.NeedsRecovery;
                View.ShowInitial(HasLoadedSave, Saves.NeedsRecovery);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (ValidationRun) gameObject.AddComponent<RuntimeSmokeRunner>().Initialize(this);
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
            bool muted = Game.State.muted, reduced = Game.State.reduceMotion;
            Saves.ConfirmNewGame();
            Game.Changed -= OnChanged;
            SetSession(new GameSession(Game.Definitions, corePrototype));
            Game.State.muted = muted; Game.State.reduceMotion = reduced;
            Feedback.BindSession(Game);
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
            if ((needsSave || saveTimer >= 10) && Saves.CanWrite)
            {
                Saves.Save(Game); needsSave = false; saveTimer = 0; View.MarkDirty();
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
        public void SetMuted(bool muted) { Game.State.muted = muted; Feedback.ApplyMute(); needsSave = true; View.MarkDirty(); }
        public void SetReducedMotion(bool reduced) { Game.State.reduceMotion = reduced; needsSave = true; View.MarkDirty(); }
        void OnApplicationQuit() { if (Game != null && Saves != null && Saves.CanWrite) Saves.Save(Game); }
    }
}
