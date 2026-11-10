using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using PittBradford.XR;

/// <summary>
/// Added to every scenario scene in the hub. Three ways back to the lobby:
///   1. Point at the "Lobby" panel that floats above the left controller and pull the trigger (desktop: aim the crosshair at it and click).
///   2. Hold both grips for 1.5 s.
///   3. Press Backspace on the desktop.
/// A short hint at the start says so.
/// </summary>
public class ReturnToLobby : MonoBehaviour
{
    const float HoldSeconds = 1.5f, HintSeconds = 10f;

    XRRig rig;
    float held, shown;
    TextMesh hint;
    bool loading;

    void Update()
    {
        if (loading) return;
        if (rig == null) { rig = FindFirstObjectByType<XRRig>(); if (rig == null) return; MakeHint(); MakeButton(); }

        bool both = rig.Left != null && rig.Right != null && rig.Left.GripHeld && rig.Right.GripHeld;
        var kb = Keyboard.current;
        if (kb != null && kb.backspaceKey.wasPressedThisFrame) { Go(); return; }
        held = both ? held + Time.deltaTime : 0f;
        if (held >= HoldSeconds) { Go(); return; }

        shown += Time.deltaTime;
        if (hint != null)
        {
            hint.text = held > 0.1f ? "Returning to lobby..." : "Point at the Lobby panel on your left hand and press the trigger\n(or hold both grips, or Backspace) to return to the lobby";
            if (shown > HintSeconds && held < 0.1f) { Destroy(hint.gameObject); hint = null; }
        }
    }

    void MakeHint()
    {
        hint = P.Text(rig.Camera.transform, "", new Vector3(0, -0.28f, 1.2f), 0.04f, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, true, "Lobby hint");
        hint.gameObject.AddComponent<DontBake>();
    }

    /// <summary>
    /// A small panel hovering above the left controller, tilted up toward the eyes, with a collider and a Pressable, so the other hand's
    /// ray can select it like any button in the scene. It rides on the hand, so it is always within reach and never hides the scene.
    /// </summary>
    void MakeButton()
    {
        var hand = rig.Left;
        if (hand == null) return;
        var root = new GameObject("Lobby button");
        root.AddComponent<DontBake>();
        root.transform.SetParent(hand.transform, false);
        // 9 cm above the controller and a little back; the face (which looks toward -Z) tilts up and back at the wearer, its top edge toward the front of the controller
        root.transform.localPosition = new Vector3(0f, 0.09f, -0.03f);
        root.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -0.8f, 0.6f), new Vector3(0f, 0.6f, 0.8f));

        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.24f, 0.09f, 0.06f); // a little bigger than the drawn panel so it is easy to hit
        var press = root.AddComponent<Pressable>();
        press.Label = "Back to lobby";
        press.Pressed += h => Go();

        Ui.Panel(root.transform, "Panel", Vector3.zero, new Vector2(0.22f, 0.08f), new Color(0f, 0.13f, 0.36f, 0.97f), P.Gold, 0.1f);
        var label = P.Text(root.transform, "< Lobby", new Vector3(0f, -0.004f, -0.012f), 0.032f, Color.white, TextAnchor.MiddleCenter, true, "Lobby button text");
        TextFit.Add(label, 0.19f, 0f, false);
    }

    void Go()
    {
        if (loading) return;
        loading = true;
        SceneManager.LoadScene(LobbyHub.LobbyScene);
    }
}
