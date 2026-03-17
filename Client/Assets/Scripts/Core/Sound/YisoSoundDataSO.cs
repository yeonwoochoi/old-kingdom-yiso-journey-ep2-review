using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Sound {
    public enum YisoSoundId {
        // BGM
        BgmLogin,
        BgmBaseCamp1,
        BgmBaseCamp2,
        
        // SFX
        SfxAttack,
        SfxHit,
    }
    
    [CreateAssetMenu(fileName = "SoundSO", menuName = "Yiso/Config/Sound")]
    public class YisoSoundDataSO: ScriptableObject {
        [Serializable]
        public class Entry {
            public YisoSoundId id;
            public AssetReferenceT<AudioClip> clipRef;
        }
        
        [SerializeField] private List<Entry> entries;
        private Dictionary<YisoSoundId, AssetReferenceT<AudioClip>> _map;
            
        public void Build() {
            _map = entries.ToDictionary(e => e.id, e => e.clipRef);
        }

        public AssetReferenceT<AudioClip> Get(YisoSoundId id) => _map[id];
    }
}