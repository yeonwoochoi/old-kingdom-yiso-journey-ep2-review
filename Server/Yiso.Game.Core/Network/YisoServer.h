#pragma once
#include "YisoSession.h"
#include "YisoSessionManager.h"
#include <boost/asio.hpp>
#include <atomic>
#include <functional>

namespace Yiso::Network
{
    class YisoServer
    {
    public:
        using OnConnect = std::function<void(YisoSession::SessionId)>;
        using OnRecv = YisoSession::OnRecv;
        using OnDisconnect = YisoSession::OnDisconnect;

        YisoServer(
            boost::asio::io_context& io_context,
            uint16_t port,
            OnConnect onConnect,
            OnRecv onRecv,
            OnDisconnect onDisconnect);

        YisoSessionManager& GetSessionManager() { return session_manager_; };
        void Stop();

    private:
        void DoAccept();

        boost::asio::ip::tcp::acceptor acceptor_;
        YisoSessionManager session_manager_;
        std::atomic<YisoSession::SessionId> next_id_;

        OnConnect on_connect_;
        OnRecv on_recv_;
        OnDisconnect on_disconnect_;
    };
}
