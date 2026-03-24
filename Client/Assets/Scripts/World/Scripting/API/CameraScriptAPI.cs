using System.Collections;
using Core;
using Core.Log;
using UnityEngine;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: CAMERA.MoveTo / CAMERA.Shake / CAMERA.ZoomTo / CAMERA.Release
    /// </summary>
    public class CameraScriptAPI : IYisoScriptAPI {
        private readonly YisoCameraManager _camera;

        public CameraScriptAPI(YisoCameraManager camera) {
            _camera = camera;
        }

        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("CAMERA.MoveTo",  OnMoveTo);
            runner.RegisterCommand("CAMERA.Shake",   OnShake);
            runner.RegisterCommand("CAMERA.ZoomTo",  OnZoomTo);
            runner.RegisterCommand("CAMERA.Release", OnRelease);
        }

        // CAMERA.MoveTo(x, y, duration)
        // 완료 대기: duration 만큼 WaitForSeconds
        private IEnumerator OnMoveTo(string[] args) {
            if (args == null || args.Length < 3) {
                YisoLogger.Warn("[CameraScriptAPI] CAMERA.MoveTo 인자 부족 — (x, y, duration) 필요");
                yield break;
            }

            var x        = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
            var y        = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
            var duration = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);

            _camera?.MoveToPosition(new Vector3(x, y, -10f));
            yield return new WaitForSeconds(duration);
        }

        // CAMERA.Shake(amplitude, frequency, duration)
        private IEnumerator OnShake(string[] args) {
            if (args == null || args.Length < 3) {
                YisoLogger.Warn("[CameraScriptAPI] CAMERA.Shake 인자 부족 — (amplitude, frequency, duration) 필요");
                yield break;
            }

            var amplitude = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
            var frequency = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
            var duration  = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);

            _camera?.Shake(amplitude, frequency, duration);
            yield return new WaitForSeconds(duration);
        }

        // CAMERA.ZoomTo(size, speed)
        private IEnumerator OnZoomTo(string[] args) {
            if (args == null || args.Length < 2) {
                YisoLogger.Warn("[CameraScriptAPI] CAMERA.ZoomTo 인자 부족 — (size, speed) 필요");
                yield break;
            }

            var size  = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
            var speed = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);

            _camera?.ZoomTo(size, speed);
            yield break;
        }

        // CAMERA.Release() — 플레이어 추적 복귀
        private IEnumerator OnRelease(string[] args) {
            _camera?.ReleaseControl();
            yield break;
        }
    }
}
