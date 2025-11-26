using System.Reflection;
using UnityEngine;

namespace Editor.Tests.Utils {
    public static class TestUtils {
        public static void SetPrivateField(object target, string fieldName, object value) {
            if (target == null) {
                Debug.LogError("Target object is null");
                return;
            }
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            // 상속 구조까지 뒤져야 할 수도 있음 (기본적으로는 위 플래그로 가능)
            while (field == null && type.BaseType != null) {
                type = type.BaseType;
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            }

            if (field != null) {
                field.SetValue(target, value);
            }
            else {
                Debug.LogError($"Field {fieldName} does not exist on {type.Name}");
            }
        }

        public static void SetPrivateProperty(object target, string propName, object value) {
            if (target == null) {
                Debug.LogError("Target object is null");
                return;
            }
            
            var type = target.GetType();
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            while (prop == null && type.BaseType != null) {
                type = type.BaseType;
                prop = type.GetProperty(propName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            }

            if (prop != null) {
                prop.SetValue(target, value);
            } else {
                Debug.LogError($"Property {propName} does not exist on {type.Name}");
            }
        }
    }
}