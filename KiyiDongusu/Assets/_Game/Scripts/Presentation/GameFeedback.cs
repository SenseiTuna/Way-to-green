using System;
using System.Globalization;
using UnityEngine;

namespace KiyiDongusu
{
    public enum FeedbackKind { Collect, Assist, Batch, Purchase, Unlock, Complete }
    public readonly struct FeedbackNotice
    {
        public readonly FeedbackKind Kind;
        public readonly string Text;
        public readonly int Facility;
        public FeedbackNotice(FeedbackKind kind, string text, int facility = -1) { Kind = kind; Text = text; Facility = facility; }
    }

    // Presentation only: no material, wallet or simulation state is changed here.
    public sealed class GameFeedback : MonoBehaviour
    {
        public event Action<FeedbackNotice> Notice;
        public AudioSource ActionSource { get; private set; }
        public AudioSource ProductionSource { get; private set; }
        public AudioClip[] Clips { get; private set; }
        GameBootstrap host;
        GameSession session;
        long pendingReward;
        float rewardWindow, lastActionSound = -10, lastProductionSound = -10;
        int unlocked;
        int collectionTotal, collectionMaterial = -1;
        float collectionTime = -10;
        bool completed;
        readonly CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");

        public void Initialize(GameBootstrap bootstrap)
        {
            host = bootstrap;
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            ActionSource = gameObject.AddComponent<AudioSource>();
            ProductionSource = gameObject.AddComponent<AudioSource>();
            foreach (var source in new[] { ActionSource, ProductionSource })
            {
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; source.volume = .23f;
            }
            Clips = new[] {
                Tone("Collect", .055f, 420, 180), Tone("Assist", .075f, 290, 410),
                Tone("Batch", .12f, 650, 870), Tone("Purchase", .20f, 523, 784),
                Tone("Unlock", .24f, 659, 988), Tone("Complete", .40f, 523, 1046)
            };
            BindSession(bootstrap.Game);
        }
        public void BindSession(GameSession game)
        {
            if (session != null) { session.BatchRewarded -= OnReward; session.ImportantChange -= OnImportant; }
            session = game; pendingReward = 0; rewardWindow = 0;
            collectionTotal = 0; collectionMaterial = -1; collectionTime = -10;
            unlocked = CountUnlocked(); completed = game.State.complete;
            session.BatchRewarded += OnReward; session.ImportantChange += OnImportant;
            if (ActionSource != null) { ActionSource.Stop(); ProductionSource.Stop(); ApplyMute(); }
        }
        int CountUnlocked() { int count = 0; foreach (var m in session.State.materials) if (m.unlocked) count++; return count; }
        static AudioClip Tone(string name, float duration, float start, float end)
        {
            const int rate = 44100;
            var data = new float[Mathf.CeilToInt(rate * duration)];
            double phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, fraction = t / duration;
                float frequency = Mathf.Lerp(start, end, fraction);
                phase += 2 * Math.PI * frequency / rate;
                float envelope = Mathf.Min(1, t / .004f) * Mathf.Pow(1 - fraction, 2);
                data[i] = (float)(Math.Sin(phase) * .65 + Math.Sin(phase * 2) * .10) * envelope;
            }
            var clip = AudioClip.Create("Kiyi_" + name, data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
        public void ApplyMute()
        {
            bool mute = session.State.muted || host.ValidationRun;
            ActionSource.mute = mute; ProductionSource.mute = mute;
            if (mute) { ActionSource.Stop(); ProductionSource.Stop(); }
        }
        void Cue(FeedbackKind kind)
        {
            if (session.State.muted || host.ValidationRun) return;
            bool action = kind == FeedbackKind.Collect || kind == FeedbackKind.Assist || kind == FeedbackKind.Purchase;
            float now = Time.unscaledTime;
            if (action)
            {
                if (now - lastActionSound < .04f) return;
                lastActionSound = now; ActionSource.PlayOneShot(Clips[(int)kind]);
            }
            else
            {
                if (kind == FeedbackKind.Batch && now - lastProductionSound < .18f) return;
                lastProductionSound = now; ProductionSource.PlayOneShot(Clips[(int)kind]);
            }
        }
        public void Collection(int material, int quantity)
        {
            collectionTotal = material == collectionMaterial && Time.unscaledTime - collectionTime < .45f ? collectionTotal + quantity : quantity;
            collectionMaterial = material; collectionTime = Time.unscaledTime;
            Cue(FeedbackKind.Collect);
            Notice?.Invoke(new FeedbackNotice(FeedbackKind.Collect, "+" + collectionTotal + " " + session.Definitions.materials[material].display_name_tr + " → ham stok"));
        }
        public void Assist()
        {
            Cue(FeedbackKind.Assist); Notice?.Invoke(new FeedbackNotice(FeedbackKind.Assist, "Çalışan hatlara işlem desteği"));
        }
        public void Purchase(PurchaseKind kind, int facility)
        {
            string name = kind == PurchaseKind.Collector ? "Toplama ekibi" : kind == PurchaseKind.Manual ? "Toplama gücü" : kind == PurchaseKind.Speed ? "Tesis hızı" : "Tesis kapasitesi";
            Cue(FeedbackKind.Purchase);
            Notice?.Invoke(new FeedbackNotice(FeedbackKind.Purchase, name + " geliştirildi · Seviye " + session.Rank(kind, facility), kind == PurchaseKind.Speed || kind == PurchaseKind.Capacity ? facility : -1));
        }
        void OnReward(long value) { pendingReward += value; }
        void OnImportant()
        {
            int current = CountUnlocked();
            if (current > unlocked)
            {
                int newest = 0; for (int i = 0; i < session.State.materials.Length; i++) if (session.State.materials[i].unlocked) newest = i;
                Cue(FeedbackKind.Unlock);
                Notice?.Invoke(new FeedbackNotice(FeedbackKind.Unlock, "Yeni ücretsiz hat: " + session.Definitions.materials[newest].display_name_tr, session.Definitions.FacilityIndex(newest)));
            }
            unlocked = current;
            if (session.State.complete && !completed) { Cue(FeedbackKind.Complete); Notice?.Invoke(new FeedbackNotice(FeedbackKind.Complete, "Sahil tamamlandı")); }
            completed = session.State.complete;
        }
        void LateUpdate()
        {
            rewardWindow += Time.unscaledDeltaTime;
            if (pendingReward == 0 || rewardWindow < .2f) return;
            long reward = pendingReward; pendingReward = 0; rewardWindow = 0;
            if (session.State.complete) return;
            Cue(FeedbackKind.Batch);
            Notice?.Invoke(new FeedbackNotice(FeedbackKind.Batch, "+" + (reward / 100.0).ToString("N1", tr) + " kredi · işlem tamamlandı"));
        }
        void OnDestroy()
        {
            if (session != null) { session.BatchRewarded -= OnReward; session.ImportantChange -= OnImportant; }
            if (Clips != null) foreach (var clip in Clips) if (clip != null) Destroy(clip);
        }
    }
}
