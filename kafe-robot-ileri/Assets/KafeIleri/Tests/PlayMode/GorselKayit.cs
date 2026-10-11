// Ders ekranları için Play modunda Unity'nin çizdiği kareleri PNG olarak kaydeder (Olcumler/gorsel/).
// [Explicit]: olağan test koşumuna girmez; yalnız adıyla istenince koşar:
//   unity test . --mode PlayMode --filter "KafeIleri.Testler.GorselKayit"
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace KafeIleri.Testler
{
    [Explicit, Category("Gorsel")]
    public class GorselKayit
    {
        const int W = 1600, H = 900;
        [TearDown] public void Temizle() => Oyun.Kapat();

        static string Yol(string ad) { var k = Path.Combine(Olcum.Klasor, "gorsel"); Directory.CreateDirectory(k); return Path.Combine(k, ad + ".png"); }

        static void Kaydet(Camera kam, string ad)
        {
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            var onceki = kam.targetTexture;
            kam.targetTexture = rt; kam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
            RenderTexture.active = null; kam.targetTexture = onceki;
            File.WriteAllBytes(Yol(ad), t.EncodeToPNG());
            Object.Destroy(t); RenderTexture.ReleaseTemporary(rt);
        }

        static Material Renk(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            m.SetColor("_BaseColor", c); m.color = c; return m;
        }

        static Camera UstKamera()
        {
            var k = new GameObject("UstKamera").AddComponent<Camera>();
            k.orthographic = true; k.orthographicSize = 6.6f;
            k.transform.position = new Vector3(0, 20, -0.9f); k.transform.rotation = Quaternion.Euler(90, 0, 0);
            k.clearFlags = CameraClearFlags.SolidColor; k.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            k.enabled = false; return k;
        }

        static Camera YanKamera(Vector3 bakilan, Vector3 fark)
        {
            var k = new GameObject("YanKamera").AddComponent<Camera>();
            k.fieldOfView = 40; k.transform.position = bakilan + fark; k.transform.LookAt(bakilan + Vector3.up * 0.6f);
            k.clearFlags = CameraClearFlags.SolidColor; k.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            k.enabled = false; return k;
        }

        static GameObject NavMeshOrtusu(Color c)
        {
            var tri = NavMesh.CalculateTriangulation();
            var m = new Mesh { vertices = tri.vertices.Select(v => v + Vector3.up * 0.03f).ToArray(), triangles = tri.indices };
            m.RecalculateNormals();
            var g = new GameObject("NavMeshOrtusu");
            g.AddComponent<MeshFilter>().sharedMesh = m;
            g.AddComponent<MeshRenderer>().sharedMaterial = Renk(c);
            return g;
        }

        static LineRenderer Cizgi(Vector3 a, Vector3 b, Color c, float kalinlik = 0.12f)
        {
            var y = new NavMeshPath();
            NavMesh.CalculatePath(KafeNoktalari.Oturt(a), KafeNoktalari.Oturt(b), NavMesh.AllAreas, y);
            var l = new GameObject("Yol").AddComponent<LineRenderer>();
            l.positionCount = y.corners.Length; l.SetPositions(y.corners.Select(p => p + Vector3.up * 0.08f).ToArray());
            l.widthMultiplier = kalinlik; l.sharedMaterial = Renk(c); l.alignment = LineAlignment.TransformZ;
            l.transform.rotation = Quaternion.Euler(90, 0, 0);
            return l;
        }

        [UnityTest]
        public IEnumerator Kareler()
        {
            yield return Oyun.SahneyiAc(30);
            var n = Oyun.Noktalar; var y = Oyun.Yonetici;
            var ust = UstKamera();
            var ana = Camera.main;

            // 1.1 · NavMesh ve en uzak masaya yol
            var ortu = NavMeshOrtusu(new Color(0.35f, 0.62f, 0.95f));
            var c1 = Cizgi(n.Konum(Nokta.Ev), n.Konum(Nokta.Servis(2)), new Color(1f, 0.55f, 0.1f));
            yield return null;
            Kaydet(ust, "1.1-navmesh-ust");
            Kaydet(ana, "1.1-navmesh-perspektif");
            Object.Destroy(c1.gameObject);

            // 1.2 · temizlik arabası öncesi / sonrası
            var once = Cizgi(n.Konum(Nokta.Ev), n.Konum(Nokta.Servis(1)), new Color(0.6f, 0.6f, 0.6f), 0.08f);
            var araba = GameObject.CreatePrimitive(PrimitiveType.Cube);
            araba.transform.position = new Vector3(-1.5f, 0.5f, -0.25f); araba.transform.localScale = new Vector3(2.2f, 1f, 0.6f);
            araba.GetComponent<Renderer>().sharedMaterial = Renk(new Color(0.85f, 0.2f, 0.25f));
            var e = araba.AddComponent<NavMeshObstacle>(); e.carving = true;
            for (int i = 0; i < 5; i++) yield return null;
            Object.Destroy(ortu);
            ortu = NavMeshOrtusu(new Color(0.35f, 0.62f, 0.95f));
            var sonra = Cizgi(n.Konum(Nokta.Ev), n.Konum(Nokta.Servis(1)), new Color(1f, 0.55f, 0.1f));
            yield return null;
            Kaydet(ust, "1.2-engel-ust");
            Object.Destroy(araba); Object.Destroy(once.gameObject); Object.Destroy(sonra.gameObject); Object.Destroy(ortu);
            yield return null;

            // 1.2 · iki müşteri koridorda karşılaşır (kaçınma açık)
            var sablon = y.musteriSablonu;
            var x1 = Object.Instantiate(sablon, new Vector3(-5f, 0, -0.25f), Quaternion.identity).GetComponent<NavMeshAgent>();
            var x2 = Object.Instantiate(sablon, new Vector3(5f, 0, -0.25f), Quaternion.identity).GetComponent<NavMeshAgent>();
            x1.SetDestination(new Vector3(5f, 0, -0.25f)); x2.SetDestination(new Vector3(-5f, 0, -0.25f));
            for (int i = 0; i < 300; i++)
            {
                if (Vector3.Distance(x1.transform.position, x2.transform.position) < 0.9f) break;
                yield return null;
            }
            var yan = YanKamera(new Vector3(0, 0, -0.25f), new Vector3(0, 3.2f, -5.5f));
            Kaydet(yan, "1.2-kacinma-yan");
            Object.Destroy(x1.gameObject); Object.Destroy(x2.gameObject); Object.Destroy(yan.gameObject);

            // 3.1 · yüz müşteri
            var r = new System.Random(3);
            var kalabalik = new List<GameObject>();
            for (int i = 0; i < 100; i++)
            {
                var p = KafeNoktalari.Oturt(new Vector3((float)(r.NextDouble() * 10 - 5), 0, (float)(r.NextDouble() * 8 - 4)));
                var g = Object.Instantiate(sablon, p, Quaternion.identity); kalabalik.Add(g);
                g.GetComponent<NavMeshAgent>().SetDestination(KafeNoktalari.Oturt(new Vector3((float)(r.NextDouble() * 10 - 5), 0, (float)(r.NextDouble() * 8 - 4))));
            }
            for (int i = 0; i < 60; i++) yield return null;
            Kaydet(ust, "3.1-yuz-ajan-ust");
            foreach (var g in kalabalik) Object.Destroy(g);
            yield return null;

            // Canlı vardiya: kalabalık bir saat (gelişler sık, ÖRNEK); durum değişince kare
            Time.captureFramerate = 20; Time.timeScale = 4f;
            y.ayar = new VardiyaAyar { ortalamaGelis = 60, vardiya = 3000 };
            y.addressablesKullan = false;   // editörde paketteki gölgelendirici bu grafik arka ucunda çizilmiyor (pembe); görüntü için doğrudan şablon
            var cekilen = new HashSet<string>();
            y.StartCoroutine(y.Baslat());
            int kare = 0;
            while (!y.Bitti && kare < 20000)
            {
                kare++;
                yield return null;
                if (y.program == null) continue;
                var d = y.program.makine.Simdiki; var rp = y.robot.transform.position;
                if (d == Durum.ServisYap && y.robot.Nokta == Nokta.Tezgah && !cekilen.Contains("2.1"))
                {
                    var k = YanKamera(rp, new Vector3(1.6f, 1.6f, -2.4f)); Kaydet(k, "2.1-tezgahta-robot"); Object.Destroy(k.gameObject); cekilen.Add("2.1");
                }
                if (d == Durum.SarjOl && y.robot.Nokta == Nokta.Sarj && !cekilen.Contains("2.3"))
                {
                    var k = YanKamera(rp, new Vector3(-2.0f, 1.7f, -2.2f)); Kaydet(k, "2.3-sarjda-robot"); Object.Destroy(k.gameObject); cekilen.Add("2.3");
                }
                if (y.ajanlar.Count(a => a != null) >= 5 && !cekilen.Contains("1.3")) { Kaydet(ana, "1.3-musteri-akisi"); cekilen.Add("1.3"); }
                if (d == Durum.SiparisAl && y.kafe.Bekleyenler(y.robot.Simdi).Count() >= 2 && !cekilen.Contains("2.2")) { Kaydet(ust, "2.2-sira-ust"); cekilen.Add("2.2"); }
            }
            Kaydet(ana, "4.4-vardiya-sonu");
            Olcum.Yaz("gorsel", new Dictionary<string, object> { { "cekilen", cekilen.OrderBy(s => s).ToList() }, { "kare", kare } });
        }
            // Ders sayfalarındaki fotoğraf yuvası için: kalabalık bir vardiyadan farklı açılarla 72 kare (Olcumler/gorsel/seri/).
        [UnityTest]
        public IEnumerator FotoSerisi()
        {
            yield return Oyun.SahneyiAc(20, 4f);
            var y = Oyun.Yonetici; var ana = Camera.main; var ust = UstKamera();
            var yan = YanKamera(Vector3.zero, Vector3.one);
            y.ayar = new VardiyaAyar { ortalamaGelis = 45, vardiya = 3600, tohum = 21 };
            y.addressablesKullan = false;
            y.StartCoroutine(y.Baslat());
            var aci = new[] { new Vector3(2.2f, 1.7f, -2.6f), new Vector3(-2.4f, 1.9f, -2.2f), new Vector3(0.2f, 2.6f, -3.4f), new Vector3(3.0f, 1.2f, 1.2f), new Vector3(-1.6f, 3.2f, 2.4f), new Vector3(0f, 1.4f, 2.8f) };
            int kare = 0, no = 0;
            while (!y.Bitti && no < 72 && kare < 40000)
            {
                kare++; yield return null;
                if (kare % 220 != 0) continue;
                var rp = y.robot.transform.position;
                Camera k;
                if (no % 4 == 0) k = no % 8 == 0 ? ana : ust;
                else { k = yan; yan.transform.position = rp + aci[no % aci.Length]; yan.transform.LookAt(rp + Vector3.up * 0.8f); yan.fieldOfView = 34 + (no % 3) * 8; }
                var dosya = Path.Combine(Olcum.Klasor, "gorsel", "seri"); Directory.CreateDirectory(dosya);
                var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
                k.targetTexture = rt; k.Render(); RenderTexture.active = rt;
                var t = new Texture2D(W, H, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
                RenderTexture.active = null; k.targetTexture = null;
                File.WriteAllBytes(Path.Combine(dosya, $"s{no:00}-{y.program.makine.Simdiki}.png"), t.EncodeToPNG());
                Object.Destroy(t); RenderTexture.ReleaseTemporary(rt);
                no++;
            }
        }
    }
}
