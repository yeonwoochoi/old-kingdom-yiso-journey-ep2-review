using System;
using System.Collections;
using Core.Log;
using Core.Sound;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: SOUND.PlayBgm / SOUND.StopBgm / SOUND.PlaySfx / SOUND.StopAll
    /// 사운드 ID는 YisoSoundId enum 이름 문자열로 전달 (예: "BgmBaseCamp1")
    /// </summary>
    public class SoundScriptAPI : IYisoScriptAPI {
        private readonly YisoSoundManager _sound;

        public SoundScriptAPI(YisoSoundManager sound) {
            _sound = sound;
        }

        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("SOUND.PlayBgm", OnPlayBgm);
            runner.RegisterCommand("SOUND.StopBgm", OnStopBgm);
            runner.RegisterCommand("SOUND.PlaySfx", OnPlaySfx);
            runner.RegisterCommand("SOUND.StopAll", OnStopAll);
        }

        // SOUND.PlayBgm("BgmBaseCamp1")
        private IEnumerator OnPlayBgm(string[] args) {
            if (!TryParseId(args, 0, out var id)) yield break;
            _sound?.PlayBgm(id);
            yield break;
        }

        // SOUND.StopBgm()
        private IEnumerator OnStopBgm(string[] args) {
            _sound?.StopBgm();
            yield break;
        }

        // SOUND.PlaySfx("SfxAttack")
        private IEnumerator OnPlaySfx(string[] args) {
            if (!TryParseId(args, 0, out var id)) yield break;
            _sound?.PlaySfx(id);
            yield break;
        }

        // SOUND.StopAll()
        private IEnumerator OnStopAll(string[] args) {
            _sound?.StopAllSfx();
            yield break;
        }

        private bool TryParseId(string[] args, int idx, out YisoSoundId id) {
            id = default;
            if (args == null || args.Length <= idx) {
                YisoLogger.Warn("[SoundScriptAPI] 사운드 ID 인자 없음");
                return false;
            }
            if (Enum.TryParse(args[idx], out id)) return true;
            YisoLogger.Warn($"[SoundScriptAPI] 알 수 없는 사운드 ID: {args[idx]}");
            return false;
        }
    }
}
