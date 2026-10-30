using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KiyiDongusu
{
    [Serializable] public sealed class SaveEnvelope { public int format = 1; public string payload, sha256; }
    public sealed class SaveService
    {
        public string Path { get; }
        public string LastMessage { get; private set; } = "Otomatik kayıt her 10 sn. Çökmede son aralık kaybolabilir.";
        public bool NeedsRecovery { get; private set; }
        public bool CanWrite { get; private set; }
        readonly GameDefinitions definitions;
        readonly bool core;
        public SaveService(string directory, GameDefinitions definitions, bool core)
        {
            this.definitions = definitions; this.core = core;
            Path = System.IO.Path.Combine(directory, core ? "core-v1.json" : "beach-v1.json");
        }
        static string Hash(string text)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        GameState Read(string path)
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path, Encoding.UTF8));
            if (envelope == null || envelope.format != 1 || string.IsNullOrEmpty(envelope.payload) || envelope.sha256 != Hash(envelope.payload)) throw new InvalidDataException("Kayıt bütünlük kontrolü başarısız.");
            var state = JsonUtility.FromJson<GameState>(envelope.payload);
            if (state == null || state.corePrototype != core) throw new InvalidDataException("Kayıt profili uyumsuz.");
            new GameSession(definitions, core, state).Validate();
            return state;
        }
        public GameState Load()
        {
            CanWrite = false; NeedsRecovery = false;
            if (!File.Exists(Path) && !File.Exists(Path + ".bak")) { CanWrite = true; return null; }
            try { var state = Read(Path); CanWrite = true; LastMessage = "Kayıt yüklendi. Kapalı geçen süre üretime eklenmedi."; return state; }
            catch (Exception error)
            {
                Debug.LogWarning("Ana kayıt okunamadı: " + error.Message);
                try
                {
                    var backup = Read(Path + ".bak"); NeedsRecovery = true;
                    LastMessage = "Ana kayıt okunamıyor. Geçerli yedek bulundu; geri yükleme onayı bekleniyor.";
                    return backup;
                }
                catch (Exception backupError)
                {
                    LastMessage = "Kayıt ve yedek açılamadı. Mevcut dosyalar korunuyor. Yeni oyun için açık onay gerekli.";
                    Debug.LogWarning("Yedek okunamadı: " + backupError.Message); NeedsRecovery = true; return null;
                }
            }
        }
        public void ConfirmRecovery()
        {
            if (File.Exists(Path)) File.Copy(Path, Path + ".preserved-" + Guid.NewGuid().ToString("N"));
            // Copy to a fresh temp and replace without rotating a corrupt primary over a valid backup.
            if (File.Exists(Path + ".bak"))
            {
                Read(Path + ".bak");
                File.Copy(Path + ".bak", Path + ".tmp", true);
                if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, null); else File.Move(Path + ".tmp", Path);
            }
            CanWrite = true; NeedsRecovery = false;
        }
        public void ConfirmNewGame()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string suffix = ".preserved-" + Guid.NewGuid().ToString("N");
            if (File.Exists(Path)) File.Move(Path, Path + suffix);
            if (File.Exists(Path + ".bak")) File.Move(Path + ".bak", Path + ".bak" + suffix);
            CanWrite = true; NeedsRecovery = false;
        }
        public bool Save(GameSession game)
        {
            if (!CanWrite) return false;
            try
            {
                game.Validate();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                string payload = JsonUtility.ToJson(game.State);
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = Hash(payload) }));
                using (var stream = new FileStream(Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, Path + ".bak"); else File.Move(Path + ".tmp", Path);
                LastMessage = "Kaydedildi. Kapalıyken üretim yok. Çökmede en çok son 10 sn kaybolabilir.";
                return true;
            }
            catch (Exception error) { LastMessage = "Kayıt HATASI: " + error.Message; Debug.LogError(LastMessage); return false; }
        }
    }
}
