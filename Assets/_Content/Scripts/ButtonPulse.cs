using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Reusable button animation: idle pulse + press squish.
/// Just add this component to any Button GameObject.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonPulse : MonoBehaviour
{
    [Tooltip("How much the button scales up/down during the idle pulse.")]
    public float pulseAmount = 0.08f;
    [Tooltip("Speed of the idle pulse animation.")]
    public float pulseSpeed = 2f;
    [Tooltip("Scale multiplier when the button is pressed.")]
    public float pressScale = 0.85f;
    [Tooltip("How fast the button snaps to press/release scale.")]
    public float pressLerp = 12f;

    private Vector3 baseScale;
    private bool isPressed;

    private void Start()
    {
        baseScale = transform.localScale;

        var trigger = gameObject.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener(_ => isPressed = true);
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        pointerUp.callback.AddListener(_ => isPressed = false);
        trigger.triggers.Add(pointerUp);
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        Vector3 targetScale = baseScale * pulse;

        if (isPressed)
            targetScale = baseScale * pressScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * pressLerp
        );
    }
}
