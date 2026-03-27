using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Movement")]
    public float pushPower = 1;
    public float fallSpeed = 3f;

    [Tooltip("How quickly velocity changes direction (higher = snappier, lower = heavier).")]
    public float acceleration = 12f;

    [Tooltip("Seconds after spawn before this item can be pushed.")]
    public float pushDelay = 0.1f;

    [Tooltip("After this time, item can always be destroyed.")]
    public float destroyFallbackTime = 2f;

    [Header("VFX")]
    public Animator animator;
    public ParticleSystem dustParticle;

    [HideInInspector] public bool Push;

    private float lifeTimer;
    private bool canBePushed;
    private float velocity;
    private bool wasFalling;

    private void Start()
    {
        velocity = -fallSpeed;
        wasFalling = true;

        if (dustParticle != null)
            dustParticle.Play();
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        canBePushed = lifeTimer >= pushDelay;

        float targetVelocity = (canBePushed && Push) ? pushPower : -fallSpeed;
        velocity = Mathf.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);

        transform.Translate(Vector3.up * (velocity * Time.deltaTime));

        bool isFalling = velocity < 0f;

        if (dustParticle != null)
        {
            if (isFalling && !wasFalling)
                dustParticle.Play();
            else if (!isFalling && wasFalling)
                dustParticle.Stop();
        }

        wasFalling = isFalling;
    }

    public virtual void Interact()
    {
        
    }

    bool CanDestroy()
    {
        return canBePushed || lifeTimer >= destroyFallbackTime;
    }

    public void DestroyBaseItem()
    {
        if (!CanDestroy()) return;

        Destroy(gameObject);
    }

    public void DestroyTrashItem()
    {
        if (!CanDestroy()) return;
        enabled = false;
        animator.SetTrigger("Explode");
        Destroy(gameObject, 1f);
    }
}
