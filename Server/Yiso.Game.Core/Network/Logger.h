#pragma once
#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <memory>
#include <vector>

namespace Yiso
{
    // 서버 시작 시 한 번 호출됨 -> spdlog::info / warn / error / debug 로 직접 로그 출력
    inline void InitLogger()
    {
        // 콘솔 출력 용으로..
        auto console_sink = std::make_shared<spdlog::sinks::stdout_color_sink_mt>();
        console_sink->set_level(spdlog::level::debug);

        // 파일 기록 용으로 (최대 5MB, 최대 3개 파일 rotation)
        auto file_sink = std::make_shared<spdlog::sinks::rotating_file_sink_mt>(
            "server.log", 5 * 1024 * 1024, 3);
        file_sink->set_level(spdlog::level::debug);

        // 위에 2개 싱크 묶어서 logger 생성 + 등록
        std::vector<spdlog::sink_ptr> sinks = { console_sink, file_sink };
        auto logger = std::make_shared<spdlog::logger>("yiso", sinks.begin(), sinks.end());
        logger->set_level(spdlog::level::debug);
        logger->set_pattern("[%Y-%m-%d %H:%M:%S.%e] [%^%l%$] %v");

        spdlog::set_default_logger(logger);
        spdlog::flush_on(spdlog::level::err); // error 이상은 즉시 flush (-> 서버 크래시 나면 flush 안된 로그 다 날아가니까)
    }
}
