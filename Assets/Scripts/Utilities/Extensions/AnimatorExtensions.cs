using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utilities.Extensions {
    public static class AnimatorExtensions {
        /// <summary>
        /// 해당 name과 type을 가진 animator parameter 있는지 없는지
        /// </summary>
        /// <param name="self">Animator</param>
        /// <param name="name">param name</param>
        /// <param name="type">float, int, bool, trigger</param>
        /// <returns></returns>
        public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type) {
            if (string.IsNullOrEmpty(name)) return false;
            var parameters = self.parameters;
            return parameters.Any(param => param.type == type && param.name == name);
        }

        public static bool TryAddAnimatorParameter(this Animator animator, string parameterName,
            AnimatorControllerParameterType type, HashSet<int> parameterList) {
            if (string.IsNullOrEmpty(parameterName)) return false;
            if (!animator.HasParameterOfType(parameterName, type)) return false;

            var hash = Animator.StringToHash(parameterName);
            parameterList.Add(hash);
            return true;
        }

        public static bool TrySetBool(this Animator animator, int hash, bool value, HashSet<int> parameterList) {
            if (!parameterList.Contains(hash)) return false;
            animator.SetBool(hash, value);
            return true;
        }

        public static bool TrySetFloat(this Animator animator, int hash, float value, HashSet<int> parameterList) {
            if (!parameterList.Contains(hash)) return false;
            animator.SetFloat(hash, value);
            return true;
        }

        public static bool TrySetInteger(this Animator animator, int hash, int value, HashSet<int> parameterList) {
            if (!parameterList.Contains(hash)) return false;
            animator.SetInteger(hash, value);
            return true;
        }

        public static bool TrySetTrigger(this Animator animator, int hash, HashSet<int> parameterList) {
            if (!parameterList.Contains(hash)) return false;
            animator.SetTrigger(hash);
            return true;
        }

        public static void SetBool(this Animator animator, int hash, bool value) {
            animator.SetBool(hash, value);
        }

        public static void SetFloat(this Animator animator, int hash, float value) {
            animator.SetFloat(hash, value);
        }

        public static void SetInteger(this Animator animator, int hash, int value) {
            animator.SetInteger(hash, value);
        }

        public static void SetTrigger(this Animator animator, int hash) {
            animator.SetTrigger(hash);
        }
    }
}