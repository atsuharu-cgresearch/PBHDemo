using UnityEngine;

/// <summary>
/// 注目点を中心に回転、ズームイン・ズームアウトするカメラ
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    // 注目する対象
    public Transform target;

    // 距離
    public float distance = 5.0f;
    public float minDistance = 2.0f;
    public float maxDistance = 15.0f;

    // 操作スピード
    public float orbitSpeed = 5.0f;
    public float zoomSpeed = 5.0f;

    // 自動モードのパラメータ
    [Header("自動モード")]
    public bool autoOrbit = false;
    public float autoOrbitSpeed = 30.0f;   // 1秒あたりの回転角度（度）
    public float autoOrbitRadius = 0.0f;   // 上下の揺れ幅（度）。0なら水平に回転
    public float autoOrbitRadiusSpeed = 0.5f; // 上下揺れの周期（Hz）

    private float autoOrbitTime = 0.0f;

    // 現在のカメラの角度を保持する変数
    private float currentX = 0.0f;
    private float currentY = 0.0f;

    // カメラが真上や真下に行き過ぎないための制限
    private float yMinLimit = -70f;
    private float yMaxLimit = 70f;

    // デバッグモード用の、注目点を上書きする為の変数
    private bool useOverrideFocus = false;
    private Vector3 overrideFocusPosition;
    private Vector3 currentPivotPos;
    public float transitionSpeed = 10.0f;

    void Start()
    {
        // 開始時のカメラの角度を取得しておく
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        if (target != null)
        {
            currentPivotPos = target.position;
        }
    }

    public void SetOverrideFocus(Vector3 pos)
    {
        useOverrideFocus = true;
        overrideFocusPosition = pos;
    }

    public void ClearOverrideFocus()
    {
        useOverrideFocus = false;
    }

    void LateUpdate()
    {
        // 回転の中心をどちらにするか決定する
        Vector3 targetPivotPos;
        // 上書きモードなら指定された座標を使用する
        if (useOverrideFocus)
        {
            targetPivotPos = overrideFocusPosition;
        }
        // 通常モードならデフォルトの座標を使用する
        else if(target != null)
        {
            targetPivotPos = target.position;
        }
        // どちらのターゲットもない場合は何もしない
        else
        {
            return;
        }

        currentPivotPos = Vector3.Lerp(currentPivotPos, targetPivotPos, transitionSpeed * Time.deltaTime);

        // 自動モード
        if (autoOrbit)
        {
            autoOrbitTime += Time.deltaTime;

            currentX = autoOrbitRadius * Mathf.Cos(autoOrbitTime * autoOrbitRadiusSpeed * Mathf.PI * 2f);
            currentY = autoOrbitRadius * Mathf.Sin(autoOrbitTime * autoOrbitRadiusSpeed * Mathf.PI * 2f);
            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
        }

        // 手動モード
        else
        {
            // 右クリック＋ドラッグで回転
            if (Input.GetMouseButton(0))
            {
                // マウスの移動量を取得して角度に足し引きする
                currentX += Input.GetAxis("Mouse X") * orbitSpeed;
                currentY -= Input.GetAxis("Mouse Y") * orbitSpeed;
                // 上下（Y軸）の回転を制限して、カメラがひっくり返らないようにする
                currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
            }

            // マウスホイールでズーム
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                // ホイールの回転量に応じて距離を変更
                distance -= scroll * zoomSpeed;
                // 距離を minDistance と maxDistance の間に制限する
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        // 角度から回転情報を計算
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        // ターゲットの位置から、計算した回転と距離の分だけ離れた位置にカメラを置く
        Vector3 dir = new Vector3(0, 0, -distance);
        transform.position = currentPivotPos + rotation * dir;
        transform.LookAt(currentPivotPos);
    }
}