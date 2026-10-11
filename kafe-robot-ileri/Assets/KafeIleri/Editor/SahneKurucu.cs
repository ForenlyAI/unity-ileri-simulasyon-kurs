// Kafe sahnesini, prefabları, NavMesh'i ve Addressables grubunu kuran editör betiği.
// Komut satırından:  unity run . -- -executeMethod KafeIleri.EditorAraclari.SahneKurucu.Kur
//                    unity run . -- -executeMethod KafeIleri.EditorAraclari.SahneKurucu.AddressablesDerle
// Editörde: menü KafeIleri > Sahneyi Kur / Addressables Derle
// Bütün ölçüler ÖRNEK'tir (12 × 10 m oda, 6 masa); robot hızı benzetim kaydından gelir.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace KafeIleri.EditorAraclari
{
    public static class SahneKurucu
    {
        public const string Kok = "Assets/KafeIleri";
        public const string SahneYolu = Kok + "/Sahneler/KafeIleri.unity";
        public const string NavMeshYolu = Kok + "/Sahneler/KafeIleri-NavMesh.asset";
        public const string MusteriYolu = Kok + "/Prefablar/Musteri.prefab";
        public const string MasaYolu = Kok + "/Prefablar/Masa.prefab";
        public const string GrupAdi = "KafeVarliklari";
        public const int GorunmezKatman = 2; // Ignore Raycast: NavMesh pişirmesine girmez

        public static readonly Vector3[] MasaKonumlari =
        {
            new Vector3(-3f, 0, -2f), new Vector3(0f, 0, -2f), new Vector3(3f, 0, -2f),
            new Vector3(-3f, 0, 1.5f), new Vector3(0f, 0, 1.5f), new Vector3(3f, 0, 1.5f),
        };

        static Material Malzeme(string ad, Color renk)
        {
            string yol = $"{Kok}/Malzemeler/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(sh); AssetDatabase.CreateAsset(m, yol);
            }
            m.SetColor("_BaseColor", renk); m.color = renk;
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Kutu(string ad, Transform ust, Vector3 merkez, Vector3 boyut, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = ad; g.transform.SetParent(ust, false);
            g.transform.localPosition = merkez; g.transform.localScale = boyut;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static GameObject Parca(PrimitiveType t, string ad, Transform ust, Vector3 yer, Vector3 boyut, Material mat)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = ad; g.transform.SetParent(ust, false);
            g.transform.localPosition = yer; g.transform.localScale = boyut;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.layer = GorunmezKatman;
            return g;
        }

        static void Dizinler()
        {
            foreach (var d in new[] { "Sahneler", "Prefablar", "Malzemeler" })
                if (!AssetDatabase.IsValidFolder($"{Kok}/{d}")) AssetDatabase.CreateFolder(Kok, d);
        }

        static GameObject MasaPrefabi()
        {
            var kok = new GameObject("Masa");
            var ahsap = Malzeme("Masa", new Color(0.55f, 0.38f, 0.24f));
            Kutu("Tabla", kok.transform, new Vector3(0, 0.74f, 0), new Vector3(0.8f, 0.04f, 0.8f), ahsap);
            Kutu("Ayak", kok.transform, new Vector3(0, 0.36f, 0), new Vector3(0.1f, 0.72f, 0.1f), ahsap);
            Kutu("Taban", kok.transform, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.04f, 0.5f), ahsap);
            var p = PrefabUtility.SaveAsPrefabAsset(kok, MasaYolu);
            Object.DestroyImmediate(kok);
            return p;
        }

        static GameObject MusteriPrefabi()
        {
            var kok = new GameObject("Musteri");
            kok.layer = GorunmezKatman;
            var mat = Malzeme("Musteri", new Color(0.30f, 0.55f, 0.85f));
            Parca(PrimitiveType.Capsule, "Govde", kok.transform, new Vector3(0, 0.85f, 0), new Vector3(0.45f, 0.85f, 0.45f), mat);
            var a = kok.AddComponent<NavMeshAgent>();
            a.speed = 1.0f; a.radius = 0.25f; a.height = 1.7f; a.acceleration = 8f; a.angularSpeed = 720f; a.stoppingDistance = 0.3f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            kok.AddComponent<MusteriAjan>();
            var p = PrefabUtility.SaveAsPrefabAsset(kok, MusteriYolu);
            Object.DestroyImmediate(kok);
            return p;
        }

        static GameObject Robot(Transform ust, Vector3 yer)
        {
            var kok = new GameObject("Robot");
            kok.layer = GorunmezKatman;
            kok.transform.SetParent(ust, false); kok.transform.position = yer;
            var beyaz = Malzeme("Robot", new Color(0.86f, 0.87f, 0.90f));
            var koyu = Malzeme("RobotEklem", new Color(0.18f, 0.19f, 0.22f));
            var turuncu = Malzeme("RobotTepsi", new Color(0.95f, 0.55f, 0.15f));
            var t = kok.transform;
            Parca(PrimitiveType.Capsule, "Govde", t, new Vector3(0, 0.95f, 0), new Vector3(0.36f, 0.32f, 0.24f), beyaz);
            Parca(PrimitiveType.Sphere, "Bas", t, new Vector3(0, 1.42f, 0), new Vector3(0.22f, 0.24f, 0.22f), beyaz);
            Parca(PrimitiveType.Cube, "Yuz", t, new Vector3(0, 1.43f, 0.09f), new Vector3(0.16f, 0.06f, 0.04f), koyu);
            Parca(PrimitiveType.Capsule, "SolBacak", t, new Vector3(-0.1f, 0.38f, 0), new Vector3(0.12f, 0.38f, 0.12f), koyu);
            Parca(PrimitiveType.Capsule, "SagBacak", t, new Vector3(0.1f, 0.38f, 0), new Vector3(0.12f, 0.38f, 0.12f), koyu);
            Parca(PrimitiveType.Capsule, "SolKol", t, new Vector3(-0.24f, 1.0f, 0.12f), new Vector3(0.09f, 0.22f, 0.09f), beyaz).transform.localRotation = Quaternion.Euler(60, 0, 0);
            Parca(PrimitiveType.Capsule, "SagKol", t, new Vector3(0.24f, 1.0f, 0.12f), new Vector3(0.09f, 0.22f, 0.09f), beyaz).transform.localRotation = Quaternion.Euler(60, 0, 0);
            Parca(PrimitiveType.Cylinder, "Tepsi", t, new Vector3(0, 1.08f, 0.3f), new Vector3(0.34f, 0.01f, 0.34f), turuncu);
            var a = kok.AddComponent<NavMeshAgent>();
            a.speed = 0.41f; a.radius = 0.3f; a.height = 1.55f; a.acceleration = 1.0f; a.angularSpeed = 180f; a.stoppingDistance = 0.1f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; a.avoidancePriority = 10;
            kok.AddComponent<CanliBeden>();
            return kok;
        }

        static Transform NoktaYap(Transform ust, string ad, Vector3 yer)
        {
            var g = new GameObject(ad); g.transform.SetParent(ust, false); g.transform.position = yer; return g.transform;
        }

        [MenuItem("KafeIleri/Sahneyi Kur")]
        public static void Kur()
        {
            Dizinler();
            var masaP = MasaPrefabi();
            MusteriPrefabi();
            var sahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var isik = new GameObject("Isik").AddComponent<Light>();
            isik.type = LightType.Directional; isik.intensity = 1.1f; isik.shadows = LightShadows.Soft;
            isik.transform.rotation = Quaternion.Euler(55, -35, 0);

            var kam = new GameObject("Kamera").AddComponent<Camera>();
            kam.tag = "MainCamera";
            kam.transform.position = new Vector3(0, 13.5f, -10.5f); kam.transform.rotation = Quaternion.Euler(55, 0, 0);
            kam.fieldOfView = 50; kam.clearFlags = CameraClearFlags.SolidColor; kam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);

            var kafe = new GameObject("Kafe");
            var zeminM = Malzeme("Zemin", new Color(0.78f, 0.74f, 0.66f));
            var duvarM = Malzeme("Duvar", new Color(0.42f, 0.45f, 0.50f));
            var tezgahM = Malzeme("Tezgah", new Color(0.30f, 0.22f, 0.18f));
            var sarjM = Malzeme("Sarj", new Color(0.20f, 0.65f, 0.40f));
            var t = kafe.transform;
            Kutu("Zemin", t, new Vector3(0, -0.05f, 0), new Vector3(12f, 0.1f, 10f), zeminM);
            Kutu("Giris", t, new Vector3(0, -0.05f, -6f), new Vector3(3f, 0.1f, 2f), zeminM);
            Kutu("DuvarKuzey", t, new Vector3(0, 0.6f, 5.1f), new Vector3(12.4f, 1.2f, 0.2f), duvarM);
            Kutu("DuvarBati", t, new Vector3(-6.1f, 0.6f, 0), new Vector3(0.2f, 1.2f, 10f), duvarM);
            Kutu("DuvarDogu", t, new Vector3(6.1f, 0.6f, 0), new Vector3(0.2f, 1.2f, 10f), duvarM);
            Kutu("DuvarGuneySol", t, new Vector3(-3.5f, 0.6f, -5.1f), new Vector3(5.4f, 1.2f, 0.2f), duvarM);
            Kutu("DuvarGuneySag", t, new Vector3(3.5f, 0.6f, -5.1f), new Vector3(5.4f, 1.2f, 0.2f), duvarM);
            Kutu("Tezgah", t, new Vector3(-4f, 0.5f, 4.3f), new Vector3(3f, 1f, 0.6f), tezgahM);
            Kutu("SarjIstasyonu", t, new Vector3(5.2f, 0.5f, 4.4f), new Vector3(0.6f, 1f, 0.6f), sarjM);

            var masalar = new GameObject("Masalar").transform; masalar.SetParent(t, false);
            for (int i = 0; i < MasaKonumlari.Length; i++)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(masaP, masalar);
                m.name = $"Masa{i + 1}"; m.transform.position = MasaKonumlari[i];
            }

            var yuzey = kafe.AddComponent<NavMeshSurface>();
            yuzey.collectObjects = CollectObjects.Children;
            yuzey.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            yuzey.layerMask = ~(1 << GorunmezKatman);

            var nk = new GameObject("Noktalar").transform;
            var liste = new List<Transform>
            {
                NoktaYap(nk, "Bekleme", new Vector3(-1.5f, 0, 3.4f)),
                NoktaYap(nk, "TezgahOnu", new Vector3(-4f, 0, 3.3f)),
                NoktaYap(nk, "SarjNoktasi", new Vector3(4.3f, 0, 3.6f)),
                NoktaYap(nk, "Kapi", new Vector3(0, 0, -6.2f)),
            };
            for (int i = 0; i < MasaKonumlari.Length; i++) liste.Add(NoktaYap(nk, $"Masa{i + 1}Servis", MasaKonumlari[i] + new Vector3(0, 0, -1.0f)));
            for (int i = 0; i < MasaKonumlari.Length; i++) liste.Add(NoktaYap(nk, $"Masa{i + 1}Oturma", MasaKonumlari[i] + new Vector3(0, 0, 1.0f)));
            var noktalar = nk.gameObject.AddComponent<KafeNoktalari>();
            noktalar.noktalar = liste.ToArray(); noktalar.masaSayisi = MasaKonumlari.Length;

            yuzey.BuildNavMesh();
            if (File.Exists(NavMeshYolu)) AssetDatabase.DeleteAsset(NavMeshYolu);
            AssetDatabase.CreateAsset(yuzey.navMeshData, NavMeshYolu);
            yuzey.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshYolu);

            var robot = Robot(null, liste[0].position);
            var beden = robot.GetComponent<CanliBeden>(); beden.noktalar = noktalar;

            var yon = new GameObject("KafeYoneticisi").AddComponent<KafeYoneticisi>();
            yon.noktalar = noktalar; yon.robot = beden;
            yon.musteriSablonu = AssetDatabase.LoadAssetAtPath<GameObject>(MusteriYolu);
            yon.otomatikBasla = true; yon.zamanCarpani = 10f;

            EditorSceneManager.SaveScene(sahne, SahneYolu);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SahneYolu, true) };
            AddressablesKur();
            MalzemeleriYenile();
            // Web derlemesi sıkıştırmasız: her durağan dosya sunucusunda açılır
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            AssetDatabase.SaveAssets();
            Debug.Log($"[KafeIleri] sahne kuruldu: {SahneYolu} · {MasaKonumlari.Length} masa · {liste.Count} nokta");
        }

        public static AddressableAssetSettings AddressablesKur()
        {
            var ayar = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var grup = ayar.FindGroup(GrupAdi) ?? ayar.CreateGroup(GrupAdi, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var sema = grup.GetSchema<BundledAssetGroupSchema>();
            sema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            ayar.AddLabel("kafe");
            foreach (var (yol, adres) in new[] { (MusteriYolu, "Musteri"), (MasaYolu, "Masa"), (Kok + "/Veri/kayit_ozet.json", "KayitOzeti") })
            {
                var e = ayar.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(yol), grup);
                e.address = adres; e.SetLabel("kafe", true, true);
            }
            // Play modunda gerçek paketlerden yükle ("Use Existing Build"): ölçüm paket dosyalarını içersin.
            for (int i = 0; i < ayar.DataBuilders.Count; i++)
                if (ayar.DataBuilders[i] is UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedPlayMode) ayar.ActivePlayModeDataBuilderIndex = i;
            EditorUtility.SetDirty(ayar);
            AssetDatabase.SaveAssets();
            return ayar;
        }

        // Betikle yaratılan malzemeleri URP'nin güncel malzeme sürümüyle yeniden içe aktarır (editör açılışında yükseltme penceresi çıkmasın).
        public static void MalzemeleriYenile()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { Kok + "/Malzemeler" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("[KafeIleri] malzemeler yenilendi");
        }

        [MenuItem("KafeIleri/Addressables Derle")]
        public static void AddressablesDerle()
        {
            AddressablesKur();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult sonuc);
            if (!string.IsNullOrEmpty(sonuc.Error)) throw new System.Exception(sonuc.Error);
            Debug.Log($"[KafeIleri] Addressables derlendi: {sonuc.AssetBundleBuildResults.Count} paket · {sonuc.Duration:0.00} sn");
        }
    }
}
