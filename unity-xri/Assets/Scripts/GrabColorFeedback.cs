using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class GrabColorFeedback : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color selectedColor = Color.green;

    private XRGrabInteractable grabInteractable;
    private Color originalColor;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null)
        {
            originalColor = targetRenderer.material.color;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelected);
        grabInteractable.selectExited.AddListener(OnDeselected);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelected);
        grabInteractable.selectExited.RemoveListener(OnDeselected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        SetColor(selectedColor);
    }

    private void OnDeselected(SelectExitEventArgs args)
    {
        SetColor(originalColor);
    }

    private void SetColor(Color color)
    {
        if (targetRenderer != null)
        {
            targetRenderer.material.color = color;
        }
    }
}
