# Kıyı Döngüsü

Unity 2D Windows geliştirme prototipi. Çalışma adı geçicidir.

## Projeyi açma

- Unity Hub ve **Unity 6000.5.5f1** kullanılır. Windows kapanışındaki UUM-146676 düzeltmesi nedeniyle bu sürüm doğrulandı.
- Unity Hub'dan deponun içindeki **KiyiDongusu/** klasörünü ekle.
- İlk import işlemleri bitince **Assets/_Game/Scenes/Game.unity** sahnesini açıp Play'e bas.
- Aynı projeyi iki Editor örneğiyle açma; paketleri veya Editor sürümünü kendiliğinden yükseltme.

## Windows build

Windows Build Support modülü kuruluyken Editor'deki **Kiyi Dongusu → Build Art Preview Windows** menüsünü kullan. Alternatif olarak, Editor kapalıyken depo kökünde:

```powershell
.\Tools\Build-Windows.ps1 -Profile Art
```

Betikteki Editor yolu yerel Unity Hub kurulumuna göre değiştirilebilir. Çıktı **Builds/** altına yazılır. Oyunu çalıştırırken exe ve bütün yan dosyaları birlikte tut.
Farklı kurulum yeri için `-EditorPath 'C:\...\Editor\Unity.exe'` verilebilir.

Development ve hızlandırılmış test kodunu içermeyen aday için **Build Release Candidate Windows** menüsü veya `-Profile Release` kullanılır. Aday çıktısı `-ReleaseCandidate` klasörüne yazılır; final kabulü ayrıca yapılır.

## Kontroller

Sol tık / Space: merkez etkileşimi. Tab / Enter: düğmeler. Esc: duraklatma.
Yeni oyunda önce sahildeki altı kâğıdı tıkla; ardından ilk yığın gelir. Malzemeler hatları açıldıkça görünür.
Krediyle alınan uygun konteynere tekil atığı sürükle: işleme tamamlandığında ek kredi. Konteynerler isteğe bağlıdır. Ayarlardan İngilizce/Türkçe, dört ses kanalı, çözünürlük, tam ekran ve azaltılmış hareket seçilir. Varsayılan oyun dili İngilizcedir.
Yükseltmenin etkisini görmek için üzerine gel veya Tab ile odaklan. Tekil atıkların rastgele konumları kayıtta korunur; sürükleme sırasında derinliğe göre boyut değişir.
Mira kısa konuşmalarla mekanikleri anlatır. Quest log sekiz sahil görevini ve kaynaklı bilgi notlarını gösterir; görevler ekstra para üretmez. Rehber ayarlardan kapatılabilir.
Kayıt otomatik tutulur; Yeni Oyun mevcut ilerlemeyi değiştirmeden önce onay ister.

## Depo kapsamı

Bu depo **private** tutulur. Tasarım belgeleri, ajan talimatları, promptlar, konsept denemeleri, test günlükleri, yerel kayıtlar, build çıktıları ve önbellekler yerelde kalır. Üçüncü taraf lisans/atıf dosyaları korunur.

Yerel push kontrolü:

```powershell
.\Tools\Check-RepositoryScope.ps1
git config core.hooksPath .githooks
```

Hook, yalnız son dosya ağacını değil, gönderilecek commit geçmişini de kontrol eder. İç belgeleri içeren eski yerel dalları pushlamayı engeller.

Üçüncü taraf bildirimleri: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Atıflar: [CREDITS.txt](CREDITS.txt).
