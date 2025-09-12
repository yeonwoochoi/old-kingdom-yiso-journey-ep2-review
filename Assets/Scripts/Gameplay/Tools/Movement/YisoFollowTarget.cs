using System.Collections;
using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Tools.Movement {
    /// <summary>
    /// Add this component to an object and it'll get moved towards the target at update, with or without interpolation based on your settings
    /// </summary>
    [AddComponentMenu("Yiso/Tools/Movement/FollowTarget")]
    public class YisoFollowTarget : RunIBehaviour {
        public enum UpdateModes {
            Update,
            FixedUpdate,
            LateUpdate
        }

        public enum PositionSpaces {
            World,
            Local
        }

        [Title("Settings")] public bool initializeOnStart = true;
        public bool initializeOnEnable = false;

        [Title("Follow Position")] public bool followPosition = true;
        [ShowIf("followPosition")] public bool followPositionX = true;
        [ShowIf("followPosition")] public bool followPositionY = true;
        [ShowIf("followPosition")] public bool followPositionZ = false;
        [ShowIf("followPosition")] public PositionSpaces positionSpaces = PositionSpaces.World;

        [Title("Follow Rotation")]
        public bool followRotation = true; // target의 rotation까지 Follow 할거냐 (원래는 position만 Follow하는 건데)

        [Title("Follow Scale")] public bool followScale = true;

        [Title("Target")] public bool followPlayer = false;
        [ShowIf("@!followPlayer")] public Transform target;
        public Vector3 offset;

        [Title("Interpolation Position")] public bool interpolatePosition = true; // Position Follow 시 보간할거냐
        public float followPositionSpeed = 10f;

        [Title("Interpolation Rotation")] public bool interpolateRotation = true; // Rotation Follow 시 보간할거냐
        public float followRotationSpeed = 10f;

        [Title("Interpolation Scale")] public bool interpolateScale = true; // Scale Follow 시 보간할거냐
        public float followScaleSpeed = 10f;

        [Title("Mode")] public UpdateModes updateMode = UpdateModes.Update;
        public bool disableSelfOnSetActiveFalse = false;

        [Title("Distance")] public bool useMinimumDistance = false;
        public float minimumDistance = 0.2f;
        public bool useMaximumDistance = false;
        public float maximumDistance = 5f;

        protected Vector3 velocity = Vector3.zero;
        protected Vector3 newTargetPosition;
        protected Quaternion newTargetRotation;
        protected Vector3 newTargetScale;
        protected Vector3 initialPosition;
        protected Vector3 direction;

        #region Initialization

        protected override void Start() {
            if (initializeOnStart) Initialization();
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (initializeOnEnable) Initialization();
        }

        public virtual void Initialization() {
            GetTarget();
            SetInitialPosition();
        }

        protected virtual void GetTarget() {
            if (!followPlayer) return;

            StartCoroutine(GetTargetRoutine());
        }

        protected virtual IEnumerator GetTargetRoutine() {
            // TODO: Game Manager 로부터 Player GameObject 참조하기.
            yield return null;
        }

        protected virtual void SetInitialPosition() {
            initialPosition = positionSpaces == PositionSpaces.World
                ? transform.position
                : transform.localPosition;
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (disableSelfOnSetActiveFalse) {
                enabled = false;
            }
        }

        #endregion

        #region Public API

        public virtual void StartFollowing() {
            followPosition = true;
            SetInitialPosition();
        }

        public virtual void StopFollowing() {
            followPosition = false;
        }

        public virtual void ChangeFollowTarget(Transform newTarget) {
            target = newTarget;
        }

        #endregion

        #region Update

        public override void OnUpdate() {
            if (target == null) return;
            if (updateMode == UpdateModes.Update) {
                FollowTargetRotation();
                FollowTargetScale();
                FollowTargetPosition();
            }
        }

        public override void OnFixedUpdate() {
            if (target == null) return;
            if (updateMode == UpdateModes.FixedUpdate) {
                FollowTargetRotation();
                FollowTargetScale();
                FollowTargetPosition();
            }
        }

        public override void OnLateUpdate() {
            if (target == null) return;
            if (updateMode == UpdateModes.LateUpdate) {
                FollowTargetRotation();
                FollowTargetScale();
                FollowTargetPosition();
            }
        }

        #endregion

        #region Follow

        protected virtual void FollowTargetPosition() {
            if (target == null) return;
            if (!followPosition) return;

            newTargetPosition = target.position + offset;
            if (!followPositionX) newTargetPosition.x = initialPosition.x;
            if (!followPositionY) newTargetPosition.y = initialPosition.y;
            if (!followPositionZ) newTargetPosition.z = initialPosition.z;

            var trueDistance = Vector3.Distance(transform.position, newTargetPosition);
            direction = (newTargetPosition - transform.position).normalized;

            var interpolatedDistance = trueDistance;
            if (interpolatePosition) {
                interpolatedDistance = YisoMathUtils.Lerp(0f, trueDistance, followPositionSpeed, Time.deltaTime);
                interpolatedDistance = ApplyMinMaxDistancing(trueDistance, interpolatedDistance);
                transform.Translate(direction * interpolatedDistance, Space.World);
            }
            else {
                interpolatedDistance = ApplyMinMaxDistancing(trueDistance, interpolatedDistance);
                transform.Translate(direction * interpolatedDistance, Space.World);
            }
        }

        protected virtual void FollowTargetRotation() {
            if (target == null) return;
            if (!followRotation) return;

            newTargetRotation = target.rotation;

            transform.rotation = interpolateRotation
                ? YisoMathUtils.Lerp(transform.rotation, newTargetRotation, followRotationSpeed, Time.deltaTime)
                : newTargetRotation;
        }

        protected virtual void FollowTargetScale() {
            if (target == null) return;
            if (!followScale) return;

            newTargetScale = target.localScale;

            transform.localScale = interpolateScale
                ? YisoMathUtils.Lerp(this.transform.localScale, newTargetScale, followScaleSpeed, Time.deltaTime)
                : newTargetScale;
        }

        #endregion


        /// <summary>
        /// Applies minimal and maximal distance rules to the interpolated distance
        /// </summary>
        /// <param name="trueDistance"></param>
        /// <param name="interpolatedDistance"></param>
        /// <returns></returns>
        protected virtual float ApplyMinMaxDistancing(float trueDistance, float interpolatedDistance) {
            if (useMinimumDistance && (trueDistance - interpolatedDistance < minimumDistance)) {
                interpolatedDistance = 0f;
            }

            if (useMaximumDistance && (trueDistance - interpolatedDistance >= maximumDistance)) {
                interpolatedDistance = trueDistance - maximumDistance;
            }

            return interpolatedDistance;
        }
    }
}