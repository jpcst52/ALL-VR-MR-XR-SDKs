using Meta.XR;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestPassthroughCameraPreview : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess cameraAccess;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Text statusText;

    private bool previewAssigned;

    private void Update()
    {
        if (cameraAccess == null)
        {
            SetStatus("Assign a PassthroughCameraAccess component.");
            return;
        }

        if (!cameraAccess.IsPlaying)
        {
            SetStatus("Waiting for Quest passthrough camera access...");
            return;
        }

        if (!previewAssigned && previewImage != null)
        {
            previewImage.texture = cameraAccess.GetTexture();
            previewAssigned = true;
        }

        SetStatus("Quest passthrough camera is streaming.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
