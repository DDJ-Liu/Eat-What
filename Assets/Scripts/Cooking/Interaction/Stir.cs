using System.Collections.Generic;
using UnityEngine;

public class Stir : InteractionController
{
    [Header("Drag Reference")]
    [SerializeField] private DragContainer dragContainer;
    [SerializeField] private MouseDraggableObject draggableObject;

    [Header("Distance Threshold")]
    [SerializeField] private float minTriggerDistance = 5f;

    [Header("Circular Detection")]
    [SerializeField] private int sampleBufferSize = 30;
    [SerializeField] private float sampleInterval = 0.05f;
    [Tooltip("累计转角低于此值时数据不足，不做判定")]
    [SerializeField] private float circularAngleThreshold = 270f;
    [Tooltip("|净旋转| / 累计转角 >= 此值视为圆形运动")]
    [SerializeField] private float windingRatioThreshold = 0.3f;

    [SerializeField] private float _accumulatedDistance;
    [SerializeField] private bool _triggered;
    [SerializeField] private bool _finished;
    private List<Vector3> _positionSamples = new List<Vector3>();
    private float _sampleTimer;

    public float AccumulatedDistance => _accumulatedDistance;
    public bool IsTriggered => _triggered;
    public bool IsFinished => _finished;

    protected override void Start()
    {
        base.Start();

        if (dragContainer != null)
        {
            dragContainer.onDragStartEvent.AddListener(OnDragStart);
            dragContainer.onDragPositionChanged.AddListener(OnDragPositionChanged);
        }
    }

    void Update()
    {
        if (draggableObject != null && draggableObject.dragging)
        {
            _sampleTimer += Time.deltaTime;
            if (_sampleTimer >= sampleInterval)
            {
                _sampleTimer = 0f;
                RecordSample(dragContainer.transform.position);
            }
        }
    }

    // ==================== Drag Callbacks ====================

    public void OnDragStart()
    {
        Debug.Log("[Stir] OnDragStart");
        _sampleTimer = 0f;
    }

    public void OnDragPositionChanged(Vector3 delta)
    {
        if (_finished) return;

        _accumulatedDistance += delta.magnitude;
        Debug.Log($"[Stir] OnDragPositionChanged | delta: {delta.magnitude:F3}, accumulated: {_accumulatedDistance:F3}/{minTriggerDistance}");

        if (!_triggered && _accumulatedDistance >= minTriggerDistance)
        {
            _triggered = true;
            Debug.Log("[Stir] Triggered — distance threshold reached");
            TriggerAction();
        }
    }

    // ==================== External Completion ====================

    /// <summary>
    /// 外部调用以标记交互完成（如动画事件、计时器等）。
    /// 仅在已触发后有效。
    /// </summary>
    public void CompleteInteraction()
    {
        if (!_triggered || _finished) return;
        _finished = true;
        Debug.Log("[Stir] CompleteInteraction — finished");
        /*ActionCompleted();*/
    }

    public override void ActionCompleted()
    {
        base.ActionCompleted();

        switch(stage)
        {
            case 1:
                CompleteInteraction();
                OnStirComplete();
                break;
        }
    }

    private void OnStirComplete()
    {
        Debug.Log("[Stir] OnStirComplete — calling onInteractionFinished");
        onInteractionFinished();
    }

    // ==================== Circular Motion Detection ====================

    private void RecordSample(Vector3 position)
    {
        _positionSamples.Add(position);
        if (_positionSamples.Count > sampleBufferSize)
        {
            _positionSamples.RemoveAt(0);
        }
    }

    /// <summary>
    /// 判断近期拖拽轨迹是否为圆形运动（而非直线来回）。
    /// 基于绕数算法：圆形运动的净旋转角度与累计转角之比较高，
    /// 直线来回的正负角度相互抵消导致比值接近零。
    /// </summary>
    public bool IsCircularMotion()
    {
        if (_positionSamples.Count < 3) return false;

        float cumulativeAbsAngle = 0f;
        float netSignedAngle = 0f;

        for (int i = 1; i < _positionSamples.Count - 1; i++)
        {
            Vector2 v1 = _positionSamples[i] - _positionSamples[i - 1];
            Vector2 v2 = _positionSamples[i + 1] - _positionSamples[i];

            if (v1.sqrMagnitude < 0.0001f || v2.sqrMagnitude < 0.0001f)
                continue;

            float signedAngle = Vector2.SignedAngle(v1, v2);
            cumulativeAbsAngle += Mathf.Abs(signedAngle);
            netSignedAngle += signedAngle;
        }

        if (cumulativeAbsAngle < circularAngleThreshold)
        {
            Debug.Log($"[Stir] IsCircularMotion — insufficient angle: {cumulativeAbsAngle:F1} < {circularAngleThreshold}");
            return false;
        }

        float ratio = Mathf.Abs(netSignedAngle) / cumulativeAbsAngle;
        Debug.Log($"[Stir] IsCircularMotion — net: {netSignedAngle:F1}, cumulative: {cumulativeAbsAngle:F1}, ratio: {ratio:F3} (threshold: {windingRatioThreshold})");
        return ratio >= windingRatioThreshold;
    }

    public void ResetCircularDetection()
    {
        _positionSamples.Clear();
        _sampleTimer = 0f;
    }
}
