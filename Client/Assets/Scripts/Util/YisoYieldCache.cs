using System.Collections.Generic;
using UnityEngine;

namespace Util {
    public static class YisoYieldCache {
        private static readonly Dictionary<float, WaitForSeconds> _seconds = new();
        private static readonly Dictionary<float, WaitForSecondsRealtime> _realtime = new();
        
        public static readonly WaitForEndOfFrame EndOfFrames = new();
        public static readonly WaitForFixedUpdate FixedUpdates = new();

        public static WaitForSeconds Seconds(float seconds) {
            if (!_seconds.TryGetValue(seconds, out var wait)) {
                _seconds[seconds] = new WaitForSeconds(seconds);
                return _seconds[seconds];
            }
            return wait;
        }

        public static WaitForSecondsRealtime RealTime(float seconds) {
            if (!_realtime.TryGetValue(seconds, out var wait)) {
                _realtime[seconds] = new WaitForSecondsRealtime(seconds);
                return _realtime[seconds];
            }
            return wait;
        }
    }
}