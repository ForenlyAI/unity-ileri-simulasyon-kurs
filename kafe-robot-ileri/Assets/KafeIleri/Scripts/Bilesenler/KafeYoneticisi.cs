// Ders 4.4 · Canlı vardiya: robot programı CanliBeden ile, müşteriler MusteriAjan ile Play modunda yürür.
// Müşteri şablonu Addressables'tan yüklenir (Ders 3.3). Ekrandaki yazı OnGUI ile çizilir.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace KafeIleri
{
    public class KafeYoneticisi : MonoBehaviour
    {
        public VardiyaAyar ayar = new VardiyaAyar();
        public KafeNoktalari noktalar;
        public CanliBeden robot;
        public GameObject musteriSablonu;
        public string musteriAdresi = "Musteri";
        public bool addressablesKullan = true;
        public bool otomatikBasla = true;
        public float zamanCarpani = 1f;
        public static bool otomatikKapali;   // testler vardiyayı kendisi başlatır

        public RobotProgrami program;
        public KafeModeli kafe;
        public VardiyaSonucu sonuc;
        public bool Bitti { get; private set; }
        public readonly List<MusteriAjan> ajanlar = new List<MusteriAjan>();
        public double YuklemeMs { get; private set; }
        AsyncOperationHandle<GameObject> sablon;

        IEnumerator Start()
        {
            if (!otomatikBasla || otomatikKapali) yield break;
            if (zamanCarpani != 1f) Time.timeScale = zamanCarpani;   // Web derlemesinde vardiya hızlı izlensin
            yield return Baslat();
        }

        public IEnumerator Baslat()
        {
            if (addressablesKullan)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                sablon = Addressables.LoadAssetAsync<GameObject>(musteriAdresi);
                yield return sablon;
                YuklemeMs = sw.Elapsed.TotalMilliseconds;
                musteriSablonu = sablon.Result;
            }
            var tablo = noktalar.YolTablosu();
            kafe = new KafeModeli(noktalar.masaSayisi, ayar);
            kafe.KapidaBelirdi += Olustur;
            program = new RobotProgrami(ayar, kafe, tablo);
            program.makine.Degisti += (eski, yeni) => robot.GorunenDurum = yeni;
            robot.SaatiSifirla();
            yield return program.Calis(robot);
            sonuc = VardiyaSonucu.Hesapla(program, robot);
            Bitti = true;
        }

        void Olustur(Musteri m)
        {
            var kapi = noktalar.Konum(Nokta.Kapi);
            var g = Instantiate(musteriSablonu, kapi, Quaternion.identity);
            g.name = $"Musteri-{m.no}";
            var a = g.GetComponent<MusteriAjan>();
            a.kayit = m;
            a.Vardi = x => { if (x.kayit.oturma < 0) kafe.Oturdu(x.kayit, robot.Simdi); };
            a.Git(noktalar.Konum(Nokta.Oturma(m.masa, noktalar.masaSayisi)), kapi);
            ajanlar.Add(a);
        }

        void Update()
        {
            if (kafe == null || Bitti) return;
            kafe.Ilerle(robot.Simdi);
            foreach (var a in ajanlar)
                if (a != null && a.kayit.kalkis >= 0 && robot.Simdi >= a.kayit.kalkis && a.kayit.oturma >= 0 && a.hedef != noktalar.Konum(Nokta.Kapi))
                    a.Kalk();
        }

        void OnGUI()
        {
            if (program == null) return;
            var s = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(14, Screen.height / 40), alignment = TextAnchor.UpperLeft };
            int servis = 0, sira = 0;
            foreach (var m in kafe.musteriler) { if (m.servis >= 0) servis++; else if (!m.kacti && m.oturma >= 0 && !m.alindi) sira++; }
            var t = System.TimeSpan.FromSeconds(robot.Simdi);
            GUI.Box(new Rect(10, 10, s.fontSize * 17, s.fontSize * 6.2f),
                $"Saat {t.Minutes:00}:{t.Seconds:00}\nDurum: {program.makine.Simdiki}\nPil: %{program.pil.Yuzde:0}\nServis edilen: {servis}\nSırada: {sira}", s);
        }

        void OnDestroy()
        {
            if (sablon.IsValid()) Addressables.Release(sablon);
        }
    }
}
