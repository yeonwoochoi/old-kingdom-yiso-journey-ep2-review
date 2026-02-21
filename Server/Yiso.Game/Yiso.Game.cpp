#include "Chat/ChatHandler.h"
#include "Network/YisoServer.h"
#include <boost/asio.hpp>
#include <iostream>

int main(int argc, char* argv[])
{
    uint16_t port = 7777;
    if (argc > 1)
        port = static_cast<uint16_t>(std::stoi(argv[1]));

    try
    {
        boost::asio::io_context io;

        // ChatHandler는 Server의 SessionManager를 참조해야 하므로
        // Server를 먼저 만들고, 이후 ChatHandler 초기화
        std::unique_ptr<Yiso::Game::ChatHandler> chat;

        Yiso::Network::YisoServer server(
            io,
            port,
            [&chat](auto id)                          { if (chat) chat->OnConnected(id); },
            [&chat](auto id, auto type, auto data, auto size) { if (chat) chat->OnRecv(id, type, data, size); },
            [&chat](auto id)                          { if (chat) chat->OnDisconnected(id); }
        );

        // io.run() 전에 초기화하므로 콜백 호출 전 보장됨
        chat = std::make_unique<Yiso::Game::ChatHandler>(server.GetSessionManager());

        std::cout << "[Server] Listening on port " << port << "\n";
        io.run();
    }
    catch (std::exception& e)
    {
        std::cerr << "[Server] Exception: " << e.what() << "\n";
        return 1;
    }

    return 0;
}
