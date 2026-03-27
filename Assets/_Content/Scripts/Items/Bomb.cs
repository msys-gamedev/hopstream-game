using UnityEngine;

public class Bomb : Item
{
    public int score = 100;
    public override void Interact()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeductScore(score);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake();
        }
    }
}