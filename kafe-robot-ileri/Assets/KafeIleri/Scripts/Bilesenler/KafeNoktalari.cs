// Ders 1.1 · Kafedeki adlı noktalar ve aralarındaki NavMesh yol uzunlukları.
// Sıra: bekleme yeri, tezgâh, şarj istasyonu, kapı, masaların servis noktaları, masaların oturma noktaları.
using UnityEngine;
using UnityEngine.AI;

namespace KafeIleri
{
    public class KafeNoktalari : MonoBehaviour
    {
        public Transform[] noktalar;
        public int masaSayisi = 6;

        public Vector3 Konum(int i) => noktalar[i].position;

        // Noktayı en yakın NavMesh noktasına oturtur (1 m içinde); bulamazsa olduğu gibi döner.
        public static Vector3 Oturt(Vector3 p) => NavMesh.SamplePosition(p, out var h, 1f, NavMesh.AllAreas) ? h.position : p;

        public static float YolUzunlugu(NavMeshPath yol)
        {
            float t = 0;
            for (int i = 1; i < yol.corners.Length; i++) t += Vector3.Distance(yol.corners[i - 1], yol.corners[i]);
            return t;
        }

        // Her nokta çifti için NavMesh.CalculatePath; yol yoksa -1.
        public MesafeTablosu YolTablosu()
        {
            int n = noktalar.Length;
            var m = new double[n, n];
            var yol = new NavMeshPath();
            for (int a = 0; a < n; a++)
                for (int b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    bool var = NavMesh.CalculatePath(Oturt(Konum(a)), Oturt(Konum(b)), NavMesh.AllAreas, yol) && yol.status == NavMeshPathStatus.PathComplete;
                    m[a, b] = var ? YolUzunlugu(yol) : -1;
                }
            return new MesafeTablosu(m);
        }
    }
}
