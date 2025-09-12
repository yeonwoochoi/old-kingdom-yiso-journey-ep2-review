using System.Collections;
using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;

namespace UI.Components
{
    public class YisoProgressBar : RunIBehaviour
    {
        public enum ProgressBarStates
        {
            Idle, 
            Decreasing, // Foreground Bar가 감소하고 있는 상태
            Increasing, // Foreground Bar가 증가하고 있는 상태 
            InDecreasingDelay, // Delayed Bar가 감소하고 있는 상태
            InIncreasingDelay  // Delayed Bar가 증가하고 있는 상태
        }
        
        [Title("Basic")]
        public string playerID;
        public Transform foregroundBar;
        public Transform delayedBarDecreasing; // the delayed bar that will show when moving from a value to a new, lower value
        public Transform delayedBarIncreasing; // the delayed bar that will show when moving from a value to a new, higher value\

        [Title("Initial Value")]
        [FormerlySerializedAs("StartValue"), Range(0f, 1f)] public float minimumBarFillValue = 0f;
        [FormerlySerializedAs("EndValue"), Range(0f, 1f)] public float maximumBarFillValue = 1f;
        [FormerlySerializedAs("InitialValue"), Range(0f, 1f)] public float initialFillValue = 1f;

        [Title("Foreground Bar")]
        public bool lerpForegroundBar = true;
        [ShowIf("lerpForegroundBar")] public float lerpForegroundBarSpeedDecreasing = 15f;
        [ShowIf("lerpForegroundBar")] public float lerpForegroundBarSpeedIncreasing = 15f;
        [ShowIf("lerpForegroundBar")] public AnimationCurve lerpForegroundBarCurveDecreasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [ShowIf("lerpForegroundBar")] public AnimationCurve lerpForegroundBarCurveIncreasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Title("Delayed Bar (Decreasing)")]
        public float decreasingDelay = 1f; // the delay before the delayed bar moves (in seconds)
        public bool lerpDecreasingDelayedBar = true;
        [ShowIf("lerpDecreasingDelayedBar")] public float lerpDecreasingDelayedBarSpeed = 15f;
        [ShowIf("lerpDecreasingDelayedBar")] public AnimationCurve lerpDecreasingDelayedBarCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Title("Delayed Bar (Increasing)")]
        public float increasingDelay = 1f; // the delay before the delayed bar moves (in seconds)
        public bool lerpIncreasingDelayedBar = true;
        [ShowIf("lerpIncreasingDelayedBar")] public float lerpIncreasingDelayedBarSpeed = 15f;
        [ShowIf("lerpIncreasingDelayedBar")] public AnimationCurve lerpIncreasingDelayedBarCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Title("Bump")]
        public bool bumpOnChange = true; // 값 변하면 bump
        public bool bumpOnIncrease = false; // 값 오를때만 bump
        public bool bumpOnDecrease = false; // 값 내려갈때만 bump
        public float bumpDuration = 0.2f;
        public bool changeColorWhenBumping = true;
        [ShowIf("changeColorWhenBumping")] public Color bumpColor = Color.white;
        public bool storeBarColorOnPlay = true;
        public AnimationCurve bumpScaleAnimationCurve = new AnimationCurve(new Keyframe(1, 1), new Keyframe(0.3f, 1.05f), new Keyframe(1, 1));
        public AnimationCurve bumpColorAnimationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));

        [Title("Event")]
        public UnityEvent onBump;
        public UnityEvent onBarMovementDecreasingStart;
        public UnityEvent onBarMovementDecreasingStop;
        public UnityEvent onBarMovementIncreasingStart;
        public UnityEvent onBarMovementIncreasingStop;
        
        [Title("Text")]
        public Text percentageText;
        public string textPrefix;
        public string textSuffix;
        public string textFormat = "{000}"; // ex. 12인 경우 012 이렇게 표시됨
        
        [Title("Progress (Readonly)")]
        [Range(0f, 1f)] public float barProgress; // Foreground Bar Progress (현재 %)
        [Range(0f, 1f)] public float barTarget = 0; // Foreground Bar Target (목표 %)

        protected float newPercent; // New Foreground Bar Progress
        protected float percentLastTimeBarWasUpdated; // Last Foreground Bar Progress
        protected float delayedBarDecreasingProgress = 0f; // Delayed Bar (Decreasing) Progress
        protected float delayedBarIncreasingProgress = 0f; // Delayed Bar (Increasing) Progress

        protected bool initialized;
        protected Vector3 initialScale; // Transform localScale
        protected Color initialColor;

        protected Image foregroundImage;

        protected float time;
        protected float lastUpdateTimeStamp;
        protected float deltaTime;
        
        protected Coroutine coroutine; // Update Bar Coroutine
        protected bool coroutineShouldRun = false; // Update Bar Coroutine

        protected bool actualUpdate; // 목표치와 현재값이 동일한지 (실제로 업데이트가 된건지)
        protected int direction; // direction > 0 : increasing <-> direction < 0 : decreasing
        protected string updatedText;
        protected ProgressBarStates currentState = ProgressBarStates.Idle;
        
        protected bool isForegroundBarNotNull;
        protected bool isForegroundImageNotNull;
        protected bool isPercentageTextNotNull;
        
        public bool Bumping { get; protected set; }

        #region PUBLIC API

        public virtual void UpdateBar01(float normalizedValue)
        {
            UpdateBar(Mathf.Clamp01(normalizedValue), 0f, 1f);
        }

        /// <summary>
        /// 값 업데이트 (Delayed Bar 있음)
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        public virtual void UpdateBar(float currentValue, float minValue, float maxValue)
        {
            // Check Init
            if (!initialized) Initialization();
            if (storeBarColorOnPlay) StoreInitialColor();
            if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
           
            // Remap current Value
            newPercent = YisoMathUtils.Remap(currentValue, minValue, maxValue, minimumBarFillValue, maximumBarFillValue);
            
            // Check is Already Updated
            actualUpdate = barTarget != newPercent;
            if (!actualUpdate) return;

            // 예외 상황 처리
            if (currentState != ProgressBarStates.Idle)
            {
                if (currentState is ProgressBarStates.Decreasing or ProgressBarStates.InDecreasingDelay)
                {
                    // (예외 상황) 피 감소하는 상태인데 새로운 설정될 값이 현재 값보다 크거나 같은 경우
                    if (newPercent >= barTarget)
                    {
                        StopCoroutine(coroutine);
                        SetBar01(barTarget);
                    }
                }
                if (currentState is ProgressBarStates.Increasing or ProgressBarStates.InIncreasingDelay)
                {
                    // (예외 상황) 피 증가하는 상태인데 새로운 설정될 값이 현재 값보다 작거나 같은 경우
                    if (newPercent <= barTarget)
                    {
                        StopCoroutine(coroutine);
                        SetBar01(barTarget);
                    }
                }
            }

            // Progress 설정
            percentLastTimeBarWasUpdated = barProgress;
            barTarget = newPercent;
            
            // Bump
            if (newPercent != percentLastTimeBarWasUpdated && !Bumping)
            {
                Bump();
            }
            
            // 시간 설정 (time, lastUpdateTimeStamp)
            DetermineDeltaTime();
            lastUpdateTimeStamp = time;
            
            // 감소인지, 증가인지 체크 후 Callback 실행
            DetermineDirection();
            if (direction < 0) onBarMovementDecreasingStart?.Invoke();
            else onBarMovementIncreasingStart?.Invoke();

            // Update Bars Coroutine 실행
            if (coroutine != null) StopCoroutine(coroutine);
            coroutineShouldRun = true;
            if (gameObject.activeInHierarchy) coroutine = StartCoroutine(UpdateBarsCo());
            else SetBar(currentValue, minValue, maxValue);
            
            // Update Text
            UpdateText();
        }

        public virtual void SetBar(float currentValue, float minValue, float maxValue)
        {
            var percent = YisoMathUtils.Remap(currentValue, minValue, maxValue, 0f, 1f);
            SetBar01(percent);
        }

        /// <summary>
        /// 값 업데이트 (Delayed Bar 없이 바로)
        /// </summary>
        /// <param name="newPercent"></param>
        public virtual void SetBar01(float newPercent)
        {
            if (!initialized) Initialization();

            newPercent = YisoMathUtils.Remap(newPercent, 0f, 1f, minimumBarFillValue, maximumBarFillValue);
            
            // delayed Bar 사용 없이 바로 값을 적용하는 거라 일괄적으로 값 세팅
            barProgress = newPercent;
            barTarget = newPercent;
            percentLastTimeBarWasUpdated = newPercent;
            delayedBarDecreasingProgress = newPercent;
            delayedBarIncreasingProgress = newPercent;
            
            SetBarInternal(newPercent, foregroundBar);
            SetBarInternal(newPercent, delayedBarDecreasing);
            SetBarInternal(newPercent, delayedBarIncreasing);
            
            UpdateText();
            
            coroutineShouldRun = false;
            currentState = ProgressBarStates.Idle;
        }

        #endregion

        #region Initialization

        protected override void Start()
        {
            if (!initialized)
            {
                Initialization();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!initialized)
            {
                return;
            }
            StoreInitialColor();
        }

        public virtual void Initialization()
        {
            isForegroundBarNotNull = foregroundBar != null;
            isPercentageTextNotNull = percentageText != null;
            
            initialScale = transform.localScale;

            if (isForegroundBarNotNull)
            {
                foregroundImage = foregroundBar.GetComponent<Image>();
                isForegroundImageNotNull = foregroundImage != null;
            }
            
            initialized = true;
            
            StoreInitialColor();

            percentLastTimeBarWasUpdated = barProgress;

            SetBar01(initialFillValue);
        }

        protected virtual void StoreInitialColor()
        {
            if (!Bumping && isForegroundImageNotNull)
            {
                initialColor = foregroundImage.color;
            }
        }

        #endregion

        #region Update

        protected virtual IEnumerator UpdateBarsCo()
        {
            while (coroutineShouldRun)
            {
                DetermineDeltaTime();
                DetermineDirection();
                UpdateBars();
                yield return null;
            }
            currentState = ProgressBarStates.Idle;
        }

        protected virtual void UpdateBars()
        {
            float newFill;
            float newFillDelayed;
            float t1, t2 = 0f;

            // Decreasing
            if (direction < 0)
            {
                newFill = ComputeNewFill(lerpForegroundBar, lerpForegroundBarSpeedDecreasing,
                    lerpForegroundBarCurveDecreasing, 0f, percentLastTimeBarWasUpdated, out t1);
                SetBarInternal(newFill, foregroundBar);
                SetBarInternal(newFill, delayedBarIncreasing);
                
                barProgress = newFill;
                delayedBarIncreasingProgress = newFill;

                currentState = ProgressBarStates.Decreasing;

                // decreasing Delay만큼 기다린다음.. (delay만큼 시간지났을때)
                if (time - lastUpdateTimeStamp > decreasingDelay)
                {
                    newFillDelayed = ComputeNewFill(lerpDecreasingDelayedBar, lerpDecreasingDelayedBarSpeed,
                        lerpDecreasingDelayedBarCurve, decreasingDelay, delayedBarDecreasingProgress, out t2);
                    SetBarInternal(newFillDelayed, delayedBarDecreasing);
                    delayedBarDecreasingProgress = newFillDelayed;
                    currentState = ProgressBarStates.InDecreasingDelay;
                }
            }
            // Increasing
            else
            {
                // Foreground Bar, Delayed Bar (Decreasing) 설정
                newFill = ComputeNewFill(lerpForegroundBar, lerpForegroundBarSpeedIncreasing,
                    lerpForegroundBarCurveIncreasing, 0f, percentLastTimeBarWasUpdated, out t1);
                SetBarInternal(newFill, foregroundBar);
                SetBarInternal(newFill, delayedBarDecreasing);

                barProgress = newFill;
                delayedBarDecreasingProgress = newFill;

                currentState = ProgressBarStates.Increasing;
                
                // Delayed Bar (Increasing) 설정
                // decreasing Delay만큼 기다린다음.. (delay만큼 시간지났을때)
                if (time - lastUpdateTimeStamp > increasingDelay)
                {
                    newFillDelayed = ComputeNewFill(lerpIncreasingDelayedBar, lerpIncreasingDelayedBarSpeed,
                        lerpIncreasingDelayedBarCurve, increasingDelay, delayedBarIncreasingProgress, out t2);
                    SetBarInternal(newFillDelayed, delayedBarIncreasing);
                    delayedBarIncreasingProgress = newFillDelayed;
                    currentState = ProgressBarStates.InIncreasingDelay;
                }
            }

            if (t1 >= 1f && t2 >= 1f)
            {
                coroutineShouldRun = false;
                if (direction > 0) onBarMovementIncreasingStop?.Invoke();
                else onBarMovementDecreasingStop?.Invoke();
            }
        }

        protected virtual void UpdateText()
        {
            updatedText = textPrefix + barProgress.ToString(textFormat) + textSuffix;
            if (isPercentageTextNotNull) percentageText.text = updatedText;
        }

        #endregion

        #region Tool

        /// <summary>
        /// 소모된 시간 계산 후 Animation Curve의 X값(시간)에 맞는 Y값(barProgress)을 계산함.
        /// </summary>
        /// <param name="lerpBar">Bar Lerp 시킬건지 말건지</param>
        /// <param name="barSpeed"></param>
        /// <param name="barCurve"></param>
        /// <param name="delay"></param>
        /// <param name="lastPercent"></param>
        /// <param name="normalizedTimeSpent">Normalized 소모된 시간 (X값)</param>
        protected virtual float ComputeNewFill(bool lerpBar, float barSpeed, AnimationCurve barCurve, float delay, float lastPercent, out float normalizedTimeSpent)
        {
            var newFill = 0f;
            normalizedTimeSpent = 0f;
            if (lerpBar)
            {
                // 소모된 시간을 계산한 후 Normalized함 = tempNormalizedTimeSpent
                var tempNormalizedTimeSpent = 0f;
                var timeSpent = time - lastUpdateTimeStamp - delay;
                var speed = barSpeed;
                if (speed == 0f) speed = 1f;
                var duration = (Mathf.Abs(newPercent - lastPercent)) / speed;

                tempNormalizedTimeSpent = YisoMathUtils.Remap(timeSpent, 0f, duration, 0f, 1f);
                tempNormalizedTimeSpent = Mathf.Clamp(tempNormalizedTimeSpent, 0f, 1f);
                normalizedTimeSpent = tempNormalizedTimeSpent;

                // Animation Curve에 넣어 값 얻어낸 후 Lerp 시켜
                if (normalizedTimeSpent < 1f)
                {
                    tempNormalizedTimeSpent = barCurve.Evaluate(tempNormalizedTimeSpent);
                    newFill = Mathf.LerpUnclamped(lastPercent, newPercent, tempNormalizedTimeSpent);
                }
                else
                {
                    newFill = newPercent;
                }
            }
            else
            {
                newFill = newPercent;
            }

            newFill = Mathf.Clamp(newFill, 0f, 1f);
            return newFill;
        }

        /// <summary>
        /// 실제로 BarProgress값에 맞게 Foreground Bar의 Width를 설정하는 함수
        /// </summary>
        /// <param name="newAmount"></param>
        /// <param name="bar"></param>
        protected virtual void SetBarInternal(float newAmount, Transform bar)
        {
            if (bar == null) return;
            var targetLocalScale = Vector3.one;
            targetLocalScale.x = newAmount;
            bar.localScale = targetLocalScale;
        }

        protected virtual void DetermineDeltaTime()
        {
            deltaTime = Time.unscaledDeltaTime;
            time = Time.unscaledTime;
        }

        protected virtual void DetermineDirection()
        {
            direction = newPercent > percentLastTimeBarWasUpdated ? 1 : -1;
        }

        #endregion

        #region Bump

        /// <summary>
        /// Bar 값 변할때 순간적으로 나타나는 색, 크기 변화 = Bump
        /// </summary>
        public virtual void Bump()
        {
            var delta = newPercent - percentLastTimeBarWasUpdated;
            var shouldBump = false;
            if (!initialized) Initialization();
            
            DetermineDirection();

            if (bumpOnIncrease && direction > 0) shouldBump = true;
            if (bumpOnDecrease && direction < 0) shouldBump = true;
            if (bumpOnChange) shouldBump = true;
            if (!shouldBump) return;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(BumpCo());
            }
            onBump?.Invoke();
        }

        protected virtual IEnumerator BumpCo()
        {
            var journey = 0f;
            Bumping = true;
            
            while (journey < bumpDuration)
            {
                journey += deltaTime;
                var percent = Mathf.Clamp01(journey / bumpDuration);
                var curvePercent = bumpScaleAnimationCurve.Evaluate(percent);
                var colorCurvePercent = bumpColorAnimationCurve.Evaluate(percent);

                transform.localScale = curvePercent * initialScale;
                if (changeColorWhenBumping && isForegroundImageNotNull)
                {
                    foregroundImage.color = Color.Lerp(initialColor, bumpColor, colorCurvePercent);
                }
                yield return null;
            }
            
            if (changeColorWhenBumping && isForegroundImageNotNull)
            {
                foregroundImage.color = initialColor;
            }
            Bumping = false;
            yield return null;
        }

        #endregion

        #region Show Hide

        public virtual void ShowBar()
        {
            gameObject.SetActive(true);
        }

        public virtual void HideBar(float delay)
        {
            if (delay <= 0f) gameObject.SetActive(false);
            else if (gameObject.activeInHierarchy) StartCoroutine(HideBarCo(delay));
        }

        protected virtual IEnumerator HideBarCo(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        #endregion

        #region Tests

        [Title("Tests")]
        [Range(0f, 1f)] public float debugNewTargetValue;
        
        [Button("Debug Update Bar")]
        protected virtual void DebugUpdateBar()
        {
            UpdateBar01(debugNewTargetValue);
        }
        
        [Button("Debug Set Bar")]
        protected virtual void DebugSetBar()
        {
            SetBar01(debugNewTargetValue);
        }

        [Button("Debug Plus 10%")]
        public virtual void Plus10Percent()
        {
            float newProgress = barTarget + 0.1f;
            newProgress = Mathf.Clamp(newProgress, 0f, 1f);
            UpdateBar01(newProgress);
        }
        
        [Button("Debug Minus 10%")]
        public virtual void Minus10Percent()
        {
            float newProgress = barTarget - 0.1f;
            newProgress = Mathf.Clamp(newProgress, 0f, 1f);
            UpdateBar01(newProgress);
        }

        #endregion
    }
}