// Play Mode: canlı vardiya (Ders 1.3 ve 4.4), Profiler ölçümü (Ders 3.1 ve 3.2), Addressables yükle-bırak (Ders 3.3 ve 3.4).
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace KafeIleri.Testler
{
    public class VardiyaCanliTestleri
    {
        [TearDown] public void Temizle() => Oyun.Kapat();

        [UnityTest]
        public IEnumerator Bir_Saatlik_Canli_Vardiya()
        {
            yield return Oyun.SahneyiAc(20, 4f);   // her kare 0,2 sn benzetim zamanı
            var y = Oyun.Yonetici;
            y.ayar = new VardiyaAyar();
            float enYakinMusteri = 99f, enYakinRobot = 99f;
            var takilan = new List<string>();
            var basla = y.StartCoroutine(y.Baslat());
            int kare = 0;
            while (!y.Bitti && kare < 40000)
            {
                var canli = y.ajanlar.Where(a => a != null).ToList();
                for (int i = 0; i < canli.Count; i++)
                {
                    var p = canli[i].transform.position; p.y = 0;
                    var r = y.robot.transform.position; r.y = 0;
                    enYakinRobot = Mathf.Min(enYakinRobot, Vector3.Distance(p, r));
                    for (int j = i + 1; j < canli.Count; j++)
                    {
                        var q = canli[j].transform.position; q.y = 0;
                        enYakinMusteri = Mathf.Min(enYakinMusteri, Vector3.Distance(p, q));
                    }
                }
                if (kare % 150 == 0)
                    foreach (var a in canli)
                        if (a.kayit.oturma < 0 && y.robot.Simdi - a.kayit.kapi > 40 && takilan.Count < 12)
                            takilan.Add($"m{a.kayit.no} t{y.robot.Simdi:0} ({a.transform.position.x:0.0},{a.transform.position.z:0.0}) hiz {a.ajan.velocity.magnitude:0.00} kalan {a.ajan.remainingDistance:0.00} robot ({y.robot.transform.position.x:0.0},{y.robot.transform.position.z:0.0})");
                kare++;
                yield return null;
            }
            var s = y.sonuc;
            var oturan = y.kafe.musteriler.Where(m => m.oturma >= 0).ToList();
            var tablo = Oyun.Noktalar.YolTablosu();
            double hesapKapiMasa = oturan.Average(m => tablo.Mesafe(Nokta.Kapi, Nokta.Oturma(m.masa, y.noktalar.masaSayisi)) / y.ayar.musteriHizi);
            var veri = s.Sozluk();
            veri["kare"] = kare;
            veri["ort_kapi_masa_s"] = Olcum.Yuvarla(oturan.Average(m => m.oturma - m.kapi), 1);
            veri["hesap_kapi_masa_s"] = Olcum.Yuvarla(hesapKapiMasa, 1);
            veri["en_yakin_iki_musteri_m"] = Olcum.Yuvarla(enYakinMusteri, 2);
            veri["en_yakin_robot_musteri_m"] = Olcum.Yuvarla(enYakinRobot, 2);
            veri["addressables_yukleme_ms"] = Olcum.Yuvarla(y.YuklemeMs, 1);
            veri["musteri_kapi_masa_s"] = oturan.Select(m => Olcum.Yuvarla(m.oturma - m.kapi, 1)).ToList();
            veri["musteri_masa"] = oturan.Select(m => m.masa + 1).ToList();
            veri["takilan"] = takilan;
            veri["gecis_gunlugu"] = y.program.makine.Gunluk.Take(8).ToList();
            Olcum.Yaz("vardiya-canli", veri);
            Assert.IsTrue(y.Bitti);
            Assert.Greater(s.servis, 0);
        }
    }

    public class ProfilerTestleri
    {
        [TearDown] public void Temizle() => Oyun.Kapat();

        static List<NavMeshAgent> Kalabalik(int adet, int tohum)
        {
            var sablon = Oyun.Yonetici.musteriSablonu;
            var r = new System.Random(tohum);
            var liste = new List<NavMeshAgent>();
            for (int i = 0; i < adet; i++)
            {
                var p = KafeNoktalari.Oturt(new Vector3((float)(r.NextDouble() * 10 - 5), 0, (float)(r.NextDouble() * 8 - 4)));
                var g = Object.Instantiate(sablon, p, Quaternion.identity);
                liste.Add(g.GetComponent<NavMeshAgent>());
            }
            return liste;
        }

        static Vector3 RastgeleHedef(System.Random r) =>
            KafeNoktalari.Oturt(new Vector3((float)(r.NextDouble() * 10 - 5), 0, (float)(r.NextDouble() * 8 - 4)));

        // adet müşteri kafede dolaşır; ısınmadan sonra 'olcum' kare ölçülür
        static IEnumerator Olc(int adet, bool herKare, Dictionary<string, object> veri, string anahtar, string kayitDosyasi = null)
        {
            yield return Oyun.SahneyiAc(0);
            Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            MusteriAjan.herKareYol = herKare;
            var r = new System.Random(11);
            var ajanlar = Kalabalik(adet, 3);
            foreach (var a in ajanlar) a.GetComponent<MusteriAjan>().Git(RastgeleHedef(r), Vector3.zero);
            for (int i = 0; i < 60; i++) yield return null;   // ısınma
            if (kayitDosyasi != null) { Profiler.logFile = kayitDosyasi; Profiler.enableBinaryLog = true; Profiler.enabled = true; }
            using var yol = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "KafeIleri.YolHesabi", 400, ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            using var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 400);
            var kareMs = new List<double>(); var yolMs = new List<double>(); var yolAdet = new List<long>(); int sayacOnce = MusteriAjan.YolSayaci; var gcB = new List<long>();
            double onceki = Time.realtimeSinceStartupAsDouble;
            const int olcum = 300;
            for (int i = 0; i < olcum; i++)
            {
                foreach (var a in ajanlar)
                    if (CanliBeden.Vardi(a)) { var m = a.GetComponent<MusteriAjan>(); m.Git(RastgeleHedef(r), Vector3.zero); }
                yield return null;
                double simdi = Time.realtimeSinceStartupAsDouble; kareMs.Add((simdi - onceki) * 1000); onceki = simdi;
                if (yol.Valid && yol.Count > 0) { yolMs.Add(yol.LastValue / 1e6); }
                if (gc.Valid && gc.Count > 0) gcB.Add(gc.LastValue);
                yolAdet.Add(yol.Valid ? yol.Count : 0);
            }
            int cagri = MusteriAjan.YolSayaci - sayacOnce;
            if (kayitDosyasi != null) { Profiler.enabled = false; Profiler.enableBinaryLog = false; Profiler.logFile = ""; }
            kareMs.Sort();
            veri[anahtar] = new Dictionary<string, object>
            {
                { "ajan", adet }, { "her_kare_yol", herKare }, { "kare", olcum },
                { "kare_ms_ortanca", Olcum.Yuvarla(kareMs[kareMs.Count / 2], 2) }, { "kare_ms_95", Olcum.Yuvarla(kareMs[(int)(kareMs.Count * 0.95)], 2) },
                { "yol_cagri_kare", Olcum.Yuvarla(cagri / (double)olcum, 1) }, { "yol_ms_ort", yolMs.Count > 0 ? Olcum.Yuvarla(yolMs.Average(), 3) : -1 },
                { "gc_bayt_ort", gcB.Count > 0 ? Olcum.Yuvarla(gcB.Average(), 0) : -1 },
            };
            MusteriAjan.herKareYol = false;
        }

        [UnityTest]
        public IEnumerator Ajan_Sayisi_Ve_Kare_Suresi()
        {
            var veri = new Dictionary<string, object> { { "butce60_ms", Olcum.Yuvarla(1000.0 / 60, 2) }, { "butce30_ms", Olcum.Yuvarla(1000.0 / 30, 2) } };
            string raw = Path.Combine(Olcum.Klasor, "profil-100-ajan");
            Directory.CreateDirectory(Olcum.Klasor);
            foreach (var n in new[] { 10, 50, 100 })
                yield return Olc(n, false, veri, $"ajan_{n}", n == 100 ? raw : null);
            veri["profil_dosyasi"] = File.Exists(raw + ".raw") ? "profil-100-ajan.raw" : "yok";
            veri["profil_kb"] = File.Exists(raw + ".raw") ? Olcum.Yuvarla(new FileInfo(raw + ".raw").Length / 1024.0, 0) : 0;
            Olcum.Yaz("profil", veri);
        }

        [UnityTest]
        public IEnumerator Her_Kare_Yol_Hesabi_Darbogaz()
        {
            var veri = new Dictionary<string, object>();
            yield return Olc(100, false, veri, "hedef_degisince");
            yield return Olc(100, true, veri, "her_kare");
            Olcum.Yaz("darbogaz", veri);
        }
    }

    public class AddressablesYuklemeTestleri
    {
        [TearDown] public void Temizle() => Oyun.Kapat();

        [UnityTest]
        public IEnumerator Yukle_Cogalt_Birak()
        {
            yield return Oyun.SahneyiAc(30);
            var init = Addressables.InitializeAsync(false);
            yield return init;
            var initDurum = init.Status; Addressables.Release(init);
            int paketOnce = AssetBundle.GetAllLoadedAssetBundles().Count();
            long bellekOnce = Profiler.GetTotalAllocatedMemoryLong();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var h = Addressables.LoadAssetAsync<GameObject>("Musteri");
            yield return h;
            double yukleMs = sw.Elapsed.TotalMilliseconds;
            int paketYuklu = AssetBundle.GetAllLoadedAssetBundles().Count();
            var konumlar = Addressables.LoadResourceLocationsAsync("kafe");
            yield return konumlar;
            int etiketli = konumlar.Result.Count;
            Addressables.Release(konumlar);
            sw.Restart();
            var kopyalar = new List<GameObject>();
            for (int i = 0; i < 20; i++)
            {
                var ih = Addressables.InstantiateAsync("Musteri", new Vector3(-4 + (i % 5) * 2, 0, -3 + (i / 5) * 1.5f), Quaternion.identity);
                yield return ih;
                kopyalar.Add(ih.Result);
            }
            double cogaltMs = sw.Elapsed.TotalMilliseconds;
            long bellekYuklu = Profiler.GetTotalAllocatedMemoryLong();
            foreach (var k in kopyalar) Addressables.ReleaseInstance(k);
            Addressables.Release(h);
            for (int i = 0; i < 10; i++) yield return null;
            int paketSonra = AssetBundle.GetAllLoadedAssetBundles().Count();
            Olcum.Yaz("addr-yukleme", new Dictionary<string, object>
            {
                { "etiketli", etiketli }, { "yukle_ms", Olcum.Yuvarla(yukleMs, 1) }, { "kopya", kopyalar.Count }, { "cogalt_ms", Olcum.Yuvarla(cogaltMs, 1) },
                { "paket_once", paketOnce }, { "paket_yuklu", paketYuklu }, { "paket_sonra", paketSonra },
                { "bellek_artis_kb", Olcum.Yuvarla((bellekYuklu - bellekOnce) / 1024.0, 0) },
                { "tutamac_gecerli", h.IsValid() },
            });
            Assert.AreEqual(AsyncOperationStatus.Succeeded, initDurum);
            Assert.IsFalse(h.IsValid());
        }
    }
}
