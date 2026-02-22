#pragma once
#include "PacketHeader.h"
#include <boost/asio.hpp>
#include <deque>
#include <functional>
#include <memory>
#include <vector>
#include <cstdint>

namespace Yiso::Network
{
    class YisoSession : public std::enable_shared_from_this<YisoSession>
    {
    public:
        using SessionId = uint32_t;
        using Socket = boost::asio::ip::tcp::socket;
        using OnRecv = std::function<void(SessionId, PacketType, const uint8_t*, uint32_t)>; // 패킷 수신 콜백: (세션ID, 패킷타입, 페이로드 포인터, 페이로드 크기)
        using OnDisconnect = std::function<void(SessionId)>; // 연결 해제 콜백: (세션ID)

        YisoSession(SessionId id, Socket socket, OnRecv onRecv, OnDisconnect onDisconnect);

        void Start();
        void Send(std::vector<uint8_t> frame);
        void Disconnect(boost::system::error_code ec={});

        SessionId GetId() const { return id_; }

    private:
        void DoReadHeader();
        void DoReadBody();
        void DoWrite();
        void ResetTimer();

        SessionId id_;
        Socket socket_;
        boost::asio::steady_timer timer_; // socket_ 이후 선언하기 (초기화 순서 보장)

        PacketHeader header_buf_{};
        std::vector<uint8_t> body_buf_;

        std::deque<std::vector<uint8_t>> send_queue_;
        bool writing_ = false;
        bool disconnected_ = false;

        OnRecv on_recv_;
        OnDisconnect on_disconnect_;

        static constexpr size_t MAX_SEND_QUEUE_SIZE = 256;
        static constexpr int TIMEOUT_SEC = 30;
    };
}
