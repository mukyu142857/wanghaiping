using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField, Min(1f)] float mapLength = 320f;
    [SerializeField, Range(0.1f, 1f)] float visibleFraction = 1f / 3f;
    [SerializeField, Min(0f)] float panSpeed = 35f;
    Camera cameraComponent;

    void Awake() => cameraComponent = GetComponent<Camera>();

    void LateUpdate()
    {
        if (cameraComponent == null) cameraComponent = GetComponent<Camera>();
        const float hudHeightFraction = 330f / 1080f;
        cameraComponent.rect = new Rect(0f, hudHeightFraction, 1f, 1f - hudHeightFraction);
        float halfVisibleWidth = mapLength * visibleFraction * 0.5f;
        cameraComponent.orthographic = true;
        float visibleAspect = Screen.width / Mathf.Max(1f, Screen.height * (1f - hudHeightFraction));
        cameraComponent.orthographicSize = halfVisibleWidth / visibleAspect;

        float direction = 0f;
        if (Input.GetKey(KeyCode.A)) direction -= 1f;
        if (Input.GetKey(KeyCode.D)) direction += 1f;

        float limit = Mathf.Max(0f, mapLength * 0.5f - halfVisibleWidth);
        var position = transform.position;
        position.x = Mathf.Clamp(position.x + direction * panSpeed * Time.deltaTime, -limit, limit);
        transform.position = position;
    }
}
