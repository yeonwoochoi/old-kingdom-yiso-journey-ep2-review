#include "YisoServer.h"
#include <iostream>

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
                }
                else
                {
                    std::cerr << "[Server] accept error: " << ec.message() << "\n";
                }

                DoAccept(); // 다음 연결 대기
            }
        );
    }
}
