using UnityEngine;


public class ThirdPersonHealthBar : MonoBehaviour
{
    public PlayerControllerModule Controller;
    public GameObject HealthBar;
    public Transform HealthBarPivot;

    private Camera MainCam;
    private float HealthBarActiveTimer;
    private readonly float HealthBarShowTime = 5f;
    public void Init()
    {
        MainCam = Camera.main;
        transform.parent = null;
    }
    public void Show(float ratio)
    {
        Debug.Log(ratio);
        HealthBar.SetActive(true);
        HealthBarPivot.localScale = new Vector3(ratio, HealthBarPivot.localScale.y, HealthBarPivot.localScale.z);
        HealthBarActiveTimer = HealthBarShowTime;
    }

    void LateUpdate()
    {
        if (!HealthBar.activeInHierarchy) return;

        float camY = MainCam != null ? MainCam.transform.eulerAngles.y : 0f;
        HealthBar.transform.SetPositionAndRotation(Controller.SmoothedVisual.transform.position, Quaternion.Euler(0f, camY, 0f));
        HealthBarActiveTimer -= Time.deltaTime;

        if (HealthBarActiveTimer <= 0)
            HealthBar.SetActive(false);
    }
}
