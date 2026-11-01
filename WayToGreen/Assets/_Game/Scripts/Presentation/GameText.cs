using System.Globalization;
namespace KiyiDongusu
{
    public static class GameText
    {
        public static string Language = "en";
        public static CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "tr" ? "tr-TR" : "en-US");
        public static string T(string en, string tr) => Language == "tr" ? tr : en;
        public static string Material(int i)
        {
            string[] en={"Paper","Cardboard","PET bottles","HDPE plastic","Aluminium","Glass"};
            string[] tr={"Kâğıt","Karton","PET şişe","HDPE plastik","Alüminyum","Cam"};
            return Language=="tr"?tr[i]:en[i];
        }
        public static string Facility(int i)
        {
            string[] en={"Paper mill","Plastic workshop","Metal works","Glass studio"};
            string[] tr={"Kâğıt merkezi","Plastik merkezi","Metal merkezi","Cam merkezi"};
            return Language=="tr"?tr[i]:en[i];
        }
        public static string SaveMessage(string message)
        {
            if(Language=="tr")return message;
            if(message.StartsWith("Kayıt HATASI"))return "Save failed. Check storage and write permissions.";
            if(message.StartsWith("Ana kayıt okunamıyor"))return "The main save is damaged. A valid backup is available. Restore it to continue.";
            if(message.StartsWith("Kayıt ve yedek"))return "Neither save file could be opened. Both files are preserved. Starting over requires confirmation.";
            if(message.StartsWith("Geçerli yedek"))return "Backup restored. No offline production was added.";
            if(message.StartsWith("Kayıt yüklendi"))return "Progress restored. No offline production.";
            if(message.StartsWith("Kaydedildi"))return "Saved locally. No offline production.";
            return "Autosaves every 10s. A crash may lose the latest interval.";
        }
    }
}
