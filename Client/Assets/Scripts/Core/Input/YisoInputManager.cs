using System;
using System.Collections.Generic;
using Core.Singleton;
using UnityEngine;

namespace Core.Input {
    /// <summary>
    /// [역할] 유저 입력 → 게임 명령 변환
    /// [책임]
    ///   - 2D 탑다운 이동 입력 처리
    ///   - 스킬 단축키, UI 클릭/터치, 귀환 주문서 숏컷
    ///   - 컷씬 재생 중 입력 차단 (Enable / Disable)
    /// [타입] MonoSingleton (Update에서 입력 감지)
    /// </summary>
    public enum InputKeyType {
        Left,
        Right,
        Up,
        Down,
        Attack,
        Pick,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Skill6,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
    }

    public class YisoInputManager : YisoMonoSingleton<YisoInputManager> {
        private readonly Dictionary<InputKeyType, Action> _inputKeyActions = new();
        private readonly Dictionary<InputKeyType, KeyCode> _inputKeyCodes = new();

        public void RegisterKeyMapping(InputKeyType type, KeyCode code) {
            _inputKeyCodes[type] = code;
        }
    }
}
