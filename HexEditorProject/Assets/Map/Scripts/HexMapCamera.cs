using UnityEngine;

/// <summary>
/// Component that controls the singleton camera that navigates the hex map.
/// </summary>
public class HexMapCamera : MonoBehaviour
{
    [SerializeField]
    float stickMinZoom, stickMaxZoom;

    [SerializeField]
    float swivelMinZoom, swivelMaxZoom;

    [SerializeField]
    float moveSpeedMinZoom, moveSpeedMaxZoom;

    [SerializeField]
    float rotationSpeed;

    [SerializeField]
    HexGrid grid;

    Transform swivel, stick;

    [SerializeField]
    Vector3 focusCameraPos = new Vector3(0, 5, 0);

    [SerializeField]
    Vector3 focusCameraAngle = new Vector3(-10, 41, 0);



    float zoom = 1f;

    //float rotationAngle;

    static HexMapCamera instance;

    /// <summary>
    /// Whether the singleton camera controls are locked.
    /// </summary>
    public static bool Locked
    {
        set => instance.enabled = !value;
    }

    /// <summary>
    /// Validate the position of the singleton camera.
    /// </summary>
    public static void ValidatePosition() => instance.AdjustPosition(0f, 0f);

    void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void OnEnable()
    {
        instance = this;
        ValidatePosition();
    }

    private Vector3 focusPoint;
    private Vector3 targetFaceDir;


    void AdjustRotation(Vector3 face)
    {
        face = face * rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(transform.eulerAngles + face);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            var root = GameObject.Find("Hex Grid");
            if (root)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    transform.position = hit.point + focusCameraPos;
                    transform.LookAt(root.transform);
                }
                else
                {
                    transform.position = focusCameraPos * 1.5f;
                    transform.eulerAngles = focusCameraAngle;
                }
            }
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            if (focusPoint != Input.mousePosition && focusPoint != Vector3.zero)
            {
                var deltaX = Input.mousePosition.x - focusPoint.x;
                var deltaY = Input.mousePosition.y - focusPoint.y;
                targetFaceDir.x = -deltaY;
                targetFaceDir.y = deltaX;
                AdjustRotation(targetFaceDir);
            }

            focusPoint = Input.mousePosition;
        }
        else
        {
            focusPoint = Vector3.zero;
        }

        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float yDelta = Input.GetAxis("Rotation");
        if (yDelta != 0)
        {
            //AdjustRotation(rotationDelta);
            AdjustPosition(0, 0, yDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }


    void AdjustPosition(float xDelta, float zDelta, float yDelta = 0)
    {
        Vector3 direction =
            transform.localRotation *
            new Vector3(xDelta, yDelta, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(yDelta), Mathf.Abs(zDelta)); //xiaonian：Y轴移动

        float distance =
            Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
            damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition =
            grid.Wrapping ? WrapPosition(position) : ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.CellCountX - 0.5f) * HexMetrics.innerDiameter;
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }

    Vector3 WrapPosition(Vector3 position)
    {
        float width = grid.CellCountX * HexMetrics.innerDiameter;
        while (position.x < 0f)
        {
            position.x += width;
        }
        while (position.x > width)
        {
            position.x -= width;
        }

        float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        grid.CenterMap(position.x);
        return position;
    }
}
