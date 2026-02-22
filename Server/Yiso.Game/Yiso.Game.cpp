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

        spdlog::info("[Server] 포트 {} 에서 수신 대기 중", port);
        io.run();

        // Graceful Shutdown
        // 현재 서버를 종료하려면 프로세스를 강제 종료해야 된다.
        // 진행중인 비동기 I/O가 정리 없이 중단된다.
        // 전송 중이던 패킷이 유실될 수 있음
        // 연결된 클라이언트들은 Reset 패킷을 받음 (정상 종료가 아닌 연결 끊김으로 인식)
        // 운영 환경에서 서버 업데이트나 재시작 시 모든 유저가 비정상 종료 경험
    }
    catch (std::exception& e)
    {
        spdlog::critical("[Server] 예외 발생: {}", e.what());
        return 1;
    }

    return 0;
}


// 숙제
// Graceful Shutdown
//      - 시그널 핸들링 추가
//      - YisoServer Stop 함수 추가
//          - 새 연결 거부
//          - 모든 세션 정리
//              - 각 세션의 소켓을 닫아서 비동기 작업이 에러와 함께 완료되도록 한다
//                  - 콜백에서 정리 로직 실행
// TimeOut이 없음
//  - 클라이언트가 TCP 연결만 맺고 아무 데이터도 보내지 않으면, 서버쪽 세션이 영원히 유지 됨
//      - DoReadHeader()에서 async_read가 완료되기를 기다리면서 무한 대기
//      - 악의적 클라이언트가 수만개의 연결을 열면 서버 리소스(소켓, 메모리)가 고갈
//      - 네트워크 장애로 클라이언트가 사라져도 감지 불가
//          - 좀비 세션
