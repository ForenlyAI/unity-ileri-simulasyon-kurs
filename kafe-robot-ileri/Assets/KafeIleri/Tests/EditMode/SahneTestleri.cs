// Edit Mode: sahne, NavMesh, hızlı vardiya hesabı ve Addressables. Sahne dosyası açılır, NavMesh sorgulanır.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KafeIleri.EditorAraclari;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace KafeIleri.Testler
{
    public static class SahneYardimcisi
    {
        public static KafeNoktalari Ac()
        {
            EditorSceneManager.OpenScene(SahneKurucu.SahneYolu, OpenSceneMode.Single);
            return Object.FindFirstObjectByType<KafeNoktalari>();
        }
        public static string[] Adlar(KafeNoktalari n) => n.noktalar.Select(t => t.name).ToArray();
    }

    public class NavMeshTestleri
    {
        [Test]
        public void NavMesh_Ozeti()
        {
            SahneYardimcisi.Ac();
            var tri = NavMesh.CalculateTriangulation();
            double alan = 0;
            for (int i = 0; i < tri.indices.Length; i += 3)
            {
                Vector3 a = tri.vertices[tri.indices[i]], b = tri.vertices[tri.indices[i + 1]], c = tri.vertices[tri.indices[i + 2]];
                alan += Vector3.Cross(b - a, c - a).magnitude / 2;
            }
            var ayar = NavMesh.GetSettingsByID(0);
            Olcum.Yaz("navmesh", new Dictionary<string, object>
            {
                { "kose", tri.vertices.Length }, { "ucgen", tri.indices.Length / 3 }, { "yurunur_alan_m2", Olcum.Yuvarla(alan, 1) },
                { "oda_alan_m2", 12 * 10 }, { "ajan_yaricap", ayar.agentRadius }, { "ajan_boy", ayar.agentHeight },
                { "ajan_turu", NavMesh.GetSettingsNameFromID(ayar.agentTypeID) }, { "basamak", ayar.agentClimb }, { "egim", ayar.agentSlope },
            });
            Assert.Greater(tri.indices.Length, 0);
        }

        [Test]
        public void Yol_Tablosu_Tam()
        {
            var n = SahneYardimcisi.Ac();
            var t = n.YolTablosu();
            var adlar = SahneYardimcisi.Adlar(n);
            int eksik = 0;
            for (int a = 0; a < t.Boyut; a++) for (int b = 0; b < t.Boyut; b++) if (a != b && t.m[a, b] < 0) eksik++;
            var satirlar = new List<object>();
            for (int a = 0; a < t.Boyut; a++)
            {
                var s = new List<object>();
                for (int b = 0; b < t.Boyut; b++) s.Add(Olcum.Yuvarla(t.m[a, b], 2));
                satirlar.Add(s);
            }
            // Ders 1.1 örneği: bekleme yerinden en uzak masanın servis noktasına
            int hedef = Nokta.Servis(2);
            var yol = new NavMeshPath();
            NavMesh.CalculatePath(KafeNoktalari.Oturt(n.Konum(Nokta.Ev)), KafeNoktalari.Oturt(n.Konum(hedef)), NavMesh.AllAreas, yol);
            float kus = Vector3.Distance(n.Konum(Nokta.Ev), n.Konum(hedef));
            var hiz = new VardiyaAyar().robotHizi;
            // ikinci örnek: kapıdan arka sıradaki masanın oturma noktasına (müşterinin yolu)
            int h2 = Nokta.Oturma(3, n.masaSayisi);
            var yol2 = new NavMeshPath();
            NavMesh.CalculatePath(KafeNoktalari.Oturt(n.Konum(Nokta.Kapi)), KafeNoktalari.Oturt(n.Konum(h2)), NavMesh.AllAreas, yol2);
            Olcum.Yaz("mesafeler", new Dictionary<string, object>
            {
                { "nokta", adlar.Length }, { "cift", adlar.Length * (adlar.Length - 1) }, { "eksik", eksik }, { "adlar", adlar.ToList() }, { "tablo", satirlar },
                { "ornek_hedef", adlar[hedef] }, { "ornek_yol_m", Olcum.Yuvarla(KafeNoktalari.YolUzunlugu(yol), 2) }, { "ornek_kus_m", Olcum.Yuvarla(kus, 2) },
                { "ornek_kose", yol.corners.Length }, { "ornek_sure_s", Olcum.Yuvarla(KafeNoktalari.YolUzunlugu(yol) / hiz, 1) }, { "robot_hizi", hiz },
                { "ornek2_hedef", adlar[h2] }, { "ornek2_yol_m", Olcum.Yuvarla(KafeNoktalari.YolUzunlugu(yol2), 2) },
                { "ornek2_kus_m", Olcum.Yuvarla(Vector3.Distance(n.Konum(Nokta.Kapi), n.Konum(h2)), 2) }, { "ornek2_kose", yol2.corners.Length },
                { "en_uzun_m", Olcum.Yuvarla(Enumerable.Range(0, t.Boyut).SelectMany(a => Enumerable.Range(0, t.Boyut).Select(b => t.m[a, b])).Max(), 2) },
            });
            Assert.AreEqual(0, eksik, "yolu olmayan nokta çifti var");
        }
    }

    public class VardiyaTestleri
    {
        static MesafeTablosu tablo;
        static MesafeTablosu Tablo() => tablo ??= SahneYardimcisi.Ac().YolTablosu();

        static Dictionary<string, object> Kos(VardiyaAyar a) => HizliBenzetim.Kos(a, Tablo()).sonuc.Sozluk();

        [Test]
        public void Ayni_Ayar_Ayni_Sonuc()
        {
            var x = HizliBenzetim.Kos(new VardiyaAyar(), Tablo()).sonuc;
            var y = HizliBenzetim.Kos(new VardiyaAyar(), Tablo()).sonuc;
            Assert.AreEqual(x.servis, y.servis);
            Assert.AreEqual(x.ortBekleme, y.ortBekleme);
        }

        [Test]
        public void Siparis_Sirasi_Politikalari()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var temel = HizliBenzetim.Kos(new VardiyaAyar(), Tablo());
            double ms = sw.Elapsed.TotalMilliseconds;
            var veri = new Dictionary<string, object> { { "hesap_ms", Olcum.Yuvarla(ms, 1) }, { "durum_gunluk_ilk", temel.program.makine.Gunluk.Take(6).ToList() } };
            foreach (var gelis in new[] { 150.0, 90.0 })
                foreach (var p in new[] { Politika.SiraIle, Politika.EnYakin })
                    veri[$"{p}_{gelis:0}"] = Kos(new VardiyaAyar { politika = p, ortalamaGelis = gelis });
            Olcum.Yaz("kuyruk", veri);
            Assert.Greater(temel.sonuc.servis, 0);
        }

        [Test]
        public void Sarj_Esikleri()
        {
            var veri = new Dictionary<string, object>();
            foreach (var esik in new[] { 5.0, 15.0, 30.0, 45.0 })
                veri[$"esik_{esik:0}"] = Kos(new VardiyaAyar { pil = new PilAyar { esik = esik } });
            Olcum.Yaz("sarj", veri);
            var dusuk = HizliBenzetim.Kos(new VardiyaAyar { pil = new PilAyar { esik = 30 } }, Tablo()).sonuc;
            Assert.IsFalse(dusuk.pilBitti);
        }

        [Test]
        public void Bir_Saatlik_Hizli_Hesap()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var v = Kos(new VardiyaAyar());
            v["hesap_ms"] = Olcum.Yuvarla(sw.Elapsed.TotalMilliseconds, 1);
            Olcum.Yaz("vardiya-hizli", v);
            Assert.Pass();
        }
    }

    public class AddressablesTestleri
    {
        [Test]
        public void Grup_Ve_Derleme()
        {
            var ayar = SahneKurucu.AddressablesKur();
            var grup = ayar.FindGroup(SahneKurucu.GrupAdi);
            Assert.IsNotNull(grup);
            var girdiler = grup.entries.ToList();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult sonuc);
            double sn = sw.Elapsed.TotalSeconds;
            Assert.IsTrue(string.IsNullOrEmpty(sonuc.Error), sonuc.Error);
            var paketler = sonuc.AssetBundleBuildResults.Select(r => new FileInfo(r.FilePath)).ToList();
            Olcum.Yaz("addressables", new Dictionary<string, object>
            {
                { "grup", grup.Name }, { "girdi", girdiler.Count }, { "adresler", girdiler.Select(e => e.address).OrderBy(x => x).ToList() },
                { "etiket", girdiler.All(e => e.labels.Contains("kafe")) }, { "paket", paketler.Count },
                { "paket_adlari", paketler.Select(f => f.Name.Split('_')[0] + "…" + f.Extension).OrderBy(x => x).ToList() },
                { "paket_kb", paketler.Select(f => Olcum.Yuvarla(f.Length / 1024.0, 1)).OrderBy(x => x).ToList() },
                { "toplam_kb", Olcum.Yuvarla(paketler.Sum(f => f.Length) / 1024.0, 1) }, { "derleme_s", Olcum.Yuvarla(sn, 1) },
                { "paketleme", grup.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>().BundleMode.ToString() },
            });
        }
    }
}
