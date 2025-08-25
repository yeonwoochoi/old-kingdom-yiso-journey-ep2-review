using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Health {
    [AddComponentMenu("Yiso/Health/Damage Processor")]
    public class YisoDamageProcessor: RunIBehaviour {
        // TODO: 모든 공격을 받아 최종 데미지를 계산하고, Health에 전달
        // 방어력 공식에 사용될 계수, 치명타 확률 및 배율 등 커스텀하고 싶은 값들 public으로 노출시키기
    }
}