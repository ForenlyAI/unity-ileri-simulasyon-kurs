# Bitirme işi: kendi kafenizin vardiya raporu — ölçüt çizelgesi

Teslim: kendi kafe yerleşiminiz (masa sayısı ya da yerleri değişmiş), seçtiğiniz bir sıra kuralı ve şarj eşiği, bir saatlik vardiyanın hızlı hesap ve canlı benzetim sonuçları, test raporları ve kısa bir not.

| Ölçüt | Yeterli | Eksik |
|---|---|---|
| 1. Yol | Değiştirdiğiniz yerleşimde bütün nokta çiftleri arasında tam yol var (`Olcumler/mesafeler.json` içinde `eksik: 0`); bir masanın yolunu ve süresini notta yazdınız | Yol tablosunda eksik çift var ya da yerleşim değişmemiş |
| 2. Durum makinesi | Seçtiğiniz sıra kuralı ve şarj eşiğiyle hızlı vardiya koştu; ortalama ve en uzun bekleme, şarj sayısı ve en düşük pil raporda; neden bu kuralı seçtiğiniz bir cümleyle yazılı | Yalnız tek sayı var ya da seçim gerekçesiz |
| 3. Ölçüm | Profiler ile kendi yerleşiminizde en az iki farklı müşteri sayısında kare süresi ölçüldü; yüklenen her Addressables varlığı bırakıldı (paket sayısı sonda 0) | Ölçüm yok ya da paket sayısı sıfıra inmiyor |
| 4. Testler | EditMode ve PlayMode testlerinin hepsi geçti (iki rapor dosyası ekli); canlı vardiya ile hızlı hesap arasındaki farkı bir cümleyle açıkladınız | Kalan test var ya da rapor eksik |

Yapay zekâ kullandıysanız: hangi dosyayı Claude Code'a yazdırdığınızı ve onu nasıl sınadığınızı (örneğin kodu bilerek bozup testin kaldığını göstererek) bir cümleyle yazın.
Oda ölçüleri, müşteri sıklığı ve pil değerleri ÖRNEK'tir; değiştirdiğiniz değerleri de ÖRNEK diye işaretleyin.
