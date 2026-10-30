using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiyiDongusu
{
    // Temporary, code-built uGUI. Economy is owned exclusively by GameSession.
    public sealed class GameView : MonoBehaviour
    {
        GameBootstrap host;
        GameSession G => host.Game;
        readonly Color ink = new Color32(24, 52, 61, 255);
        readonly Color teal = new Color32(14, 104, 108, 255);
        readonly Color pale = new Color32(239, 246, 237, 255);
        readonly Color sand = new Color32(232, 207, 151, 255);
        readonly CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        Font font;
        GameArt art;
        Image pileImage;
        RectTransform root, beach, overlay;
        Text money, income, target, status, cleanup, recycled, pile, mode, result;
        Button center;
        Text feedbackText;
        CanvasGroup feedbackGroup;
        float feedbackRemaining, centerPulse;
        int feedbackPriority;
        readonly Image[] facilityCards = new Image[4];
        readonly float[] facilityFlash = new float[4];
        readonly List<(int i, Text text, Image bar)> lines = new List<(int, Text, Image)>();
        readonly List<(int i, Button button, Text text)> tokens = new List<(int, Button, Text)>();
        readonly List<(PurchaseKind kind, int facility, Button button, Text text)> upgrades = new List<(PurchaseKind, int, Button, Text)>();
        readonly List<Image> dirt = new List<Image>();
        readonly List<Button> buttons = new List<Button>();
        readonly int[] displayedGenerations = new int[6];
        bool dirty = true, resultShown;
        float renderTimer;
        public void MarkDirty() { dirty = true; }
        RectTransform Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return r;
        }
        Text Label(string name, Transform parent, float x, float y, float w, float h, string value, int size = 26, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            var text = go.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color ?? ink; text.supportRichText = true; text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft; return text;
        }
        Button Button(string name, Transform parent, float x, float y, float w, float h, string value, Action action, out Text label)
        {
            var r = Panel(name, parent, x, y, w, h, teal); r.GetComponent<Image>().raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>(); b.onClick.AddListener(() => action());
            // Keyboard main action is handled once by the bootstrap, avoiding Submit+Return double dispatch.
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            buttons.Add(b);
            label = Label(name + "Text", r, 14, 4, w - 28, h - 8, value, 25, Color.white); label.alignment = TextAnchor.MiddleCenter;
            return b;
        }
        public void Initialize(GameBootstrap bootstrap)
        {
            host = bootstrap;
            art = new GameArt();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = canvasGo.GetComponent<RectTransform>();
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.GetComponent<EventSystem>().sendNavigationEvents = false;
            Panel("Background", root, 0, 0, 1920, 1080, pale);
            Label("Title", root, 24, 18, 560, 60, "KIYI DÖNGÜSÜ", 38);
            money = Label("Credits", root, 610, 18, 420, 60, "", 34);
            income = Label("Income", root, 1050, 18, 540, 60, "", 25);
            Button("Pause", root, 1660, 18, 236, 72, "Duraklat · ESC", TogglePause, out _);
            target = Label("NextGoal", root, 24, 104, 1872, 62, "", 28);
            beach = Panel("Beach", root, 24, 182, 1188, 768, sand);
            Panel("Sea", beach, 0, 0, 1188, 142, new Color32(102, 184, 184, 255));
            Panel("WaterEdge", beach, 0, 142, 1188, 14, new Color32(202, 231, 216, 255));
            if (art.Beach != null)
            {
                var background = Panel("BeachArtwork", beach, 0, 0, 1188, 768, Color.white).GetComponent<Image>();
                background.sprite = art.Beach;
            }
            Label("BeachTitle", beach, 28, 20, 1120, 60, "TEK SAHİL · SONLU KAYNAK", 30);
            var noticePanel = Panel("FeedbackNotice", beach, 28, 106, 1120, 62, new Color(.96f, .98f, .92f, .93f));
            feedbackGroup = noticePanel.gameObject.AddComponent<CanvasGroup>(); feedbackGroup.alpha = 0; feedbackGroup.blocksRaycasts = false; feedbackGroup.interactable = false;
            feedbackText = Label("FeedbackText", noticePanel, 18, 0, 1084, 62, "", 28);
            host.Feedback.Notice += ShowFeedback;
            for (int n = 0; n < 6; n++)
            {
                var patch = Panel("WastePatch" + n, beach, 54 + n * 182, 192 + (n % 2) * 46, 130, 88, new Color32(140, 128, 96, 255)).GetComponent<Image>();
                if (art.Waste[n] != null) { patch.sprite = art.Waste[n]; patch.preserveAspect = true; patch.color = Color.white; }
                dirt.Add(patch);
            }
            mode = Label("CenterRole", beach, 200, 290, 790, 64, "", 34); mode.alignment = TextAnchor.MiddleCenter;
            center = Button("CenterAction", beach, 410, 364, 368, 196, "", host.CenterAction, out pile);
            if (art.Pile != null)
            {
                center.GetComponent<Image>().color = new Color(1, 1, 1, .18f);
                pileImage = Panel("PileArtwork", center.transform, 0, 0, 368, 130, Color.white).GetComponent<Image>();
                pileImage.sprite = art.Pile; pileImage.preserveAspect = true;
                pile.rectTransform.anchoredPosition = new Vector2(14, -130);
                pile.rectTransform.sizeDelta = new Vector2(340, 62); pile.color = ink; pile.fontSize = 24;
            }
            for (int i = 0; i < G.State.materials.Length; i++)
            {
                int index = i;
                var button = Button("WasteToken_" + i, beach, 30 + (i % 3) * 388, 574 + (i / 3) * 82, 356, 72, "", () => host.TokenAction(index, displayedGenerations[index]), out var label);
                if (art.Waste[i] != null)
                {
                    button.GetComponent<Image>().color = new Color(1, 1, 1, .82f);
                    var icon = Panel("WasteIcon", button.transform, 4, 4, 64, 64, Color.white).GetComponent<Image>(); icon.sprite = art.Waste[i]; icon.preserveAspect = true;
                    label.rectTransform.anchoredPosition = new Vector2(72, -4); label.rectTransform.sizeDelta = new Vector2(276, 64); label.color = ink; label.fontSize = 24;
                }
                tokens.Add((i, button, label));
            }
            Panel("CounterBackdrop", beach, 12, 686, 1164, 70, new Color(.97f, .98f, .93f, .92f));
            cleanup = Label("Collected", beach, 28, 690, 550, 58, "", 26);
            recycled = Label("Processed", beach, 590, 690, 570, 58, "", 26);
            var facilities = Panel("FacilityPanel", root, 1236, 182, 660, 768, pale);
            string[] facilityNames = { "KÂĞIT MERKEZİ", "PLASTİK MERKEZİ", "METAL MERKEZİ", "CAM MERKEZİ" };
            float top = 0;
            for (int f = 0; f < (host.corePrototype ? 1 : 4); f++)
            {
                int facility = f;
                float height = f < 2 ? 216 : 156;
                var card = Panel("Facility_" + f, facilities, 0, top, 660, height, Color.white);
                facilityCards[f] = card.GetComponent<Image>();
                Label("FacilityName", card, art.Facilities[f] == null ? 16 : 80, 6, art.Facilities[f] == null ? 628 : 564, 40, facilityNames[f], 28);
                if (art.Facilities[f] != null)
                {
                    var icon = Panel("FacilityIcon", card, 8, 2, 64, 44, Color.white).GetComponent<Image>(); icon.sprite = art.Facilities[f]; icon.preserveAspect = true;
                }
                int lane = 0;
                for (int i = 0; i < G.State.materials.Length; i++)
                {
                    if (G.Definitions.FacilityIndex(i) != f) continue;
                    var label = Label("Lane_" + i, card, 16, 45 + lane * 60, 628, 46, "", 24);
                    Panel("Track_" + i, card, 16, 89 + lane * 60, 628, 6, new Color32(216, 230, 224, 255));
                    var bar = Panel("Progress_" + i, card, 16, 89 + lane * 60, 0, 6, teal).GetComponent<Image>();
                    lines.Add((i, label, bar)); lane++;
                }
                AddUpgrade(PurchaseKind.Speed, facility, card, 16, height - 66, 304, 66);
                AddUpgrade(PurchaseKind.Capacity, facility, card, 336, height - 66, 308, 66);
                top += height + 8;
            }
            if (host.corePrototype) Label("PrototypeScope", facilities, 16, 242, 628, 230, "İLK OYNANABİLİR PROTOTİP\n\nBu çıktı tek kâğıt hattını doğrular.\nAltı malzeme / dört tesis tam sürüme eklenecek.\n\nGörseller geçicidir.", 28);
            AddUpgrade(PurchaseKind.Collector, 0, root, 24, 964, 570, 72);
            AddUpgrade(PurchaseKind.Manual, 0, root, 610, 964, 602, 72);
            status = Label("SaveStatus", root, 1236, 958, 660, 114, "", 23);
            Label("InputHint", root, 24, 1036, 1188, 40, "Tıkla / Space: merkez · Tab / Enter: düğmeler · ESC: duraklat", 24);
            overlay = Panel("Overlay", root, 0, 0, 1920, 1080, new Color(0.04f, 0.12f, 0.15f, 0.92f)); overlay.GetComponent<Image>().raycastTarget = true;
            overlay.gameObject.SetActive(false);
            Refresh();
        }
        void AddUpgrade(PurchaseKind kind, int facility, Transform parent, float x, float y, float w, float h)
        {
            var button = Button(kind + "_" + facility, parent, x, y, w, h, "", () => host.Purchase(kind, facility), out var label);
            if (h < 66) label.fontSize = 22;
            upgrades.Add((kind, facility, button, label));
        }
        string Credits(long value) => (value / 100.0).ToString("N1", tr);
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            feedbackRemaining = Mathf.Max(0, feedbackRemaining - dt);
            feedbackGroup.alpha = Mathf.Clamp01(feedbackRemaining / .2f);
            centerPulse = Mathf.Max(0, centerPulse - dt);
            if (pileImage != null) pileImage.rectTransform.localScale = Vector3.one * (G.State.reduceMotion ? 1 : 1 - .06f * centerPulse / .1f);
            for (int i = 0; i < facilityCards.Length; i++)
            {
                facilityFlash[i] = Mathf.Max(0, facilityFlash[i] - dt);
                if (facilityCards[i] != null) facilityCards[i].color = Color.Lerp(Color.white, new Color(.72f, .92f, .8f), facilityFlash[i] / .6f);
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                buttons.RemoveAll(b => b == null);
                int selected = buttons.FindIndex(b => b.gameObject == EventSystem.current.currentSelectedGameObject);
                int direction = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
                for (int n = 1; n <= buttons.Count; n++)
                {
                    int i = (selected + n * direction + buttons.Count * 2) % buttons.Count;
                    var b = buttons[i];
                    if (b.gameObject.activeInHierarchy && b.interactable && (host.Paused == b.transform.IsChildOf(overlay))) { b.Select(); break; }
                }
            }
            if (Input.GetKeyDown(KeyCode.Return) && EventSystem.current.currentSelectedGameObject != null)
            {
                var b = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
                if (b != null && b.gameObject.activeInHierarchy && b.interactable && (host.Paused == b.transform.IsChildOf(overlay))) b.onClick.Invoke();
            }
            renderTimer += Time.unscaledDeltaTime;
            if (dirty && renderTimer >= .1f) { Refresh(); dirty = false; renderTimer = 0; }
            if (G.State.complete && !resultShown) { resultShown = true; ShowResult(); }
        }
        void ShowFeedback(FeedbackNotice notice)
        {
            if (notice.Kind == FeedbackKind.Collect || notice.Kind == FeedbackKind.Assist) centerPulse = .1f;
            if (notice.Facility >= 0 && notice.Facility < facilityCards.Length) facilityFlash[notice.Facility] = .6f;
            int priority = notice.Kind == FeedbackKind.Unlock || notice.Kind == FeedbackKind.Complete ? 3 : notice.Kind == FeedbackKind.Purchase ? 2 : 1;
            if (feedbackRemaining > .5f && priority < feedbackPriority) return;
            feedbackPriority = priority; feedbackRemaining = 1.25f;
            feedbackText.text = notice.Text; feedbackGroup.alpha = 1;
        }
        void Refresh()
        {
            money.text = Credits(G.State.money) + " kredi";
            income.text = G.RecentIncome().ToString("N1", tr) + " kr/sn · son 10 sn ort.";
            target.text = G.State.complete ? "Bölüm tamamlandı." : G.State.finalPhase ? "Sahil toplandı; son partiler işleniyor. İşlem hızı ×4." : G.State.collectorRank == 0 ? "Sonraki hedef: 100 kredi biriktir, toplama ekibini al. Toplamak para vermez; işlemek verir." : NextUnlock();
            if (host.ValidationRun) target.text = "DOĞRULAMA ×20 · Oyuncu kaydından ayrı · " + target.text;
            mode.text = G.State.pileRemaining > 0 ? "TOPLA · " + G.Definitions.materials[G.State.pileMaterial].display_name_tr : "İŞLEMEYE YARDIM ET";
            pile.text = G.State.pileRemaining > 0 ? G.State.pileRemaining + " / " + G.Definitions.sources.pile_units + "\nHam stoka aktar" : G.WorldRemaining == 0 ? "Son işlemlere destek" : "Yeni parti hazırlanıyor\n" + G.State.refillRemaining.ToString("N1", tr) + " sn";
            if (pileImage != null) pileImage.gameObject.SetActive(G.State.pileRemaining > 0);
            center.interactable = !host.Paused && !G.State.complete;
            cleanup.text = "Toplandı: " + G.Collected + "/" + G.TotalUnits + " · %" + (100.0 * G.Collected / G.TotalUnits).ToString("N1", tr);
            recycled.text = "İşlendi: " + G.Processed + "/" + G.TotalUnits;
            foreach (var line in lines)
            {
                var m = G.State.materials[line.i]; var d = G.Definitions.materials[line.i];
                string name = d.id == "hdpe" ? "HDPE ambalaj" : d.id == "pet" ? "PET şişe" : d.id == "glass" ? "Ambalaj camı" : d.display_name_tr;
                line.text.text = !m.unlocked ? name + " · Kilitli: " + d.unlock_processed_total_units + " işlenen" : name + " · Stok " + m.raw + " · " + (m.batch > 0 ? m.batch + " işlemde · " + ((m.batch - m.work) / G.Processing.Rate(line.i)).ToString("N1", tr) + " sn" : "Malzeme bekliyor");
                line.bar.rectTransform.sizeDelta = new Vector2(m.batch > 0 ? (float)(628 * m.work / m.batch) : 0, 6);
            }
            foreach (var token in tokens)
            {
                var m = G.State.materials[token.i];
                token.button.gameObject.SetActive(m.unlocked && m.visible > 0);
                displayedGenerations[token.i] = m.tokenGeneration;
                token.text.text = G.Definitions.materials[token.i].display_name_tr + " · +1 ham";
            }
            foreach (var item in upgrades)
            {
                long cost = G.Cost(item.kind, item.facility); int rank = G.Rank(item.kind, item.facility);
                bool locked = (item.kind == PurchaseKind.Speed || item.kind == PurchaseKind.Capacity) && !G.FacilityUnlocked(item.facility);
                string name = item.kind == PurchaseKind.Collector ? "Toplama ekibi" : item.kind == PurchaseKind.Manual ? "Toplama gücü" : item.kind == PurchaseKind.Speed ? "Hız" : "Kapasite";
                string effect = item.kind == PurchaseKind.Collector ? " · " + G.Definitions.collector.nominal_units_per_second_by_rank[Math.Min(rank + 1, 5)].ToString("N2", tr) + "/sn" : item.kind == PurchaseKind.Manual ? " · " + G.Definitions.manual_collection_upgrade.work_per_click_by_rank[Math.Min(rank + 1, 3)].ToString("N2", tr) + "/tık" : "";
                item.text.text = name + " " + rank + " → " + (locked ? "Kilitli" : cost < 0 ? "Maks." : Credits(cost) + " kr" + effect);
                if (!locked && cost >= 0 && G.State.money < cost) item.text.text += "\n" + Credits(cost - G.State.money) + " kr eksik";
                item.text.fontSize = 24;
                item.button.interactable = !host.Paused && !G.State.complete && !locked && cost >= 0 && G.State.money >= cost;
            }
            double fraction = G.Collected / (double)G.TotalUnits;
            double[] thresholds = { .1, .25, .5, .75, .9, 1 };
            for (int i = 0; i < dirt.Count; i++) dirt[i].gameObject.SetActive(fraction < thresholds[i]);
            status.text = host.Saves.LastMessage;
        }
        string NextUnlock()
        {
            for (int i = 0; i < G.State.materials.Length; i++) if (!G.State.materials[i].unlocked) return "Sonraki ücretsiz hat: " + G.Definitions.materials[i].display_name_tr + " · " + G.Processed + "/" + G.Definitions.materials[i].unlock_processed_total_units + " işlenen";
            return "Bütün hatlar açık. Sahilde kalan: " + G.WorldRemaining + " · Ekip tıklama olmadan çalışır.";
        }
        void ClearOverlay(string title, string explanation)
        {
            buttons.RemoveAll(b => b == null || b.transform.IsChildOf(overlay));
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            overlay.gameObject.SetActive(true); host.Paused = true; MarkDirty();
            Label("OverlayTitle", overlay, 380, 200, 1160, 100, title, 46, Color.white);
            Label("OverlayExplanation", overlay, 380, 310, 1160, 180, explanation, 30, Color.white);
        }
        void Resume() { overlay.gameObject.SetActive(false); host.Paused = false; MarkDirty(); }
        public void ShowInitial(bool loaded, bool recovery)
        {
            if (recovery)
            {
                ClearOverlay("Kayıt kontrolü gerekiyor", host.Saves.LastMessage);
                if (loaded) Button("RestoreBackup", overlay, 380, 520, 560, 90, "Geçerli yedeği geri yükle", () => { host.Saves.ConfirmRecovery(); Resume(); }, out _);
                Button("NewGame", overlay, 970, 520, 570, 90, "Yeni oyun…", ConfirmNewGame, out _);
            }
            else if (loaded)
            {
                ClearOverlay("Kaldığın yerden devam et", "Kayıt geri yüklendi. Oyun kapalıyken üretim hesaplanmaz.");
                Button("Continue", overlay, 380, 520, 560, 90, "Devam et", Resume, out _);
                Button("NewGame", overlay, 970, 520, 570, 90, "Yeni oyun…", ConfirmNewGame, out _);
            }
        }
        public void TogglePause()
        {
            if (host.Saves.NeedsRecovery || G.State.complete) return;
            if (overlay.gameObject.activeSelf) { Resume(); return; }
            ShowPause();
        }
        void ShowPause()
        {
            ClearOverlay("Duraklatıldı", "Ayarlar açıkken oyun durur. Alt-Tab tek başına duraklatmaz.\nOtomatik kayıt: 10 sn. Çökmede son aralık kaybolabilir.");
            Button("Resume", overlay, 380, 520, 560, 80, "Devam et", Resume, out _);
            Button("Mute", overlay, 970, 520, 570, 80, G.State.muted ? "Ses: kapalı" : "Ses: açık", () => { host.SetMuted(!G.State.muted); ShowPause(); }, out _);
            Button("ReducedMotion", overlay, 380, 640, 1160, 80, G.State.reduceMotion ? "Az hareket: açık" : "Az hareket: kapalı", () => { host.SetReducedMotion(!G.State.reduceMotion); ShowPause(); }, out _);
            Button("New", overlay, 380, 760, 560, 80, "Yeni oyun…", ConfirmNewGame, out _);
            Button("Quit", overlay, 970, 760, 570, 80, "Kaydet ve çık", () => Application.Quit(), out _);
        }
        void ConfirmNewGame()
        {
            ClearOverlay("Mevcut ilerleme sıfırlansın mı?", "Yeni oyun, mevcut oyunun yerine başlar. Eski dosya yerel yedek olarak korunur.");
            Button("ConfirmNew", overlay, 380, 520, 560, 90, "Evet, yeni oyun başlat", () => { host.NewGameConfirmed(); resultShown = false; Resume(); }, out _);
            Button("CancelNew", overlay, 970, 520, 570, 90, "Vazgeç", () => { overlay.gameObject.SetActive(false); if (host.Saves.NeedsRecovery) ShowInitial(host.HasLoadedSave, true); else if (G.State.complete) ShowResult(); else Resume(); }, out _);
        }
        void ShowResult()
        {
            ClearOverlay("Sahil tamamlandı", G.TotalUnits + " birim toplandı ve işlendi.\nBrüt ödül: " + Credits(G.State.earned) + " kredi · Oynanan süre: " + TimeSpan.FromSeconds(G.State.elapsed).ToString(@"mm\:ss") + "\n" + (host.corePrototype ? "Bu tek malzemeli ilk prototiptir." : "Dört tesiste altı bağımsız hat tamamlandı."));
            result = Label("Result", overlay, 380, 760, 1160, 70, "Geliştirme sürümü · V" + Application.version, 24, Color.white);
            Button("New", overlay, 380, 560, 560, 90, "Yeni oyun…", ConfirmNewGame, out _);
            Button("Exit", overlay, 970, 560, 570, 90, "Kaydet ve çık", () => Application.Quit(), out _);
        }
        void OnDestroy() { if (host != null && host.Feedback != null) host.Feedback.Notice -= ShowFeedback; }
    }
}
