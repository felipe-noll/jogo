using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLock : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;
    [SerializeField] private Key unlockKey = Key.Escape;

    private void Start()
    {
        if (lockOnStart) Lock();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[unlockKey].wasPressedThisFrame) Unlock();

        // Clique na tela volta a travar (útil depois de destravar)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Mouse m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) Lock();
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && lockOnStart) Lock();
    }

    public void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}