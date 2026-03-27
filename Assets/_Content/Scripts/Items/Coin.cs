using TMPro;

public class Coin : Item
{
    public TextMeshPro scoreText;
    public int score = 0;

    public override void Interact()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(score);
        }
    }
}
