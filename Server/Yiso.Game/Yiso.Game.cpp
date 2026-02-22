#include "Chat/ChatHandler.h"
#include "Network/Logger.h"
#include "Network/YisoServer.h"
#include <boost/asio.hpp>
#include <spdlog/spdlog.h>

int main(int argc, char* argv[])
{
    Yiso::InitLogger();

    uint16_t port = 7777;
    if (argc > 1)
    {
        int raw = std::stoi(argv[1]);
        if (raw < 1024 || raw > 65535)
        {
            spdlog::critical("[Server] 포트 범위 오류: {} (유효 범위: 1024~65535)", raw);
            return 1;
        }
        port = static_cast<uint16_t>(raw);
    }

    try
    {
        boost::asio::io_context io;

        // ChatHandler는 Server의 SessionManager를 참조해야 하므로
        // Server를 먼저 만들고, 이후 ChatHandler 초기화
        std::unique_ptr<Yiso::Game::ChatHandler> chat;

        Yiso::Network::YisoServer server(
            io,
            port,
            [&chat](auto id) { if (chat) chat->OnConnected(id); },
            [&chat](auto id, auto type, auto data, auto size) { if (chat) chat->OnRecv(id, type, data, size); },
            [&chat](auto id) { if (chat) chat->OnDisconnected(id); }
        );

        // io.run() 전에 초기화하므로 콜백 호출 전 보장됨
        chat = std::make_unique<Yiso::Game::ChatHandler>(server.GetSessionManager());
        
        // SIGINT (2) : Ctrl + C
        // SIGTERM (15): 프로세스 종료 요청 (kill 등)
        // SIGKILL (9) : 강제 종료 (catch 불가)
        // SIGHUP (1) : 터미널 종료 / 설정 리로드
        // -> 그 중, SIGINT, SIGTERM 수신 시 Graceful Shutdown
        boost::asio::signal_set signals(io, SIGINT, SIGTERM);
        signals.async_wait([&server](boost::system::error_code, int signo)
        {
            spdlog::info("[Server] 시그널 수신 (signo={}), Graceful Shutdown 시작...", signo);
            server.Stop();
            // Stop() 후 진행 중인 비동기 I/O가 모두 에러로 완료되면 io_context 자연 종료
        });

        spdlog::info("[Server] 포트 {} 에서 수신 대기 중", port);
        io.run();
        spdlog::info("[Server] 서버 종료");
    }
    catch (std::exception& e)
    {
        spdlog::critical("[Server] 예외 발생: {}", e.what());
        return 1;
    }

    return 0;
}

// 숙제
// TimeOut이 없음
//  - 클라이언트가 TCP 연결만 맺고 아무 데이터도 보내지 않으면, 서버쪽 세션이 영원히 유지 됨
//      - DoReadHeader()에서 async_read가 완료되기를 기다리면서 무한 대기
//      - 악의적 클라이언트가 수만개의 연결을 열면 서버 리소스(소켓, 메모리)가 고갈
//      - 네트워크 장애로 클라이언트가 사라져도 감지 불가
//          - 좀비 세션
