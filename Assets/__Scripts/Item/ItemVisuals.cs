using System.Collections;
using UnityEngine;

public class ItemVisuals : MonoBehaviour
{
    public WorldItemGameObject ItemPickUp;
    public LayerMask GroundMask;
    public Renderer OreRenderer;
    public Color GlowColor = new Color(1f, 0.6f, 0.1f);

    [Header("Jump")]
    private float GroundYOffset = 0.4f;
    private float JumpHeight = 1.5f;
    private float JumpDuration = 0.4f;
    private float ScatterRadius = 1.5f;

    [Header("Hover")]
    private float HoverHeight = 0.15f;
    private float HoverSpeed = 1.2f;

    [Header("Rotation")]
    private float RotationSpeed = 35f;
    private Vector3 RotationAxis = new Vector3(0.2f, 1f, 0.1f);

    [Header("Glow")]
    private float GlowIntensity = 1.4f;
    private float GlowPulseSpeed = 1.8f;

    private Vector3 GroundPosition;
    private bool Settled = false;
    private Material Material;

    private void Awake()
    {
        if (OreRenderer != null)
        {
            Material = OreRenderer.material;
            Material.EnableKeyword("_EMISSION");
        }
    }

    public void StartJumpToGroundCoroutine()
    {
        StartCoroutine(JumpToGround());
    }
    public void StartMoveToGround()
    {
        StartCoroutine(MoveToGround());
    }
    private void Update()
    {
        if (Material != null)
        {
            float pulse = (Mathf.Sin(Time.time * GlowPulseSpeed) * 0.5f + 0.5f);
            Material.SetColor("_EmissionColor", GlowColor * (pulse * GlowIntensity));
        }

        if (!Settled) return;
        if (ItemPickUp.Moving) return;

        // Hover
        float hoverOffset = Mathf.Sin(Time.time * HoverSpeed) * HoverHeight;
        transform.position = new Vector3(transform.position.x, GroundPosition.y + hoverOffset, transform.position.z );

        // Rotate
        transform.Rotate(RotationAxis.normalized, RotationSpeed * Time.deltaTime, Space.World);



    }

    private IEnumerator JumpToGround()
    {
        Settled = false;

        // Pick a random point in a circle around the spawn origin
        Vector2 scatter = Random.insideUnitCircle * ScatterRadius;
        Vector3 startPos = transform.position;
        Vector3 targetGround = startPos + new Vector3(scatter.x, 0f, scatter.y);

        // Raycast down to find the actual ground level
        if (Physics.Raycast(targetGround + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, GroundMask))
            targetGround.y = hit.point.y + GroundYOffset;


        GroundPosition = targetGround;

        // Arc the ore from start to target
        float elapsed = 0f;
        while (elapsed < JumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / JumpDuration;

            // Lerp XZ, arc Y
            Vector3 current = Vector3.Lerp(startPos, targetGround, t);
            current.y += Mathf.Sin(t * Mathf.PI) * JumpHeight;
            transform.position = current;

            yield return null;
        }
      
        transform.position = targetGround;
        Settled = true;
    }
    private IEnumerator MoveToGround()
    {
        Settled = false;

        // Pick a random point in a circle around the spawn origin
        Vector2 scatter = Random.insideUnitCircle * ScatterRadius;
        Vector3 startPos = transform.position;
        Vector3 targetGround = startPos + new Vector3(scatter.x, 0f, scatter.y);

        // Raycast down to find the actual ground level
        if (Physics.Raycast(targetGround + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, GroundMask))
        {
            targetGround.y = hit.point.y + GroundYOffset;
        }

        GroundPosition = targetGround;

        float elapsed = 0f;

        while (elapsed < JumpDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / JumpDuration);

            transform.position = Vector3.Lerp(startPos, targetGround, t);

            yield return null;
        }

        transform.position = targetGround;
        Settled = true;
    }
}