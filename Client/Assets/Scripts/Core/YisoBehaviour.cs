using UnityEngine;

namespace Core {
    public class YisoBehaviour: MonoBehaviour {
        protected T GetOrAddComponent<T>() where T : Component {
            return !gameObject.TryGetComponent<T>(out var component)
                ? gameObject.AddComponent<T>()
                : component;
        }
    }
}