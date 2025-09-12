using System.Collections;
using Core.Behaviour;
using Settings;
using Sirenix.OdinInspector;
using Tools.Movement;
using UI.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils.Beagle;

namespace Gameplay.Health.GUI {
    public class YisoHealthBar : RunIBehaviour {
        [Title("Setting")]
        public bool initializeOnAwake = false; // Yiso Character 가 있는 경우엔 false로 하고 외부에서 Initialization 시키기

        [Title("Drawn Setting")] public Vector2 size = new(1.2f, 0.3f);
        public Vector2 backgroundPadding = new(0.05f, 0.05f);
        public Vector3 initialRotationAngles = Vector3.zero; // health Bar 수직으로 하고 싶으면 Vector(0, 0, 90f)

        // Health Bar 색
        public Gradient foregroundColor = new Gradient() {
            colorKeys = new GradientColorKey[2] {
                new GradientColorKey(YisoColorUtils.BestRed, 0),
                new GradientColorKey(YisoColorUtils.BestRed, 1f)
            },
            alphaKeys = new GradientAlphaKey[2] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        // 데미지 -> 감소한 Health만큼 Delayed Color로 바뀜 -> 몇 초후 감소
        public Gradient delayedColor = new Gradient() {
            colorKeys = new GradientColorKey[2] {
                new GradientColorKey(YisoColorUtils.Orange, 0),
                new GradientColorKey(YisoColorUtils.Orange, 1f)
            },
            alphaKeys = new GradientAlphaKey[2] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        public Gradient borderColor = new Gradient() {
            colorKeys = new GradientColorKey[2] {
                new GradientColorKey(YisoColorUtils.Black, 0),
                new GradientColorKey(YisoColorUtils.Black, 1f)
            },
            alphaKeys = new GradientAlphaKey[2] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        public Gradient backgroundColor = new Gradient() {
            colorKeys = new GradientColorKey[2] {
                new GradientColorKey(YisoColorUtils.DarkDarkRed, 0),
                new GradientColorKey(YisoColorUtils.DarkDarkRed, 1f)
            },
            alphaKeys = new GradientAlphaKey[2] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        public string sortingLayerName = GameLayers.UISortingName;
        public float delay = 0.5f; // Delay Bar 적용 시간
        public bool lerpFrontBar = true; // false : 스타카토처럼 피 깎임, true : 연속적으로 피 깎임
        public float lerpFrontBarSpeed = 15f;
        public bool lerpDelayedBar = true;
        public float lerpDelayedBarSpeed = 15f;
        public bool bumpScaleOnChange = true;
        public float bumpDuration = 0.2f;
        public AnimationCurve bumpAnimationCurve = AnimationCurve.Constant(0, 1, 1);

        [Title("Offset")] public Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

        [Title("Display")] public bool alwaysVisible = false;
        public float displayDurationOnHit = 1f; // 맞았을때 Health Bar 몇초 보여줄건지
        public bool hideBarAtZero = true; // 피 0일때 Health Bar 감출건지
        public float hideBarAtZeroDelay = 1f; // 피 0되고 몇초동안 Delay 됐다가 감출건지

        protected YisoProgressBar progressBar;
        protected YisoFollowTarget followTransform;
        protected float lastShowTimestamp = 0f;

        protected Image backgroundImage = null;
        protected Image borderImage = null;
        protected Image foregroundImage = null;
        protected Image delayedImage = null;

        protected bool initialized = false;
        protected bool showBar = false;
        protected bool finalHideStarted = false;

        #region Initialize

        protected override void Awake() {
            if (initializeOnAwake) {
                Initialization();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            finalHideStarted = false;
            SetInitialActiveState();
        }

        /// <summary>
        /// Revive하는 경우엔 외부에서 call
        /// </summary>
        public virtual void Initialization() {
            finalHideStarted = false;

            // Revive 하는 경우겠지
            if (progressBar != null) {
                initialized = true;
                SetInitialActiveState();
                return;
            }

            DrawHealthBar();
            UpdateDrawnColors();

            initialized = true;

            SetInitialActiveState();
            if (progressBar != null) {
                // 100%라고 생각하면 됨
                progressBar.SetBar(100f, 0f, 100f);
            }
        }

        #endregion

        #region Draw

        protected virtual void DrawHealthBar() {
            // 가장 상위 Health Bar Object 생성
            var healthBarObj = new GameObject();
            SceneManager.MoveGameObjectToScene(healthBarObj, gameObject.scene);
            healthBarObj.name = $"HealthBar|{gameObject.name}";

            // Health Bar -> Add YisoProgressBar and Initial Setting
            progressBar = healthBarObj.AddComponent<YisoProgressBar>();

            // Health Bar -> Add YisoFollowTarget and Initial Setting
            followTransform = healthBarObj.AddComponent<YisoFollowTarget>();
            followTransform.target = transform;
            followTransform.offset = healthBarOffset;
            followTransform.followRotation = false;
            followTransform.interpolatePosition = false;
            followTransform.interpolateRotation = false;
            followTransform.updateMode = YisoFollowTarget.UpdateModes.LateUpdate;

            // Health Bar -> Add Canvas and Initial Setting
            var newCanvas = healthBarObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.WorldSpace;
            newCanvas.transform.localScale = Vector3.one;
            newCanvas.GetComponent<RectTransform>().sizeDelta = size;
            newCanvas.sortingLayerName = sortingLayerName;


            // 바로 아래 하위 옵젝인 Container 생성 및 설정
            var container = new GameObject();
            container.transform.SetParent(healthBarObj.transform);
            container.name = "ProgressBarContainer";
            container.transform.localScale = Vector3.one;

            var borderImageGameObject = new GameObject();
            borderImageGameObject.transform.SetParent(container.transform);
            borderImageGameObject.name = "HealthBar Border";
            borderImage = borderImageGameObject.AddComponent<Image>();
            borderImage.transform.position = Vector3.zero;
            borderImage.transform.localScale = Vector3.one;
            borderImage.GetComponent<RectTransform>().sizeDelta = size;
            borderImage.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

            var bgImageGameObject = new GameObject();
            bgImageGameObject.transform.SetParent(container.transform);
            bgImageGameObject.name = "HealthBar Background";
            backgroundImage = bgImageGameObject.AddComponent<Image>();
            backgroundImage.transform.position = Vector3.zero;
            backgroundImage.transform.localScale = Vector3.one;
            backgroundImage.GetComponent<RectTransform>().sizeDelta = size - backgroundPadding * 2;
            backgroundImage.GetComponent<RectTransform>().anchoredPosition =
                -backgroundImage.GetComponent<RectTransform>().sizeDelta / 2;
            backgroundImage.GetComponent<RectTransform>().pivot = Vector2.zero;

            var delayedImageGameObject = new GameObject();
            delayedImageGameObject.transform.SetParent(container.transform);
            delayedImageGameObject.name = "HealthBar Delayed Foreground";
            delayedImage = delayedImageGameObject.AddComponent<Image>();
            delayedImage.transform.position = Vector3.zero;
            delayedImage.transform.localScale = Vector3.one;
            delayedImage.GetComponent<RectTransform>().sizeDelta = size - backgroundPadding * 2;
            delayedImage.GetComponent<RectTransform>().anchoredPosition =
                -delayedImage.GetComponent<RectTransform>().sizeDelta / 2;
            delayedImage.GetComponent<RectTransform>().pivot = Vector3.zero;

            var frontImageGameObject = new GameObject();
            frontImageGameObject.transform.SetParent(container.transform);
            frontImageGameObject.name = "HealthBar Foreground";
            foregroundImage = frontImageGameObject.AddComponent<Image>();
            foregroundImage.transform.position = Vector3.zero;
            foregroundImage.transform.localScale = Vector3.one;
            foregroundImage.color = foregroundColor.Evaluate(1);
            foregroundImage.GetComponent<RectTransform>().sizeDelta = size - backgroundPadding * 2;
            foregroundImage.GetComponent<RectTransform>().anchoredPosition =
                -foregroundImage.GetComponent<RectTransform>().sizeDelta / 2;
            foregroundImage.GetComponent<RectTransform>().pivot = Vector3.zero;

            progressBar.lerpDecreasingDelayedBar = lerpDelayedBar;
            progressBar.lerpForegroundBar = lerpFrontBar;
            progressBar.lerpDecreasingDelayedBarSpeed = lerpDelayedBarSpeed;
            progressBar.lerpForegroundBarSpeedIncreasing = lerpFrontBarSpeed;
            progressBar.foregroundBar = foregroundImage.transform;
            progressBar.delayedBarDecreasing = delayedImage.transform;
            progressBar.decreasingDelay = delay;
            progressBar.bumpOnChange = bumpScaleOnChange;
            progressBar.bumpDuration = bumpDuration;
            progressBar.bumpScaleAnimationCurve = bumpAnimationCurve;
            container.transform.localEulerAngles = initialRotationAngles;

            progressBar.Initialization();
        }

        #endregion

        #region Update

        /// <summary>
        /// On Update, we hide or show our health bar based on our current status
        /// </summary>
        public override void OnUpdate() {
            if (!initialized) return;
            if (progressBar == null) return;
            if (finalHideStarted) return;

            UpdateDrawnColors();

            if (alwaysVisible) return;
            if (showBar) {
                ShowBar(true);
                var currentTime = Time.unscaledTime;
                if (currentTime - lastShowTimestamp > displayDurationOnHit) {
                    showBar = false;
                }
            }
            else {
                if (IsBarShown()) {
                    ShowBar(false);
                }
            }
        }

        /// <summary>
        /// Health Bar를 Update하는 함수
        /// Update마다 호출되는 함수는 아님 (ex. Damage를 입었거나, 회복포션을 먹은 경우에 YisoHealth에서 호출)
        /// </summary>
        /// <param name="currentHealth"></param>
        /// <param name="minHealth"></param>
        /// <param name="maxHealth"></param>
        /// <param name="show"></param>
        public virtual void UpdateBar(float currentHealth, float minHealth, float maxHealth, bool show) {
            if (!alwaysVisible && show) {
                showBar = true;
                lastShowTimestamp = Time.unscaledTime;
            }

            if (progressBar != null) {
                progressBar.UpdateBar(currentHealth, minHealth, maxHealth);
                if (hideBarAtZero && currentHealth <= minHealth) {
                    StartCoroutine(FinalHideBar());
                }

                if (bumpScaleOnChange) {
                    progressBar.Bump();
                }
            }
        }

        /// <summary>
        /// 시간 흐름에 따라 Bar 색깔 변화
        /// Update마다 호출
        /// </summary>
        protected virtual void UpdateDrawnColors() {
            if (progressBar.Bumping) return;
            if (borderImage != null) borderImage.color = borderColor.Evaluate(progressBar.barProgress);
            if (backgroundImage != null) backgroundImage.color = backgroundColor.Evaluate(progressBar.barProgress);
            if (delayedImage != null) delayedImage.color = delayedColor.Evaluate(progressBar.barProgress);
            if (foregroundImage != null) foregroundImage.color = foregroundColor.Evaluate(progressBar.barProgress);
        }

        #endregion

        #region Hide

        protected virtual IEnumerator FinalHideBar() {
            finalHideStarted = true;
            if (hideBarAtZeroDelay == 0) {
                showBar = false;
                ShowBar(false);
                yield return null;
            }
            else {
                progressBar.HideBar(hideBarAtZeroDelay);
            }
        }

        #endregion

        #region Tool

        protected virtual void SetInitialActiveState() {
            if (!alwaysVisible && progressBar != null) {
                ShowBar(false);
            }
        }

        public virtual void ShowBar(bool state) {
            if (!initialized) Initialization();
            if (progressBar != null) {
                progressBar.gameObject.SetActive(state);
            }
        }

        public virtual bool IsBarShown() {
            return progressBar.gameObject.activeInHierarchy;
        }

        #endregion
    }
}