// Unity'siz kural testleri: Unity'deki EditMode testlerinin saf C# kısmı.
// Yol uzunlukları Unity'nin NavMesh'inden alınmış tablodan (beklenen/olcumler/mesafeler.json) okunur;
// böylece hızlı vardiya hesabı Unity olmadan da aynı sonucu vermelidir.
using System.IO;
using System.Linq;
using System.Text.Json;
using KafeIleri;
using NUnit.Framework;

public class KuralTestleri
{
    static MesafeTablosu Tablo()
    {
        var j = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "mesafeler.json")));
        var satirlar = j.RootElement.GetProperty("tablo").EnumerateArray().Select(r => r.EnumerateArray().Select(x => x.GetDouble()).ToArray()).ToArray();
        var m = new double[satirlar.Length, satirlar.Length];
        for (int a = 0; a < satirlar.Length; a++) for (int b = 0; b < satirlar.Length; b++) m[a, b] = satirlar[a][b];
        return new MesafeTablosu(m);
    }

    static JsonElement Beklenen(string dosya) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, dosya))).RootElement;

    [Test]
    public void Durum_Makinesi_Bes_Gecis()
    {
        Assert.AreEqual(4, GorevMakinesi.DurumSayisi);
        Assert.AreEqual(5, GorevMakinesi.GecisSayisi);
        var g = new GorevMakinesi();
        g.Gec(Olay.MusteriGeldi); g.Gec(Olay.SiparisAlindi);
        Assert.IsFalse(g.Gec(Olay.PilAzaldi), "serviste şarj isteği reddedilmeli");
    }

    [Test]
    public void Ayni_Tohum_Ayni_Akis()
    {
        CollectionAssert.AreEqual(MusteriAkisi.Gelisler(7, 150, 3600), MusteriAkisi.Gelisler(7, 150, 3600));
    }

    [Test]
    public void Hizli_Vardiya_Unity_Ile_Ayni()
    {
        var s = HizliBenzetim.Kos(new VardiyaAyar(), Tablo()).sonuc;
        var b = Beklenen("vardiya-hizli.json");
        Assert.AreEqual(b.GetProperty("servis").GetInt32(), s.servis);
        Assert.AreEqual(b.GetProperty("gelen").GetInt32(), s.gelen);
        Assert.AreEqual(b.GetProperty("ort_bekleme_s").GetDouble(), System.Math.Round(s.ortBekleme, 1));
        Assert.AreEqual(b.GetProperty("sarj").GetInt32(), s.sarj);
    }

    [Test]
    public void Yogun_Saatte_En_Yakin_Once()
    {
        var k = Beklenen("kuyruk.json");
        var s = HizliBenzetim.Kos(new VardiyaAyar { politika = Politika.EnYakin, ortalamaGelis = 90 }, Tablo()).sonuc;
        // Yol tablosu JSON'da santimetreye yuvarlı; bu yüzden yoğun saatte ortalama 0,1 sn kayabilir (2026-10-11: 52,5 / 52,4)
        Assert.AreEqual(k.GetProperty("EnYakin_90").GetProperty("ort_bekleme_s").GetDouble(), s.ortBekleme, 0.5);
        Assert.AreEqual(k.GetProperty("EnYakin_90").GetProperty("servis").GetInt32(), s.servis);
    }
}
