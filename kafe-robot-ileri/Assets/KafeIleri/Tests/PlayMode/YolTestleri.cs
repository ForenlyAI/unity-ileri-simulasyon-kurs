// Play Mode: NavMeshAgent gerçekten yürür. Robotun yürüyüşü, engel ve iki ajanın karşılaşması.
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KafeIleri.Testler
{
    public static class Oyun
    {
        public static IEnumerator SahneyiAc(int kareHizi = 30, float zamanCarpani = 1f)
        {
            KafeYoneticisi.otomatikKapali = true;
            SceneManager.LoadScene("KafeIleri", LoadSceneMode.Single);
            yield return null; yield return null;
            Time.captureFramerate = kareHizi; Time.timeScale = zamanCarpani;
        }
        public static void Kapat() { Time.captureFramerate = 0; Time.timeScale = 1f; MusteriAjan.herKareYol = false; KafeYoneticisi.otomatikKapali = false; }
        public static KafeNoktalari Noktalar => Object.FindFirstObjectByType<KafeNoktalari>();
        public static KafeYoneticisi Yonetici => Object.FindFirstObjectByType<KafeYoneticisi>();
        public static float Yol(Vector3 a, Vector3 b, out int kose, out NavMeshPathStatus durum)
        {
            var y = new NavMeshPath();
            NavMesh.CalculatePath(KafeNoktalari.Oturt(a), KafeNoktalari.Oturt(b), NavMesh.AllAreas, y);
            kose = y.corners.Length; durum = y.status;
            return KafeNoktalari.YolUzunlugu(y);
        }
    }

    public class YolTestleri
    {
        [TearDown] public void Temizle() => Oyun.Kapat();

        [UnityTest]
        public IEnumerator Robot_Masaya_Yurur()
        {
            yield return Oyun.SahneyiAc(30);
            var n = Oyun.Noktalar; var robot = Oyun.Yonetici.robot;
            int hedef = Nokta.Servis(2);
            float beklenen = Oyun.Yol(n.Konum(Nokta.Ev), n.Konum(hedef), out int kose, out _);
            var pil = new Pil(new PilAyar());
            robot.SaatiSifirla();
            var is_ = robot.Yuru(hedef, pil);
            while (is_.MoveNext()) yield return is_.Current;
            double sure = robot.Simdi;
            Olcum.Yaz("yuruyus", new Dictionary<string, object>
            {
                { "hedef", n.noktalar[hedef].name }, { "yol_m", Olcum.Yuvarla(beklenen, 2) }, { "kose", kose },
                { "yurunen_m", Olcum.Yuvarla(robot.Yol, 2) }, { "sure_s", Olcum.Yuvarla(sure, 1) },
                { "hesap_s", Olcum.Yuvarla(beklenen / robot.ajan.speed, 1) }, { "hiz", robot.ajan.speed }, { "ivme", robot.ajan.acceleration },
                { "kalan_m", Olcum.Yuvarla(Vector3.Distance(robot.transform.position, KafeNoktalari.Oturt(n.Konum(hedef))), 2) },
                { "pil_once", 60 }, { "pil_sonra", Olcum.Yuvarla(pil.Yuzde, 1) },
            });
            Assert.Less(Vector3.Distance(robot.transform.position, KafeNoktalari.Oturt(n.Konum(hedef))), 0.3f);
        }

        [UnityTest]
        public IEnumerator Engel_Yolu_Degistirir()
        {
            yield return Oyun.SahneyiAc(30);
            var n = Oyun.Noktalar;
            Vector3 a = n.Konum(Nokta.Ev), b = n.Konum(Nokta.Servis(1));
            float once = Oyun.Yol(a, b, out int koseOnce, out _);
            // temizlik arabası (ÖRNEK 2,2 × 0,6 m) iki masa arasındaki geçidi kapatır
            var araba = GameObject.CreatePrimitive(PrimitiveType.Cube);
            araba.name = "TemizlikArabasi"; araba.transform.position = new Vector3(-1.5f, 0.5f, -0.25f); araba.transform.localScale = new Vector3(2.2f, 1f, 0.6f);
            var engel = araba.AddComponent<NavMeshObstacle>(); engel.carving = true; engel.carveOnlyStationary = false;
            for (int i = 0; i < 5; i++) yield return null;
            float sonra = Oyun.Yol(a, b, out int koseSonra, out var durumSonra);
            // kapının önüne konan engel: içeri yol kalmaz
            var kapiEngeli = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kapiEngeli.transform.position = new Vector3(0f, 0.5f, -5.1f); kapiEngeli.transform.localScale = new Vector3(1.8f, 1f, 0.6f);
            var e2 = kapiEngeli.AddComponent<NavMeshObstacle>(); e2.carving = true;
            for (int i = 0; i < 5; i++) yield return null;
            Oyun.Yol(n.Konum(Nokta.Kapi), n.Konum(Nokta.Oturma(0, n.masaSayisi)), out _, out var kapiDurum);
            Olcum.Yaz("engel", new Dictionary<string, object>
            {
                { "hedef", n.noktalar[Nokta.Servis(1)].name }, { "once_m", Olcum.Yuvarla(once, 2) }, { "once_kose", koseOnce },
                { "sonra_m", Olcum.Yuvarla(sonra, 2) }, { "sonra_kose", koseSonra }, { "sonra_durum", durumSonra.ToString() },
                { "fark_m", Olcum.Yuvarla(sonra - once, 2) }, { "kapi_durum", kapiDurum.ToString() },
            });
            Assert.AreEqual(NavMeshPathStatus.PathComplete, durumSonra);
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, kapiDurum);
        }

        IEnumerator Karsilasma(bool kacinma, Dictionary<string, object> veri, string ad)
        {
            yield return Oyun.SahneyiAc(30);
            var sablon = Oyun.Yonetici.musteriSablonu;
            Vector3 sol = new Vector3(-5f, 0, -0.25f), sag = new Vector3(5f, 0, -0.25f);
            var x = Object.Instantiate(sablon, sol, Quaternion.identity).GetComponent<NavMeshAgent>();
            var y = Object.Instantiate(sablon, sag, Quaternion.identity).GetComponent<NavMeshAgent>();
            foreach (var a in new[] { x, y })
                a.obstacleAvoidanceType = kacinma ? ObstacleAvoidanceType.HighQualityObstacleAvoidance : ObstacleAvoidanceType.NoObstacleAvoidance;
            x.SetDestination(sag); y.SetDestination(sol);
            float enYakin = 99f;
            for (int k = 0; k < 30 * 20; k++)
            {
                var p = x.transform.position; var q = y.transform.position; p.y = q.y = 0;
                enYakin = Mathf.Min(enYakin, Vector3.Distance(p, q));
                if (CanliBeden.Vardi(x) && CanliBeden.Vardi(y) && k > 10) break;
                yield return null;
            }
            veri[ad] = Olcum.Yuvarla(enYakin, 2);
            veri["yaricap_toplam"] = Olcum.Yuvarla(x.radius + y.radius, 2);
        }

        [UnityTest]
        public IEnumerator Iki_Ajan_Karsilasir()
        {
            var veri = new Dictionary<string, object>();
            yield return Karsilasma(false, veri, "kacinmasiz_en_yakin_m");
            yield return Karsilasma(true, veri, "kacinmali_en_yakin_m");
            Olcum.Yaz("kacinma", veri);
            Assert.Greater((double)veri["kacinmali_en_yakin_m"], (double)veri["kacinmasiz_en_yakin_m"]);
        }
    }
}
