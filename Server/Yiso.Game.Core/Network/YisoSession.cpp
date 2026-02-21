#include "YisoSession.h"

namespace Yiso::Network
{
    YisoSession::YisoSession(SessionId id, Socket socket, OnRecv onRecv, OnDisconnect onDisconnect)
        : id_(id),
          socket_(std::move(socket)),
          on_recv_(onRecv),
          on_disconnect_(onDisconnect)
    {
    }

    void YisoSession::Start()
    {
        DoReadHeader();
    }
    
    void YisoSession::Send(std::vector<uint8_t> frame)
    {
        send_queue_.push_back(std::move(frame));
        if (!writing_)
            DoWrite();
    }

    void YisoSession::DoReadHeader()
    {
        auto self = shared_from_this();
        boost::asio::async_read(
            socket_,
            boost::asio::buffer(&header_buf_, HEADER_SIZE),
            [this, self](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    on_disconnect_(id_);
                    return;
                }
                DoReadBody();
            }
        );
    }

    void YisoSession::DoReadBody()
    {
        body_buf_.resize(header_buf_.body_size);
        auto self = shared_from_this();
        boost::asio::async_read(
            socket_,
            boost::asio::buffer(body_buf_),
            [this, self](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    on_disconnect_(id_);
                    return;
                }
                on_recv_(id_, static_cast<PacketType>(header_buf_.type), body_buf_.data(), static_cast<uint32_t>(body_buf_.size()));
                DoReadHeader(); // 이렇게 계속 다음 패킷 올떄까지 대기 -> 처리 반복
            }
        );
    }

    void YisoSession::DoWrite()
    {
        writing_ = true;
        auto self = shared_from_this();
        boost::asio::async_write(
            socket_,
            boost::asio::buffer(send_queue_.front()),
            [this, self](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    on_disconnect_(id_);
                    return;
                }
                send_queue_.pop_front();
                if (!send_queue_.empty())
                    DoWrite();
                else
                    writing_ = false;
            }
        );
    }
}
