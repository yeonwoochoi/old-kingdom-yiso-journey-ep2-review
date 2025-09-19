using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Health {
    [AddComponentMenu("Yiso/Health/Health UI Controller")]
    [RequireComponent(typeof(YisoEntityHealth))]
    public class YisoHealthUIController: RunIBehaviour {
        // TODO: 리스너 (Listener) 이벤트를 구독하여 UI (체력바, 데미지 텍스트) 갱신
        // YisoHealthbar 없애고 (prefab으로 대체할거임)
        // 1. 빈 게임 오브젝트 만들고 YisoProgressBar 컴포넌트 붙여서 프리팹화
        // 2. 미리 ProgressBar 설정 끝내놓기
        
        // 기능
        // 1. YisoProgressBar prefab 생성
        // 2. Floating Text 오브젝트 생성
        // 3. 이벤트 등록해서 타이밍 맞게 업데이트
        // 4. YisoProgressBar 가시성 관리
    }
}