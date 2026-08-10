using UnityEngine;

public class CamFollowPlayer : MonoBehaviour
{
    public PlayerControllerModule Controller;
    public FirstPersonCamera FPCam;
    internal Camera Cam;
    public void ClientInit()
    {
        Cam = Camera.main;
        FPCam.transform.SetParent(null);
    }
    void LateUpdate()
    {
        if (Controller != null)
        {
            FPCam.transform.position = Controller.SmoothedVisual.transform.position;
            FPCam.transform.localRotation = Quaternion.Euler(Controller.LookDelta.y, Controller.LookDelta.x, 0f);
        }
        if (Cam != null)
        {
            Cam.transform.position = FPCam.transform.position;
            Cam.transform.localRotation = FPCam.transform.localRotation;
        }
    }
}
