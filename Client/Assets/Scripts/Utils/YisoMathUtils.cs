using System;
using UnityEngine;

namespace Utils {
    public static class YisoMathUtils {
        /// <summary>
        /// Remaps a value x in interval [A,B], to the proportional value in interval [C,D]
        /// </summary>
        /// <param name="x">The value to remap.</param>
        /// <param name="A">the minimum bound of interval [A,B] that contains the x value</param>
        /// <param name="B">the maximum bound of interval [A,B] that contains the x value</param>
        /// <param name="C">the minimum bound of target interval [C,D]</param>
        /// <param name="D">the maximum bound of target interval [C,D]</param>
        public static float Remap(float x, float A, float B, float C, float D) {
            // 0으로 나누기 방지: A == B이면 C 반환 (비율 계산 불가능)
            if (Mathf.Approximately(B - A, 0f)) {
                return C;
            }

            float remappedValue = C + (x - A) / (B - A) * (D - C);
            return remappedValue;
        }

        public static float Lerp(float value, float target, float rate, float deltaTime) {
            return deltaTime == 0f ? value : Mathf.Lerp(target, value, LerpRate(rate, deltaTime));
        }

        public static Quaternion Lerp(Quaternion value, Quaternion target, float rate, float deltaTime) {
            return deltaTime == 0f ? value : Quaternion.Lerp(target, value, LerpRate(rate, deltaTime));
        }

        public static Vector3 Lerp(Vector3 value, Vector3 target, float rate, float deltaTime) {
            return deltaTime == 0f ? value : Vector3.Lerp(target, value, LerpRate(rate, deltaTime));
        }

        private static float LerpRate(float rate, float deltaTime) {
            rate = Mathf.Clamp01(rate);
            var invRate = -Mathf.Log(1.0f - rate, 2.0f) * 60f;
            return Mathf.Pow(2.0f, -invRate * deltaTime);
        }

        /// <summary>
        /// Rounds the value passed in parameters to the closest value in the parameter array
        /// </summary>
        /// <param name="value"></param>
        /// <param name="possibleValues"></param>
        /// <returns></returns>
        public static float RoundToClosest(float value, float[] possibleValues, bool pickSmallestDistance = false) {
            if (possibleValues.Length == 0) {
                return 0f;
            }

            float closestValue = possibleValues[0];

            foreach (float possibleValue in possibleValues) {
                float closestDistance = Mathf.Abs(closestValue - value);
                float possibleDistance = Mathf.Abs(possibleValue - value);

                if (closestDistance > possibleDistance) {
                    closestValue = possibleValue;
                }
                else if (closestDistance == possibleDistance) {
                    if ((pickSmallestDistance && closestValue > possibleValue) ||
                        (!pickSmallestDistance && closestValue < possibleValue)) {
                        closestValue = (value < 0) ? closestValue : possibleValue;
                    }
                }
            }

            return closestValue;
        }

        public static float RoundToDecimal(float value, int numberOfDecimals) {
            if (numberOfDecimals <= 0) {
                return Mathf.Round(value);
            }
            else {
                return Mathf.Round(value * 10f * numberOfDecimals) / (10f * numberOfDecimals);
            }
        }

        /// <summary>
        /// Returns a vector3 based on the angle in parameters
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector3 DirectionFromAngle2D(float angle, float additionalAngle) {
            angle += additionalAngle;

            Vector3 direction = Vector3.zero;
            direction.x = Mathf.Cos(angle * Mathf.Deg2Rad);
            direction.y = Mathf.Sin(angle * Mathf.Deg2Rad);
            direction.z = 0f;
            return direction;
        }

        /// <summary>
        /// Computes and returns the angle between two vectors, on a 360° scale
        /// </summary>
        /// <returns>The <see cref="System.Single"/>.</returns>
        /// <param name="vectorA">Vector a.</param>
        /// <param name="vectorB">Vector b.</param>
        public static float AngleBetween(Vector2 vectorA, Vector2 vectorB) {
            float angle = Vector2.Angle(vectorA, vectorB);
            Vector3 cross = Vector3.Cross(vectorA, vectorB);

            if (cross.z > 0) {
                angle = 360 - angle;
            }

            return angle;
        }

        /// <summary>
        /// Moves from "from" to "to" by the specified amount and returns the corresponding value
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="amount">Amount.</param>
        public static float Approach(float from, float to, float amount) {
            if (from < to) {
                from += amount;
                if (from > to) {
                    return to;
                }
            }
            else {
                from -= amount;
                if (from < to) {
                    return to;
                }
            }

            return from;
        }

        /// <summary>
        /// double 범위 내에서 랜덤한 값 생성
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Range(double min, double max) {
            double randomValue = (UnityEngine.Random.value * (max - min)) + min;
            return randomValue;
        }

        public static T GetRandomEnumValueInRange<T>(int startIndex, int endIndex) where T : Enum {
            var enumValues = Enum.GetValues(typeof(T));
            if (startIndex < 0 || endIndex >= enumValues.Length || startIndex > endIndex) {
                throw new ArgumentException("Invalid range");
            }

            var randomIndex = new System.Random().Next(startIndex, endIndex + 1); // Including endIndex
            return (T) enumValues.GetValue(randomIndex);
        }

        public static T GetRandomEnumValueInRange<T>() where T : Enum {
            var enumValues = Enum.GetValues(typeof(T));
            var randomIndex = new System.Random().Next(0, enumValues.Length); // Including endIndex
            return (T) enumValues.GetValue(randomIndex);
        }

        public static T GetRandomEnumValueInRange<T>(int startIndex) where T : Enum {
            var enumValues = Enum.GetValues(typeof(T));
            if (startIndex < 0 || startIndex >= enumValues.Length) {
                throw new ArgumentException("Invalid range");
            }

            var randomIndex = new System.Random().Next(startIndex, enumValues.Length); // Including endIndex
            return (T) enumValues.GetValue(randomIndex);
        }
    }
}