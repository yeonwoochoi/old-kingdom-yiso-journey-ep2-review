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

        /// <summary>
        /// parameterName을 hash값으로 바꾼다음 parameterList에 넣어주고 out parameter로 빼줌
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="parameterName"></param>
        /// <param name="parameter"></param>
        /// <param name="type"></param>
        /// <param name="parameterList"></param>
        public static void AddAnimatorParameterIfExists(Animator animator, string parameterName, out int parameter,
            AnimatorControllerParameterType type, HashSet<int> parameterList) {
            if (string.IsNullOrEmpty(parameterName)) {
                parameter = -1;
                return;
            }

            parameter = Animator.StringToHash(parameterName);

            if (animator.HasParameterOfType(parameterName, type)) {
                parameterList.Add(parameter);
            }
        }

        public static void UpdateAnimatorBool(Animator animator, string parameterName, bool value) {
            animator.SetBool(parameterName, value);
        }

        public static void UpdateAnimatorTrigger(Animator animator, string parameterName) {
            animator.SetTrigger(parameterName);
        }

        public static void UpdateAnimatorInteger(Animator animator, string parameterName, int value) {
            animator.SetInteger(parameterName, value);
        }

        public static void UpdateAnimatorFloat(Animator animator, string parameterName, float value) {
            animator.SetFloat(parameterName, value);
        }

        public static void UpdateAnimatorBool(Animator animator, int parameter, bool value) {
            animator.SetBool(parameter, value);
        }

        public static void UpdateAnimatorTrigger(Animator animator, int parameter) {
            animator.SetTrigger(parameter);
        }

        public static void UpdateAnimatorInteger(Animator animator, int parameter, int value) {
            animator.SetInteger(parameter, value);
        }

        public static void UpdateAnimatorFloat(Animator animator, int parameter, float value) {
            animator.SetFloat(parameter, value);
        }

        public static bool UpdateAnimatorBool(Animator animator, int parameter, bool value, HashSet<int> parameterList,
            bool performSanityCheck = true) {
            if (performSanityCheck && !parameterList.Contains(parameter)) return false;
            animator.SetBool(parameter, value);
            return true;
        }

        public static bool UpdateAnimatorTrigger(Animator animator, int parameter, HashSet<int> parameterList,
            bool performSanityCheck = true) {
            if (performSanityCheck && !parameterList.Contains(parameter)) return false;
            animator.SetTrigger(parameter);
            return true;
        }

        public static bool UpdateAnimatorFloat(Animator animator, int parameter, float value,
            HashSet<int> parameterList, bool performSanityCheck = true) {
            if (performSanityCheck && !parameterList.Contains(parameter)) return false;
            animator.SetFloat(parameter, value);
            return true;
        }

        public static bool UpdateAnimatorInteger(Animator animator, int parameter, int value,
            HashSet<int> parameterList, bool performSanityCheck = true) {
            if (performSanityCheck && !parameterList.Contains(parameter)) return false;
            animator.SetInteger(parameter, value);
            return true;
        }
    }
}