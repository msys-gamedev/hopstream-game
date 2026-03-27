using UnityEngine;

public class BasePortal : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Item item))
        {
            item.Interact();
            item.DestroyBaseItem();
        }
    }
}
