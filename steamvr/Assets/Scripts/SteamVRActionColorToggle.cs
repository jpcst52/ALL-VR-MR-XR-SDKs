using UnityEngine;
using Valve.VR;

public sealed class SteamVRActionColorToggle : MonoBehaviour
{
    [SerializeField] private SteamVR_Action_Boolean toggleAction;
    [SerializeField] private SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color inactiveColor = Color.white;
    [SerializeField] private Color activeColor = Color.cyan;

    private bool isActive;

    private void Start()
    {
        ApplyColor();
    }

    private void Update()
    {
        if (toggleAction != null && toggleAction.GetStateDown(inputSource))
        {
            isActive = !isActive;
            ApplyColor();
        }
    }

    private void ApplyColor()
    {
        if (targetRenderer != null)
        {
            targetRenderer.material.color = isActive ? activeColor : inactiveColor;
        }
    }
}
