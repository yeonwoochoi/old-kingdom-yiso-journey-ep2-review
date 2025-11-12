using System;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public class YisoCharacterAIModule: YisoCharacterModuleBase {
        private Settings _settings;
        public Vector2 PathDirection { get; private set; }

        public YisoCharacterAIModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void OnUpdate() {
            base.OnUpdate();
            // TODO: NavMesh라던지 Astar라던지 연동하기 (여긴 path를 지정하는 역할) 
            // cf. YisoMovementAbility는 이런 MovementVector를 가지고 실제 물리적으로 움직이게만 하는 역할만 담당
            PathDirection = Vector2.zero;
        }

        [Serializable]
        public class Settings {
            /* TODO: AI 관련 설정 (시야 범위 등) */
        }
    }
}