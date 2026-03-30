using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Settings")]
    public int points;

    public float pushPower = 10f;
    public float speed = 3;
    public float minX = -8f;
    public float maxX = 8f;

    [Header("Shoot Settings")]
    public float hitDistance = 10;
    [Tooltip("Push strength at max distance as a fraction of full power (0.3 = 30%).")]
    [Range(0f, 1f)] public float pushFalloff = 0.3f;

    [Header("Water Trail")]
    public LineRenderer waterTrail;
    public EffectsManager effects;
    [Tooltip("Width of the water trail.")]
    public float trailWidth = 0.15f;

    [Header("Audio")]
    public AudioSource shootAudioSource;
    public AudioClip shootSound;

    [Header("Animation")]
    public Animator animator;

    private Camera mainCam;
    private RaycastHit2D lastHit;
    private bool hasHit;
    private Item currentItem;

    private Item[] pushedItem = new Item[16];
    private int pushedCount;
    private bool isShooting;
    private bool wasShooting;
    private float lastXPosition;

    public int PlayerPoint { get => points;
        set
        {
            points = value;
            OnPointChanged?.Invoke(points);
        }
    }

    public delegate void LifeChange(int _life);
    public static event LifeChange OnPointChanged;

    private void Start()
    {
        mainCam = Camera.main;
        lastXPosition = transform.position.x;

        if (waterTrail == null)
            waterTrail = GetComponentInChildren<LineRenderer>();

        if (waterTrail != null)
        {
            waterTrail.positionCount = 2;
            waterTrail.startWidth = trailWidth;
            waterTrail.endWidth = trailWidth * 2f;
            waterTrail.useWorldSpace = true;
            waterTrail.sortingOrder = 10;
            waterTrail.enabled = false;
            
            // Fallback material so the line is always visible
            if (waterTrail.material == null || waterTrail.material.shader.name == "Hidden/InternalErrorShader")
            {
                waterTrail.material = new Material(Shader.Find("Sprites/Default"));
                waterTrail.startColor = new Color(0.3f, 0.7f, 1f, 0.8f);
                waterTrail.endColor = new Color(0.3f, 0.7f, 1f, 0.3f);
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.StartGame)
        {
            StopPush();
            HideTrail();
            return;
        }
        isShooting = Input.GetMouseButton(0);

        if (isShooting)
        {
            Move();
            Shoot();
        }
        else
        {
            StopPush();
            HideTrail();
        }
        
        wasShooting = isShooting;

        bool isMoving = Mathf.Abs(transform.position.x - lastXPosition) > 0.001f;
        lastXPosition = transform.position.x;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private void Shoot()
    {
        for (int i = 0; i < pushedCount; i++)
        {
            if (pushedItem[i] != null)
                pushedItem[i].Push = false;
        }

        pushedCount = 0;
        hasHit = false;

        Vector2 origin = transform.position;
        var hits = Physics2D.RaycastAll(origin, Vector2.up, hitDistance);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
                continue;

            // Ignore self
            if (hits[i].collider.transform == transform)
                continue;

            if (hits[i].collider.TryGetComponent(out Item item))
            {
                // First valid item hit becomes trail target
                if (!hasHit)
                {
                    hasHit = true;
                    lastHit = hits[i];
                }

                item.Push = true;

                float distanceFactor = Mathf.Lerp(1f, pushFalloff, hits[i].distance / hitDistance);
                item.pushPower = pushPower * distanceFactor;

                if (pushedCount < pushedItem.Length)
                    pushedItem[pushedCount++] = item;
            }
        }

        UpdateTrail();
        
        if (!wasShooting)
        {
            effects.EnableParticle();
            StartShootSound();
        }

        Debug.DrawRay(origin, Vector2.up * (hasHit ? lastHit.distance : hitDistance),
            hasHit ? Color.red : Color.green);
    }

    private void Move()
    {
        if (mainCam == null) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(mousePos);
        if(minX < mouseWorld.x)return;
        if(maxX > mouseWorld.x)return;
        
        transform.position = new Vector3(mouseWorld.x, transform.position.y, transform.position.z);
    }

    private void StopPush()
    {
        for (int i = 0; i < pushedCount; i++)
            if (pushedItem[i] != null)
                pushedItem[i].Push = false;

        pushedCount = 0;
        hasHit = false;
    }

    private void UpdateTrail()
    {
        if (waterTrail == null) return;

        waterTrail.enabled = true;

        Vector3 start = waterTrail.transform.position;
        start.z = - -0.1f;

        Vector3 end = hasHit
            ? new Vector3(lastHit.point.x, lastHit.point.y, -0.1f)
            : start + new Vector3(0f, hitDistance, 0f);

        waterTrail.SetPosition(0, start);
        waterTrail.SetPosition(1, end);
        effects.startVfx.transform.position = start;
        effects.endVfx.transform.position = end;
    }

    private void HideTrail()
    {
        if (waterTrail != null)
            waterTrail.enabled = false;
        if (wasShooting)
        {
            effects.DisableParticle();
            StopShootSound();
        }
    }

    private void StartShootSound()
    {
        if (shootAudioSource != null && shootSound != null && !shootAudioSource.isPlaying)
        {
            shootAudioSource.clip = shootSound;
            shootAudioSource.loop = true;
            shootAudioSource.Play();
        }
    }

    private void StopShootSound()
    {
        if (shootAudioSource != null && shootAudioSource.isPlaying)
            shootAudioSource.Stop();
    }

    private void OnDrawGizmos()
    {
        Vector3 start = transform.position;

        if (hasHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, start + Vector3.up * lastHit.distance);
            Gizmos.DrawWireSphere(lastHit.point, 0.25f);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, start + Vector3.up * hitDistance);
        }
    }
}
