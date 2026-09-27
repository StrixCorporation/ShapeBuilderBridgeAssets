using Strix.ShapeBuilder.SplineBuilder008;
using System.ComponentModel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Strix.ShapeBuilder.Bridge
{
    [ExecuteAlways]
    [RequireComponent(typeof(BezierSpline), typeof(OutlineBezierShape))]
    public class Pillars : MonoBehaviour
    {
        const float ReferenceHeight = 8.487833f;
        const float MinHeight = 0.01f;

        [SerializeField, ReadOnly(true)] OutlineBezierShape outlineBezierShape;
        [SerializeField, ReadOnly(true)] BezierSpline bezierSpline;

        [SerializeField, Min(MinHeight)] float height = ReferenceHeight;
        [SerializeField, Min(1)] int referenceResolution = 50;
        [SerializeField] bool autoRefresh = true;
        [SerializeField] bool debounceTransformRefresh = true;
        [SerializeField, Min(0.0f)] float transformRefreshDelay = 0.15f;
        [SerializeField, Min(0.0f)] float heightRefreshThreshold = 0.01f;
        [SerializeField] bool disableOutlineAutoRefresh = true;
        [Header("Bridge Height")]
        [SerializeField, Min(MinHeight)] float minPillarsHeight = 1.0f;
        [SerializeField] float targetY = 0.0f;

        bool isRefreshing;
        Vector3 lastWorldPosition;
        bool hasLastWorldPosition;
        bool pendingTransformRefresh;
        double lastTransformChangeTime;

        public float Height
        {
            get => height;
            set
            {
                float clampedValue = Mathf.Max(MinHeight, value);
                if (Mathf.Approximately(height, clampedValue))
                    return;

                height = clampedValue;
                RefreshPillar();
            }
        }

        public int ReferenceResolution
        {
            get => referenceResolution;
            set
            {
                int clampedValue = Mathf.Max(1, value);
                if (referenceResolution == clampedValue)
                    return;

                referenceResolution = clampedValue;
                RefreshPillar();
            }
        }

        public bool AutoRefresh
        {
            get => autoRefresh;
            set => autoRefresh = value;
        }

        void Reset()
        {
            RecordWorldPosition();
            RefreshPillar();
        }

        void OnEnable()
        {
            RecordWorldPosition();
        }

        void OnValidate()
        {
            height = Mathf.Max(MinHeight, height);
            referenceResolution = Mathf.Max(1, referenceResolution);

            if (autoRefresh)
                RefreshPillar();
        }

        void Update()
        {
            if (!autoRefresh)
                return;

            if (!hasLastWorldPosition)
            {
                RecordWorldPosition();
                return;
            }

            if ((transform.position - lastWorldPosition).sqrMagnitude <= 0.000001f)
                return;

            if (debounceTransformRefresh)
            {
                pendingTransformRefresh = true;
                lastWorldPosition = transform.position;
                lastTransformChangeTime = GetRefreshTime();
                return;
            }

            RefreshPillarIfHeightChanged();
        }

        void LateUpdate()
        {
            if (!autoRefresh || !pendingTransformRefresh)
                return;

            if (GetRefreshTime() - lastTransformChangeTime < transformRefreshDelay)
                return;

            pendingTransformRefresh = false;
            RefreshPillarIfHeightChanged();
        }

        void RefreshPillarIfHeightChanged()
        {
            float refreshedHeight = CalculateHeightFromBridge();
            if (Mathf.Abs(refreshedHeight - height) <= heightRefreshThreshold)
            {
                RecordWorldPosition();
                return;
            }

            RefreshPillar();
        }

        public void RefreshPillar()
        {
            if (isRefreshing)
                return;

            isRefreshing = true;

            EnsureShape();
            RefreshHeightFromBridge();
            ApplySplineHeight();
            ApplyOutlineSettings();
            outlineBezierShape.RequestRefreshMesh();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(outlineBezierShape);
                EditorUtility.SetDirty(bezierSpline);
            }
#endif

            isRefreshing = false;
            RecordWorldPosition();
        }

        public void RefreshHeightFromBridge()
        {
            height = CalculateHeightFromBridge();
        }

        float CalculateHeightFromBridge()
        {
            Vector3 position = transform.position;
            return Mathf.Max(Mathf.Abs(position.y - targetY), minPillarsHeight);
        }

        void RecordWorldPosition()
        {
            lastWorldPosition = transform.position;
            hasLastWorldPosition = true;
        }

        void EnsureShape()
        {
            if (outlineBezierShape == null)
                outlineBezierShape = GetComponent<OutlineBezierShape>();

            if (outlineBezierShape == null)
                outlineBezierShape = gameObject.AddComponent<OutlineBezierShape>();

            if (disableOutlineAutoRefresh)
                outlineBezierShape.AutoGenerateOnChange = false;

            if (bezierSpline == null)
                bezierSpline = GetComponent<BezierSpline>();

            if (bezierSpline == null)
                bezierSpline = gameObject.AddComponent<BezierSpline>();
        }

        void ApplySplineHeight()
        {
            if (bezierSpline.SplineCount != 2)
            {
                bezierSpline.Clear();
                bezierSpline.AddAnchor(Vector3.zero);
                bezierSpline.AddAnchor(Vector3.up * -height);
            }
            else
            {
                bezierSpline.UpdateLocalPositionOfAnchor(Vector3.zero, 0);
                bezierSpline.UpdateLocalPositionOfAnchor(Vector3.up * -height, 1);
            }

            bezierSpline.Looped = false;
            bezierSpline.InTengants = new[] { Vector3.zero, Vector3.zero };
            bezierSpline.OutTengant = new[] { Vector3.zero, Vector3.zero };
            bezierSpline.TengantMode = new[] { ETangentMode.LINEAR, ETangentMode.LINEAR };
        }

        void ApplyOutlineSettings()
        {
            outlineBezierShape.AutoComputeResolution = false;
            outlineBezierShape.Resolution = Mathf.Max(1, Mathf.RoundToInt(referenceResolution * height / ReferenceHeight));
            outlineBezierShape.AnimationCurveScaleOffsetX = CreateScaleOffsetXCurve();
            outlineBezierShape.AnimationCurveScaleOffsetY = CreateScaleOffsetYCurve();
        }

        static double GetRefreshTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return EditorApplication.timeSinceStartup;
#endif
            return Time.realtimeSinceStartupAsDouble;
        }

    AnimationCurve CreateScaleOffsetXCurve()
    {
        return CreatePillarCurve(
            new Keyframe(0.0f, 8.0f),
            new Keyframe(0.08f, 8.0f),
            new Keyframe(0.22f, 3.0f),
            new Keyframe(0.88f, 3.0f),
            new Keyframe(0.95f, 8.0f),
            new Keyframe(1.0f, 8.0f));
    }

    AnimationCurve CreateScaleOffsetYCurve()
    {
        return CreatePillarCurve(
            new Keyframe(0.0f, 3.0f),
            new Keyframe(0.08f, 3.0f),
            new Keyframe(0.22f, 3.0f),
            new Keyframe(0.88f, 3.0f),
            new Keyframe(0.95f, 8.0f),
            new Keyframe(1.0f, 8.0f));
    }

        AnimationCurve CreatePillarCurve(params Keyframe[] referenceKeys)
        {
            Keyframe[] keys = new Keyframe[referenceKeys.Length];

            for (int i = 0; i < referenceKeys.Length; i++)
            {
                Keyframe key = referenceKeys[i];
                key.time = RemapReferenceTime(key.time);
                key.inTangent = 0.0f;
                key.outTangent = 0.0f;
                keys[i] = key;
            }

            EnsureIncreasingKeyTimes(keys);

            AnimationCurve curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };

            return curve;
        }

        float RemapReferenceTime(float referenceTime)
        {
            float ratio = ReferenceHeight / height;

            if (referenceTime <= 0.5f)
                return Mathf.Clamp01(referenceTime * ratio);

            return Mathf.Clamp01(1.0f - ((1.0f - referenceTime) * ratio));
        }

        static void EnsureIncreasingKeyTimes(Keyframe[] keys)
        {
            const float minStep = 0.0001f;

            for (int i = 1; i < keys.Length; i++)
            {
                if (keys[i].time <= keys[i - 1].time)
                    keys[i].time = Mathf.Min(1.0f, keys[i - 1].time + minStep);
            }
        }
    }
}

