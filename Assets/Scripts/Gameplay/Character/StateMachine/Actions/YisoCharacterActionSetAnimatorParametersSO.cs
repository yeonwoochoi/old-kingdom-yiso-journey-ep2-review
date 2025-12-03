using System;
using System.Collections.Generic;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// 다중 Animator Parameter를 한 번에 제어하는 FSM Action.
    /// 단일 SO 파일에서 여러 파라미터 설정을 리스트로 관리하여 파일 폭증 문제 해결.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_SetAnimatorParameters", menuName = "Yiso/State Machine/Action/SetAnimatorParameters")]
    public class YisoCharacterActionSetAnimatorParametersSO : YisoCharacterActionSO {
        /// <summary>
        /// 개별 Animator Parameter 설정을 나타내는 구조체.
        /// Blackboard 연동 또는 직접 값 설정 가능.
        /// </summary>
        [Serializable]
        public class AnimatorParameterSetting {
            [HorizontalGroup("Header", Width = 0.5f)]
            [LabelText("Parameter")]
            [Tooltip("제어할 Animator Parameter")]
            public YisoCharacterAnimationState parameter;

            [HorizontalGroup("Header", Width = 0.5f)]
            [LabelText("Type")]
            [Tooltip("Parameter 타입")]
            public AnimatorControllerParameterType parameterType;

            [Space(5)]
            [ShowIf("@parameterType != UnityEngine.AnimatorControllerParameterType.Trigger")]
            [Tooltip("Blackboard에서 값을 가져올지 여부 (Trigger는 항상 false)")]
            public bool useBlackboard;

            // ========== Blackboard Mode ==========
            [ShowIf("@useBlackboard && parameterType != UnityEngine.AnimatorControllerParameterType.Trigger")]
            [Indent]
            [LabelText("Blackboard Key")]
            [Tooltip("값을 가져올 Blackboard Key")]
            public YisoBlackboardKeySO blackboardKey;

            // ========== Direct Value Mode ==========
            [ShowIf("@!useBlackboard && parameterType == UnityEngine.AnimatorControllerParameterType.Bool")]
            [Indent]
            [LabelText("Bool Value")]
            public bool boolValue;

            [ShowIf("@!useBlackboard && parameterType == UnityEngine.AnimatorControllerParameterType.Float")]
            [Indent]
            [LabelText("Float Value")]
            public float floatValue;

            [ShowIf("@!useBlackboard && parameterType == UnityEngine.AnimatorControllerParameterType.Int")]
            [Indent]
            [LabelText("Int Value")]
            public int intValue;
        }

        [Title("Animator Parameter Settings")]
        [InfoBox("여러 Animator Parameter를 한 번에 설정합니다. 리스트 순서대로 적용됩니다.")]
        [ListDrawerSettings(
            ShowIndexLabels = true,
            ListElementLabelName = "parameter",
            DraggableItems = true,
            ShowPaging = false
        )]
        [SerializeField]
        private List<AnimatorParameterSetting> settings = new();

        public override void PerformAction(IYisoCharacterContext context) {
            if (settings == null || settings.Count == 0) {
                Debug.LogWarning($"[{name}] Settings list is empty. No parameters to set.");
                return;
            }

            // Blackboard 모듈 Lazy Load (필요할 때만 가져옴)
            YisoCharacterBlackboardModule blackboard = null;
            var blackboardChecked = false;

            foreach (var setting in settings) {
                // Null 체크
                if (setting == null) continue;

                // Trigger 처리 (값이 필요 없음)
                if (setting.parameterType == AnimatorControllerParameterType.Trigger) {
                    context.PlayAnimation(setting.parameter);
                    continue;
                }

                // Blackboard 모듈이 필요한 경우 한 번만 가져옴
                if (setting.useBlackboard && !blackboardChecked) {
                    blackboard = context.GetModule<YisoCharacterBlackboardModule>();
                    blackboardChecked = true;

                    if (blackboard == null) {
                        Debug.LogWarning(
                            $"[{name}] Blackboard module not found. " +
                            "Parameters using Blackboard will use default values."
                        );
                    }
                }

                // 타입에 따라 값 적용
                switch (setting.parameterType) {
                    case AnimatorControllerParameterType.Bool:
                        var boolVal = GetBoolValue(setting, blackboard);
                        context.PlayAnimation(setting.parameter, boolVal);
                        break;

                    case AnimatorControllerParameterType.Float:
                        var floatVal = GetFloatValue(setting, blackboard);
                        context.PlayAnimation(setting.parameter, floatVal);
                        break;

                    case AnimatorControllerParameterType.Int:
                        var intVal = GetIntValue(setting, blackboard);
                        context.PlayAnimation(setting.parameter, intVal);
                        break;
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Bool 값을 가져옵니다 (Blackboard 또는 직접 값).
        /// </summary>
        private bool GetBoolValue(AnimatorParameterSetting setting, YisoCharacterBlackboardModule blackboard) {
            if (!setting.useBlackboard) {
                return setting.boolValue;
            }

            if (blackboard == null || setting.blackboardKey == null) {
                Debug.LogWarning($"[{name}] Invalid Blackboard setup for {setting.parameter}. Using false.");
                return false;
            }

            return blackboard.GetBool(setting.blackboardKey);
        }

        /// <summary>
        /// Float 값을 가져옵니다 (Blackboard 또는 직접 값).
        /// </summary>
        private float GetFloatValue(AnimatorParameterSetting setting, YisoCharacterBlackboardModule blackboard) {
            if (!setting.useBlackboard) {
                return setting.floatValue;
            }

            if (blackboard == null || setting.blackboardKey == null) {
                Debug.LogWarning($"[{name}] Invalid Blackboard setup for {setting.parameter}. Using 0f.");
                return 0f;
            }

            return blackboard.GetFloat(setting.blackboardKey);
        }

        /// <summary>
        /// Int 값을 가져옵니다 (Blackboard 또는 직접 값).
        /// </summary>
        private int GetIntValue(AnimatorParameterSetting setting, YisoCharacterBlackboardModule blackboard) {
            if (!setting.useBlackboard) {
                return setting.intValue;
            }

            if (blackboard == null || setting.blackboardKey == null) {
                Debug.LogWarning($"[{name}] Invalid Blackboard setup for {setting.parameter}. Using 0.");
                return 0;
            }

            return blackboard.GetInt(setting.blackboardKey);
        }

        #endregion
    }
}
