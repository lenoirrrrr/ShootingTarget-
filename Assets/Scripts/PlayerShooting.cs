using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public Gun gun;

    void OnReload()
    {
        if (gun != null)
        {
            gun.TryReload();
        }
    }

    void OnShoot()
    {
        if (gun != null)
        {
            gun.Shoot();
        }
    }
}
