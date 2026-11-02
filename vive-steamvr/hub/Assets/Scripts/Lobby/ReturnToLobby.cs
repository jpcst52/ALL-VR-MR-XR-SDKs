using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using PittBradford.XR;

/// <summary>
/// Added to every scenario scene in the hub. Hold both grips for 1.5 s (or press Backspace on the desktop) to go back
/// to the lobby. Shows a short hint at the start.
/// </summary>
public class ReturnToLobby : MonoBehaviour
{
    const float HoldSeconds = 1.5f, HintSeconds = 8f;

    XRRig rig;
    float held, shown;
    TextMesh hint;
    bool loading;

    void Update()
    {
        if (loading) return;
        if (rig == null) { rig = FindFirstObjectByType<XRRig>(); if (rig == null) return; MakeHint(); }

        bool both = rig.Left != null && rig.Right != null && rig.Left.GripHeld && rig.Right.GripHeld;
        var kb = Keyboard.current;
        if (kb != null && kb.backspaceKey.wasPressedThisFrame) { Go(); return; }
        held = both ? held + Time.deltaTime : 0f;
        if (held >= HoldSeconds) { Go(); return; }

        shown += Time.deltaTime;
        if (hint != null)
        {
            hint.text = held > 0.1f ? "Returning to lobby..." : "Hold both grips (or Backspace) to return to the lobby";
            if (shown > HintSeconds && held < 0.1f) { Destroy(hint.gameObject); hint = null; }
        }
    }

    void MakeHint()
    {
        hint = P.Text(rig.Camera.transform, "", new Vector3(0, -0.28f, 1.2f), 0.045f, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, true);
    }

    void Go()
    {
        loading = true;
        SceneManager.LoadScene(LobbyHub.LobbyScene);
    }
}
