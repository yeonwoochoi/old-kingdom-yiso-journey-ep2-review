#include "YisoServer.h"
#include <spdlog/spdlog.h>

namespace Yiso::Network
{
    YisoServer::YisoServer(boost::asio::io_context& io_context, uint16_t port, OnConnect onConnect, OnRecv onRecv, OnDisconnect onDisconnect)
        : acceptor_(io_context, boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), port)),
          on_connect_(onConnect),
          on_recv_(onRecv),
          on_disconnect_(onDisconnect)
    {
        next_id_.store(1u);
        DoAccept();
    }

    void YisoServer::Stop()
    {
        boost::system::error_code ec;
        acceptor_.close(ec); // 새 연결 거부 (DoAccept 콜백이 operation_aborted로 완료됨)
        if (ec) spdlog::warn("[Server] acceptor 닫기 실패: {}", ec.message());
        session_manager_.DisconnectAll(); // 모든 세션 소켓 닫기 -> 진행 중인 async I/O가 에러로 완료 -> io_context 자연 종료
    }

    void YisoServer::DoAccept()
    {
        acceptor_.async_accept(
            [this](boost::system::error_code ec, boost::asio::ip::tcp::socket socket)
            {
                if (!ec)
                {
                    auto id = next_id_.fetch_add(1);

                    auto onDisconnect = [this](YisoSession::SessionId sessionId)
                    {
                        session_manager_.RemoveSession(sessionId);
                        on_disconnect_(sessionId);
                    };

                    auto session = std::make_shared<YisoSession>(
                        id, std::move(socket), on_recv_, onDisconnect
                    );

                    session_manager_.AddSession(session);
                    on_connect_(id);
                    session->Start();
                    DoAccept(); // 다음 연결 대기
                }
                else if (ec == boost::asio::error::operation_aborted)
                {
                    // Stop() 호출로 acceptor 닫힘 -> 정상 종료
                    return;
                }
                else
                {
                    spdlog::error("[Server] accept 오류: {}", ec.message());
                    DoAccept(); // 일시적 오류는 계속 대기
                }
            }
        );
    }
}
