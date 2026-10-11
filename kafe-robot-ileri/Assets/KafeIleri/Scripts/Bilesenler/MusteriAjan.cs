// Ders 1.3 · Müşteri: kapıdan masasına yürür, servisten sonra kalkıp kapıdan çıkar.
// Ders 3.2 · herKareYol açıkken hedefi her karede yeniden hesaplatır (bilerek kötü örnek: darboğaz).
using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

namespace KafeIleri
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MusteriAjan : MonoBehaviour
    {
        public static readonly ProfilerMarker YolHesabi = new ProfilerMarker(ProfilerCategory.Scripts, "KafeIleri.YolHesabi");
        public static bool herKareYol;
        public static int YolSayaci;   // Ders 3.2: kaç kez yol hesaplatıldı
        public NavMeshAgent ajan;
        public Musteri kayit;
        public Vector3 hedef;
        public Action<MusteriAjan> Vardi;
        bool vardi, cikiyor;
        Vector3 kapi;

        void Awake() { ajan = GetComponent<NavMeshAgent>(); }

        public void Git(Vector3 nereye, Vector3 kapiNoktasi)
        {
            hedef = nereye; kapi = kapiNoktasi; vardi = false;
            { YolSayaci++; using (YolHesabi.Auto()) ajan.SetDestination(hedef); }
        }

        public void Kalk() { cikiyor = true; hedef = kapi; vardi = false; YolSayaci++; using (YolHesabi.Auto()) ajan.SetDestination(kapi); }

        void Update()
        {
            if (herKareYol && !vardi) { YolSayaci++; using (YolHesabi.Auto()) ajan.SetDestination(hedef); }
            if (!vardi && CanliBeden.Vardi(ajan))
            {
                vardi = true;
                if (cikiyor) { Destroy(gameObject); return; }
                Vardi?.Invoke(this);
            }
        }
    }
}
