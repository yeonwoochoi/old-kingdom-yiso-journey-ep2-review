using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests.Utils {
    /// <summary>
    /// 테스트에서 private/protected 필드 및 프로퍼티에 안전하게 접근하기 위한 Reflection 유틸리티.
    /// 실패 시 Assert.Fail을 호출하여 테스트가 즉시 실패하도록 합니다.
    /// </summary>
    public static class TestUtils {
        /// <summary>
        /// Private 또는 Public 필드에 값을 설정합니다.
        /// 필드가 존재하지 않으면 테스트를 즉시 실패시킵니다.
        /// </summary>
        public static void SetPrivateField(object target, string fieldName, object value) {
            if (target == null) {
                Assert.Fail($"[TestUtils] SetPrivateField: target is null (field: {fieldName})");
                return;
            }

            var field = FindField(target.GetType(), fieldName);

            if (field == null) {
                Assert.Fail($"[TestUtils] Field '{fieldName}' not found on type '{target.GetType().Name}'. " +
                           $"Available fields: {GetAvailableFieldNames(target.GetType())}");
                return;
            }

            try {
                field.SetValue(target, value);
            }
            catch (ArgumentException ex) {
                Assert.Fail($"[TestUtils] Type mismatch setting field '{fieldName}'. " +
                           $"Expected: {field.FieldType.Name}, Got: {value?.GetType().Name ?? "null"}. " +
                           $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Private 또는 Public 필드의 값을 가져옵니다.
        /// 필드가 존재하지 않으면 테스트를 즉시 실패시킵니다.
        /// </summary>
        public static T GetPrivateField<T>(object target, string fieldName) {
            if (target == null) {
                Assert.Fail($"[TestUtils] GetPrivateField: target is null (field: {fieldName})");
                return default;
            }

            var field = FindField(target.GetType(), fieldName);

            if (field == null) {
                Assert.Fail($"[TestUtils] Field '{fieldName}' not found on type '{target.GetType().Name}'. " +
                           $"Available fields: {GetAvailableFieldNames(target.GetType())}");
                return default;
            }

            try {
                var value = field.GetValue(target);
                return (T)value;
            }
            catch (InvalidCastException ex) {
                Assert.Fail($"[TestUtils] Type mismatch getting field '{fieldName}'. " +
                           $"Expected: {typeof(T).Name}, Got: {field.FieldType.Name}. " +
                           $"Error: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Private 또는 Public 프로퍼티에 값을 설정합니다.
        /// </summary>
        public static void SetPrivateProperty(object target, string propName, object value) {
            if (target == null) {
                Assert.Fail($"[TestUtils] SetPrivateProperty: target is null (property: {propName})");
                return;
            }

            var prop = FindProperty(target.GetType(), propName);

            if (prop == null) {
                Assert.Fail($"[TestUtils] Property '{propName}' not found on type '{target.GetType().Name}'. " +
                           $"Available properties: {GetAvailablePropertyNames(target.GetType())}");
                return;
            }

            if (!prop.CanWrite) {
                Assert.Fail($"[TestUtils] Property '{propName}' is read-only.");
                return;
            }

            try {
                prop.SetValue(target, value);
            }
            catch (ArgumentException ex) {
                Assert.Fail($"[TestUtils] Type mismatch setting property '{propName}'. " +
                           $"Expected: {prop.PropertyType.Name}, Got: {value?.GetType().Name ?? "null"}. " +
                           $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Private 또는 Public 프로퍼티의 값을 가져옵니다.
        /// </summary>
        public static T GetPrivateProperty<T>(object target, string propName) {
            if (target == null) {
                Assert.Fail($"[TestUtils] GetPrivateProperty: target is null (property: {propName})");
                return default;
            }

            var prop = FindProperty(target.GetType(), propName);

            if (prop == null) {
                Assert.Fail($"[TestUtils] Property '{propName}' not found on type '{target.GetType().Name}'. " +
                           $"Available properties: {GetAvailablePropertyNames(target.GetType())}");
                return default;
            }

            if (!prop.CanRead) {
                Assert.Fail($"[TestUtils] Property '{propName}' is write-only.");
                return default;
            }

            try {
                var value = prop.GetValue(target);
                return (T)value;
            }
            catch (InvalidCastException ex) {
                Assert.Fail($"[TestUtils] Type mismatch getting property '{propName}'. " +
                           $"Expected: {typeof(T).Name}, Got: {prop.PropertyType.Name}. " +
                           $"Error: {ex.Message}");
                return default;
            }
        }

        #region Helper Methods

        /// <summary>
        /// 타입 및 상속 체인에서 필드를 찾습니다.
        /// </summary>
        private static FieldInfo FindField(Type type, string fieldName) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            while (type != null) {
                var field = type.GetField(fieldName, flags);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 타입 및 상속 체인에서 프로퍼티를 찾습니다.
        /// </summary>
        private static PropertyInfo FindProperty(Type type, string propertyName) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            while (type != null) {
                var prop = type.GetProperty(propertyName, flags);
                if (prop != null) return prop;
                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 타입의 모든 필드 이름을 가져옵니다 (디버깅용).
        /// </summary>
        private static string GetAvailableFieldNames(Type type) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var fields = type.GetFields(flags);
            return fields.Length > 0
                ? string.Join(", ", Array.ConvertAll(fields, f => f.Name))
                : "(none)";
        }

        /// <summary>
        /// 타입의 모든 프로퍼티 이름을 가져옵니다 (디버깅용).
        /// </summary>
        private static string GetAvailablePropertyNames(Type type) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var props = type.GetProperties(flags);
            return props.Length > 0
                ? string.Join(", ", Array.ConvertAll(props, p => p.Name))
                : "(none)";
        }

        #endregion
    }
}
