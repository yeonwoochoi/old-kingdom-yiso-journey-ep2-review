using UnityEngine;

namespace Core.Localization {
    public enum LocaleType {
        KR = 0,
        EN = 1,
    }
    
    [CreateAssetMenu(fileName = "LocaleSO", menuName = "Yiso/Config/Localization")]
    public class YisoLocaleSO: ScriptableObject {
        public LocaleType localeType;
    }
}