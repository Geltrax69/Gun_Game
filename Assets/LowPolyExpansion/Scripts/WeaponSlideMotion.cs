using UnityEngine;
using Unity.FPS.Game;

namespace LowPolyExpansion
{
    [RequireComponent(typeof(WeaponController))]
    public class WeaponSlideMotion : MonoBehaviour
    {
        public Transform Slide;
        public float Travel = .045f;
        WeaponController weapon;
        Vector3 rest;
        float kick;
        void Awake()
        {
            weapon = GetComponent<WeaponController>();
            if (Slide) rest = Slide.localPosition;
            weapon.OnShoot += Shot;
        }
        void Shot() { kick = 1; }
        void LateUpdate()
        {
            kick *= Mathf.Exp(-18 * Time.deltaTime);
            if (Slide) Slide.localPosition = rest + Vector3.back * kick * Travel;
        }
        void OnDestroy() { if (weapon) weapon.OnShoot -= Shot; }
    }
}
