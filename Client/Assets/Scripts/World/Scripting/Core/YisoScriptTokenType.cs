namespace World.Scripting.Core {
    public enum YisoScriptTokenType {
        // 블록 선언
        AtDialogue, AtQuest, AtTrigger, AtWave, AtCutscene,

        // 리터럴
        Identifier, String, Number, Bool,

        // 문법 기호
        Colon, LParen, RParen, Comma, Equals, Question, Dot,

        // 대화
        Speaker, DialogueText, LocaleKey,

        // 공통 키워드
        Wait, End, Default, Set, Get, Event,

        // 대화/퀘스트 액션
        GiveQuest, UnlockQuest,

        // 퀘스트 구조
        Title, Desc, Objective, OnStart, OnComplete, Reward,
        Kill, Talk, Reach, Count, Label, Exp, Gold, Interval,

        // 커맨드 접두어 (dot notation)
        Camera, Sound, Input, Cutscene,

        // 웨이브
        Wave, Spawn,

        // 구조
        Newline, Indent, Dedent, Eof,
    }
}
