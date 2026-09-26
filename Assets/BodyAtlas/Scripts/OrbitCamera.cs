using UnityEngine;

namespace BodyAtlas
{
    /// <summary>
    /// Premium damped orbit camera: inertia, pitch clamp, pinch-zoom,
    /// two-finger pan, tap-vs-drag threshold so selection doesn't fight orbit.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);

        [Header("Orbit")]
        public float yaw = 25f;
        public float pitch = 25f;
        public float distance = 2.4f;
        public float minDistance = 0.35f;
        public float maxDistance = 6.5f;
        public float minPitch = 10f;
        public float maxPitch = 80f;
        public float rotateSensitivity = 0.18f;
        public float zoomSensitivity = 0.0045f;
        public float panSensitivity = 0.0018f;
        public float damping = 12f;
        public float inertiaDecay = 4.5f;
        public float tapMoveThresholdPx = 14f;
        public float tapTimeThreshold = 0.28f;

        [Header("Reset")]
        public float resetYaw = 25f;
        public float resetPitch = 25f;
        public float resetDistance = 2.4f;
        public Vector3 resetOffset = new Vector3(0f, 1.0f, 0f);

        float _vyaw, _vpitch, _vzoom;
        Vector3 _vpan;
        Vector3 _smoothPos;
        bool _dragging;
        bool _pinching;
        bool _panning;
        bool _movedBeyondTap;
        float _touchStartTime;
        Vector2 _touchStartPos;
        float _pinchStartDist;
        float _pinchStartDistance;
        Vector2 _panStartMid;
        Vector3 _panStartOffset;
        int _activeFinger = -1;
        bool _consumeNextTap;
        bool _focusAnimating;
        float _focusT;
        Vector3 _focusFromOffset, _focusToOffset;
        float _focusFromYaw, _focusToYaw, _focusFromPitch, _focusToPitch;
        float _focusFromDist, _focusToDist;
        float _focusDuration = 0.85f;

        public bool IsInteracting => _dragging || _pinching || _panning || _focusAnimating;
        public bool WasTap { get; private set; }
        public Vector2 LastTapScreenPos { get; private set; }

        void Start()
        {
            if (target == null)
            {
                var go = new GameObject("OrbitTarget");
                target = go.transform;
                target.position = Vector3.zero;
            }
            resetOffset = targetOffset;
            ApplyImmediate();
            _smoothPos = transform.position;
        }

        void LateUpdate()
        {
            WasTap = false;
            HandleInput(Time.unscaledDeltaTime);
            if (_focusAnimating)
                StepFocus(Time.unscaledDeltaTime);
            else
                StepInertia(Time.unscaledDeltaTime);

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = target.position + targetOffset + rot * (Vector3.back * distance);
            float follow = 1f - Mathf.Exp(-damping * Time.unscaledDeltaTime);
            _smoothPos = Vector3.Lerp(_smoothPos, desired, follow);
            transform.position = _smoothPos;
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, follow);
            transform.LookAt(target.position + targetOffset);
        }

        void HandleInput(float dt)
        {
            // Mouse / editor
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _dragging = true;
                    _movedBeyondTap = false;
                    _touchStartTime = Time.unscaledTime;
                    _touchStartPos = Input.mousePosition;
                    _activeFinger = 0;
                }
                if (_dragging && Input.GetMouseButton(0))
                {
                    Vector2 cur = Input.mousePosition;
                    Vector2 delta = cur - _touchStartPos;
                    if (delta.magnitude > tapMoveThresholdPx) _movedBeyondTap = true;
                    if (_movedBeyondTap)
                    {
                        _vyaw += delta.x * rotateSensitivity * 0.35f / Mathf.Max(dt, 0.008f) * 0.016f;
                        _vpitch -= delta.y * rotateSensitivity * 0.35f / Mathf.Max(dt, 0.008f) * 0.016f;
                        // Use frame delta for smoother feel
                        Vector2 frame = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                        _vyaw = frame.x * rotateSensitivity * 60f;
                        _vpitch = -frame.y * rotateSensitivity * 60f;
                        yaw += _vyaw;
                        pitch += _vpitch;
                        _touchStartPos = cur;
                    }
                }
                if (Input.GetMouseButtonUp(0) && _dragging)
                {
                    float held = Time.unscaledTime - _touchStartTime;
                    if (!_movedBeyondTap && held <= tapTimeThreshold && !_consumeNextTap)
                    {
                        WasTap = true;
                        LastTapScreenPos = Input.mousePosition;
                    }
                    _dragging = false;
                    _consumeNextTap = false;
                }
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _vzoom = -scroll * 0.35f;
                    distance += _vzoom;
                }
                return;
            }

            // Touch
            if (Input.touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _dragging = true;
                    _pinching = false;
                    _panning = false;
                    _movedBeyondTap = false;
                    _touchStartTime = Time.unscaledTime;
                    _touchStartPos = t.position;
                    _activeFinger = t.fingerId;
                }
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    if (_dragging && !_pinching && !_panning)
                    {
                        Vector2 delta = t.position - _touchStartPos;
                        if (delta.magnitude > tapMoveThresholdPx) _movedBeyondTap = true;
                        if (_movedBeyondTap)
                        {
                            _vyaw = t.deltaPosition.x * rotateSensitivity;
                            _vpitch = -t.deltaPosition.y * rotateSensitivity;
                            yaw += _vyaw;
                            pitch += _vpitch;
                        }
                    }
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    float held = Time.unscaledTime - _touchStartTime;
                    if (_dragging && !_movedBeyondTap && held <= tapTimeThreshold && !_pinching && !_panning && !_consumeNextTap)
                    {
                        WasTap = true;
                        LastTapScreenPos = t.position;
                    }
                    _dragging = false;
                    _consumeNextTap = false;
                }
            }
            else if (Input.touchCount >= 2)
            {
                _movedBeyondTap = true;
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);
                Vector2 mid = (t0.position + t1.position) * 0.5f;
                float pinch = Vector2.Distance(t0.position, t1.position);

                // Classify: dominant pinch vs pan by relative motion
                Vector2 d0 = t0.deltaPosition;
                Vector2 d1 = t1.deltaPosition;
                float sep = Vector2.Dot(d0.normalized, d1.normalized);

                if (!_pinching && !_panning)
                {
                    _pinchStartDist = pinch;
                    _pinchStartDistance = distance;
                    _panStartMid = mid;
                    _panStartOffset = targetOffset;
                    // Prefer pinch if fingers move apart/together
                    if (sep < -0.2f || Mathf.Abs(pinch - _pinchStartDist) > 18f)
                        _pinching = true;
                    else
                        _panning = true;
                }

                if (_pinching || Mathf.Abs(pinch - _pinchStartDist) > Mathf.Abs((mid - _panStartMid).magnitude) * 0.6f)
                {
                    _pinching = true;
                    _panning = false;
                    float ratio = _pinchStartDist / Mathf.Max(pinch, 1f);
                    distance = Mathf.Clamp(_pinchStartDistance * ratio, minDistance, maxDistance);
                    _vzoom = 0f;
                }
                else
                {
                    _panning = true;
                    _pinching = false;
                    Vector2 delta = mid - _panStartMid;
                    Vector3 right = transform.right;
                    Vector3 up = transform.up;
                    float scale = distance * panSensitivity;
                    targetOffset = _panStartOffset - (right * delta.x + up * delta.y) * scale;
                }

                _dragging = false;
            }
            else
            {
                _pinching = false;
                _panning = false;
            }
        }

        void StepInertia(float dt)
        {
            if (_dragging || _pinching || _panning) return;
            float decay = Mathf.Exp(-inertiaDecay * dt);
            _vyaw *= decay;
            _vpitch *= decay;
            _vzoom *= decay;
            _vpan *= decay;
            if (Mathf.Abs(_vyaw) > 0.001f) yaw += _vyaw * dt * 60f;
            if (Mathf.Abs(_vpitch) > 0.001f) pitch += _vpitch * dt * 60f;
            if (Mathf.Abs(_vzoom) > 0.0001f) distance += _vzoom * dt * 60f;
            if (_vpan.sqrMagnitude > 1e-8f) targetOffset += _vpan * dt;
            // Kill tiny residual so we never "wild continuous spin"
            if (Mathf.Abs(_vyaw) < 0.02f) _vyaw = 0f;
            if (Mathf.Abs(_vpitch) < 0.02f) _vpitch = 0f;
        }

        void StepFocus(float dt)
        {
            _focusT += dt / _focusDuration;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_focusT));
            yaw = Mathf.LerpAngle(_focusFromYaw, _focusToYaw, u);
            pitch = Mathf.Lerp(_focusFromPitch, _focusToPitch, u);
            distance = Mathf.Lerp(_focusFromDist, _focusToDist, u);
            targetOffset = Vector3.Lerp(_focusFromOffset, _focusToOffset, u);
            _vyaw = _vpitch = _vzoom = 0f;
            if (_focusT >= 1f) _focusAnimating = false;
        }

        public void ResetView()
        {
            AnimateTo(resetYaw, resetPitch, resetDistance, resetOffset, 0.7f);
        }

        public void AnimateTo(float toYaw, float toPitch, float toDist, Vector3 toOffset, float duration = 0.85f)
        {
            _focusFromYaw = yaw;
            _focusFromPitch = pitch;
            _focusFromDist = distance;
            _focusFromOffset = targetOffset;
            _focusToYaw = toYaw;
            _focusToPitch = Mathf.Clamp(toPitch, minPitch, maxPitch);
            _focusToDist = Mathf.Clamp(toDist, minDistance, maxDistance);
            _focusToOffset = toOffset;
            _focusDuration = Mathf.Max(0.15f, duration);
            _focusT = 0f;
            _focusAnimating = true;
            _vyaw = _vpitch = _vzoom = 0f;
        }

        public void FocusWorldPoint(Vector3 worldPoint, float dist = 0.9f, float toYaw = 20f, float toPitch = 28f)
        {
            Vector3 offset = worldPoint - (target != null ? target.position : Vector3.zero);
            AnimateTo(toYaw, toPitch, dist, offset, 0.9f);
        }

        public void ConsumeTap() { _consumeNextTap = true; WasTap = false; }

        void ApplyImmediate()
        {
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = (target ? target.position : Vector3.zero) + targetOffset + rot * (Vector3.back * distance);
            transform.position = desired;
            transform.LookAt((target ? target.position : Vector3.zero) + targetOffset);
            _smoothPos = transform.position;
        }
    }
}
