using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractModule : MonoBehaviour
{
    public PlayerInventoryModule InventoryModule;

    [Header("Interact Settings")]
    public InputActionReference InteractInput;

    [SerializeField] private float InteractRange = 3f;
    [SerializeField] private float InteractCloseDist = 4f;
    [SerializeField] private LayerMask InteractLayerMask = ~0;


    private IInteractable CurrentInteractable;
    private Vector3 InteractablePosition;
    [HideInInspector] public bool IsInteracting;

    public void Init()
    {
        InventoryModule = GetComponent<PlayerInventoryModule>();
    }

    private void OnEnable()
    {
        InteractInput.action.performed += OnInteract;
        InteractInput.action.Enable();
    }

    private void OnDisable()
    {
        InteractInput.action.performed -= OnInteract;
        InteractInput.action.Disable();
    }

    private void Update()
    {
        if (IsInteracting && CurrentInteractable != null)
        {
            float dist = Vector3.Distance(transform.position, InteractablePosition);
            if (dist > InteractCloseDist)
            {
                CloseInteraction();
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {;
        if (IsInteracting)
        {
            CloseInteraction();
            return;
        }

        Camera cam = Camera.main;
        Ray ray = new(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, InteractRange, InteractLayerMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                CurrentInteractable = interactable;
                InteractablePosition = hit.collider.transform.position;
                IsInteracting = true;

                interactable.Interact(gameObject.GetComponent<NetworkObject>());
                InventoryModule.Open();
            }
        }
    }
    public void CloseInteraction()
    {
        if (CurrentInteractable != null)
        {
            CurrentInteractable.CloseInteraction();
            CurrentInteractable = null;
        }

        IsInteracting = false;

        if (InventoryModule != null)
        {
            InventoryModule.Close();
        }
    }
}