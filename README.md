# Unity'de İnsansı Robot Vardiyası — kurs dosyaları

Kafede kendi başına çalışan insansı robotun bir saatlik vardiyası: robot NavMesh ile yol bulur, sipariş alır, servis yapar, pili azalınca şarja gider; müşteriler kapıdan gelir, masalarına yürür. Bu depoda gerçek bir **Unity projesi** (`kafe-robot-ileri/`), Unity'siz koşan kural testleri (`kurallar-dotnet/`) ve kurs hazırlanırken alınan beklenen sonuçlar (`beklenen/`) var.

| Araç | Kursta kullanılan sürüm |
|---|---|
| Unity Editor | Unity 6.3 LTS (6000.3.26f1), Unity Personal |
| Şablon | Universal 3D (URP 17.3.0) |
| Paketler | AI Navigation 2.0.15 · Addressables 2.10.3 · Unity Test Framework 1.6.0 |
| Unity CLI | 1.0.0-beta.13 (beta) · Pipeline paketi com.unity.pipeline 0.8.0-exp.1 (deneysel, "önizleme") |

Robotun yürüme hızı (0,41 m/sn) insansı robotun kafede çay servisi **benzetim kaydından** gelir; gerçek robot ölçümü değildir. Oda ve masa ölçüleri, müşteri sıklığı, sipariş ve hazırlama süreleri, pil değerleri **ÖRNEK**'tir.

## 1. Kurulum

1. **Unity CLI**: `curl -fsSL https://unity.com/install.sh | bash` (Windows: `irm https://unity.com/install.ps1 | iex`), sonra `unity --version`.
2. **Giriş ve lisans**: `unity auth login`, sonra `unity license` (listede Unity Personal görünmeli).
3. **Unity 6.3 LTS + Web modülü**: `unity install 6000.3.26f1 --module webgl`.
4. **Projeyi bir kez elle açın**: `unity open kafe-robot-ileri`. İlk açılış paketleri indirir ve `Library/` klasörünü kurar.

## 2. Claude Code'u Unity'ye bağlama

Yapay zekâ ajanları Unity'ye yalnız Unity'nin kendi araçlarıyla bağlanır (Unity Hizmet Koşulları); editör penceresini tıklatan araç kullanılmaz.
`unity skill install claude-code` Unity CLI rehberini Claude Code'a kurar. Kursta Claude Code'a verilen istekler ve aldığımız yanıtlar `claude-code/` klasöründe.

## 3. Sahneyi kurma ve testler

```bash
cd kafe-robot-ileri
unity run . -- -executeMethod KafeIleri.EditorAraclari.SahneKurucu.Kur        # sahne, NavMesh, prefablar, Addressables grubu
unity test . --mode EditMode --output ../editmode.xml                            # 20 test (Addressables grubunu da derler)
unity test . --mode PlayMode --output ../playmode.xml                            # 7 test + 2 görüntü testi (yalnız adıyla)
cd .. && python3 kontrol.py kafe-robot-ileri/Olcumler                            # sonuçlarınız beklenenle aynı mı
```

PlayMode testlerini **editör kapalıyken** koşun: editör penceresi odakta değilken oyun modu testi takılabilir. EditMode testleri `Addressables` grubunu derler; PlayMode testleri bu derlenmiş paketlerden yükler, bu yüzden sıra önemli.
Kare süresi, milisaniye ve bellek gibi makineye bağlı sayılar karşılaştırılmaz; geri kalan her sayı aynı çıkmalıdır.

Profiler kaydı: PlayMode testi `kafe-robot-ileri/Olcumler/profil-100-ajan.raw` dosyasını yazar; Profiler penceresinde "Load" ile açılır.

## 4. Unity'siz katman (Codespaces)

Kural sınıfları (`Assets/KafeIleri/Scripts/Kurallar`) Unity'ye bağlı değildir. `kurallar-dotnet/` aynı dosyaları sade bir C# test projesinde derler:

```bash
dotnet test kurallar-dotnet
python3 kontrol.py
```

Bu depo GitHub Codespaces'te açıldığında (`.devcontainer`) bu iki komut Unity kurmadan koşar. `.github/workflows/kurallar.yml` aynı adımları her gönderimde koşar.

## 5. Unity'nin CI iş akışı ve Web derlemesi

`ci/unity-ci.yml`, `unity ci init --provider github --target WebGL --dry-run` çıktısıdır (15 adım). Bu iş akışı ücretli lisansın seri numarasını ister; dosyanın kendi notuna göre Unity Personal bu ortamda açılmaz. Bu yüzden depoda etkin değildir; aynı adımları kendi makinenizde koşun:

```bash
unity build . --create-profile WebGL                                 # bir kez
unity build . --profile WebGL --output-path ../kafe-robot-ileri-web   # kursta 27 dakika sürdü
cd ../kafe-robot-ileri-web && python3 -m http.server 8000             # tarayıcıda http://127.0.0.1:8000
```

## 6. Klasörler

- `kafe-robot-ileri/Assets/KafeIleri/Scripts/Kurallar/` — saf C#: durum makinesi, pil, müşteri akışı, kafe modeli, robot programı, hızlı vardiya hesabı
- `kafe-robot-ileri/Assets/KafeIleri/Scripts/Bilesenler/` — Unity bileşenleri: nokta tablosu ve NavMesh yolları, robotun canlı bedeni, müşteri ajanı, vardiya yöneticisi
- `kafe-robot-ileri/Assets/KafeIleri/Editor/SahneKurucu.cs` — sahneyi, NavMesh'i, prefabları ve Addressables grubunu kuran editör betiği
- `kafe-robot-ileri/Assets/KafeIleri/Tests/` — EditMode ve PlayMode testleri (Claude Code'un yazdıkları `Claude…Testleri.cs`)
- `beklenen/` — test raporları, ölçüm dosyaları, `editor/` altında editör penceresi görüntüleri
- `RUBRIK.md` — bitirme işinin ölçüt çizelgesi
