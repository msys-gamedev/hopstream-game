using System;
using UnityEngine;

public class TrashPortal : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Item item))
        {
            item.DestroyTrashItem();
        }
    }
}
