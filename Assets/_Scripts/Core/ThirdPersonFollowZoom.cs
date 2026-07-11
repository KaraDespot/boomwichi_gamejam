using UnityEngine;
using Unity.Cinemachine;

public class ThirdPersonFollowZoom : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private float minCameraDistance = 2f;
    [SerializeField] private float maxCameraDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;

    private CinemachineThirdPersonFollow follow;

    private void Awake()
    {
        if (cmCamera == null)
            cmCamera = GetComponent<CinemachineCamera>();

        if (cmCamera != null)
            follow = cmCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
    }

    private void Update()
    {
        if (follow == null)
            return;

        float scroll = InputManager.Instance != null ? InputManager.Instance.GetZoomInput() : 0f; 
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            follow.CameraDistance = Mathf.Clamp(
                follow.CameraDistance - scroll * zoomSpeed,
                minCameraDistance,
                maxCameraDistance
            );
        }
    }
}
