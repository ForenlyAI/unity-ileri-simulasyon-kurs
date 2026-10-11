// Edit Mode: saf C# kuralları (durum makinesi, pil, müşteri akışı). Unity sahnesi gerekmez.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace KafeIleri.Testler
{
    public class GorevMakinesiTestleri
    {
        [Test]
        public void Bir_Siparis_Dongusu_Bekleye_Doner()
        {
            var g = new GorevMakinesi();
            Assert.IsTrue(g.Gec(Olay.MusteriGeldi));
            Assert.IsTrue(g.Gec(Olay.SiparisAlindi));
            Assert.IsTrue(g.Gec(Olay.ServisBitti));
            Assert.AreEqual(Durum.Bekle, g.Simdiki);
        }

        [Test]
        public void Serviste_Sarj_Istegi_Reddedilir()
        {
            var g = new GorevMakinesi();
            g.Gec(Olay.MusteriGeldi); g.Gec(Olay.SiparisAlindi);
            Assert.IsFalse(g.Gec(Olay.PilAzaldi));
            Assert.AreEqual(Durum.ServisYap, g.Simdiki);
        }

        [Test]
        public void Sarjda_Musteri_Reddedilir()
        {
            var g = new GorevMakinesi();
            g.Gec(Olay.PilAzaldi);
            Assert.IsFalse(g.Gec(Olay.MusteriGeldi));
            Assert.IsTrue(g.Gec(Olay.PilDoldu));
            Assert.AreEqual(Durum.Bekle, g.Simdiki);
        }

        [Test]
        public void Gecis_Tablosu_Olcumu()
        {
            var g = new GorevMakinesi();
            var istekler = new[] { Olay.MusteriGeldi, Olay.PilAzaldi, Olay.SiparisAlindi, Olay.PilAzaldi, Olay.ServisBitti, Olay.PilAzaldi, Olay.MusteriGeldi, Olay.PilDoldu };
            foreach (var o in istekler) g.Gec(o);
            Olcum.Yaz("durum", new Dictionary<string, object>
            {
                { "durum_sayisi", GorevMakinesi.DurumSayisi }, { "gecis_sayisi", GorevMakinesi.GecisSayisi },
                { "tablo", GorevMakinesi.Tablo().Select(t => $"{t.Item1} > {t.Item2} > {t.Item3}").ToList() },
                { "olasi_istek", GorevMakinesi.DurumSayisi * System.Enum.GetValues(typeof(Olay)).Length },
                { "istek", istekler.Length }, { "kabul", g.Kabul }, { "red", g.Red }, { "adimlar", g.Gunluk }, { "son", g.Simdiki.ToString() },
            });
            Assert.AreEqual(5, GorevMakinesi.GecisSayisi);
        }
    }

    public class PilTestleri
    {
        [Test]
        public void Esik_Ve_Dolma_Suresi()
        {
            var a = new PilAyar();
            var p = new Pil(a);
            Assert.IsFalse(p.Azaldi);
            double yuru = (a.baslangic - a.esik) / a.yuruTuketim;
            p.Yuru(yuru);
            Assert.IsTrue(p.Azaldi);
            double dolma = p.DolmaSuresi;
            p.Sarj(dolma);
            Assert.IsTrue(p.Doldu);
            Olcum.Yaz("pil", new Dictionary<string, object>
            {
                { "baslangic", a.baslangic }, { "esik", a.esik }, { "hedef", a.hedef }, { "yuru_tuketim", a.yuruTuketim },
                { "bekle_tuketim", a.bekleTuketim }, { "sarj_hizi", a.sarjHizi },
                { "esige_yuruyus_s", Olcum.Yuvarla(yuru, 0) }, { "dolma_s", Olcum.Yuvarla(dolma, 0) },
                { "tam_yuruyus_dk", Olcum.Yuvarla(100 / a.yuruTuketim / 60, 1) },
            });
        }

        [Test]
        public void Pil_Sifirin_Altina_Inmez()
        {
            var p = new Pil(new PilAyar { baslangic = 1 });
            p.Yuru(1000);
            Assert.AreEqual(0, p.Yuzde);
            Assert.IsTrue(p.Bitti);
        }
    }

    public class MusteriAkisiTestleri
    {
        [Test]
        public void Ayni_Tohum_Ayni_Akis()
        {
            var a = MusteriAkisi.Gelisler(7, 150, 3600);
            var b = MusteriAkisi.Gelisler(7, 150, 3600);
            CollectionAssert.AreEqual(a, b);
            var c = MusteriAkisi.Gelisler(8, 150, 3600);
            CollectionAssert.AreNotEqual(a, c);
            var uzun = MusteriAkisi.Gelisler(7, 150, 360000);
            var aralik = uzun.Zip(uzun.Skip(1), (x, y) => y - x).Average();
            var d = new VardiyaAyar();
            var on = MusteriAkisi.Gelisler(d.tohum, d.ortalamaGelis, 600);
            Olcum.Yaz("akis", new Dictionary<string, object>
            {
                { "tohum", 7 }, { "ortalama", 150 }, { "saat_gelis", a.Count }, { "tohum8_gelis", c.Count },
                { "ilk_bes", a.Take(5).Select(x => Olcum.Yuvarla(x, 0)).ToList() },
                { "yuz_saat_gelis", uzun.Count }, { "yuz_saat_ort_aralik", Olcum.Yuvarla(aralik, 1) },
                { "on_dk_gelis", on.Count },
            });
            Assert.AreEqual(a.Count, b.Count);
        }
    }
}
