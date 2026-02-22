#include "YisoSession.h"
#include <spdlog/spdlog.h>

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

    // Send 함수는 어디서든 호출 될 수 있음
    // 예를 들면 Broadcast에서 여러 세션에 전송..
    // 그런데 wirting_ 플래그와 send_queue_에 대한 동기화가 없음
    // Send를 상항 io_conext 스레드에서 실행되도록 post 로 감싼다
    // 이렇게하면 Send, DoWrite, DoReadHeader, DoReadBody 함수가 모두 같은 io_context 스레드에서 실행되므로 동기화 문제를 피할 수 있음
    // 추후 멀티 스레드 io_context를 사용할 경우, boost::asio::strand 도입
    void YisoSession::Send(std::vector<uint8_t> frame)
    {
        send_queue_.push_back(std::move(frame));
        // 제한 없이 계속 쌓임
        // 클라이언트가 데이터를 느리게 수신하거나 아예 수신하지 않으면?
        //  - 서버는 계속 Broadcast 메시지를 send_queue_에 넣음
        //  - async_write는 소켓 버퍼가 꽉차서 완료되지 않음
        //  - send_queue_에 메시지가 무한히 쌓인다 -> 서버 메모리 고갈
        // 해결 필요
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
                    // EOF는 클라이언트가 정상적으로 연결을 끊은 것
                    if (ec == boost::asio::error::eof)
                        spdlog::info("[Session:{}] 클라이언트 연결 종료 (EOF)", id_);
                    else
                        spdlog::error("[Session:{}] 헤더 읽기 오류: {}", id_, ec.message());
                    Disconnect(ec);
                    return;
                }
                DoReadBody();
            }
        );
    }

    void YisoSession::DoReadBody()
    {
        if (header_buf_.body_size == 0 || header_buf_.body_size > MAX_PACKET_SIZE)
        {
            spdlog::warn("[Session:{}] 잘못된 body_size={}, 연결 종료", id_, header_buf_.body_size);
            Disconnect();
            return;
        }

        try
        {
            body_buf_.resize(header_buf_.body_size);
        }
        catch (const std::bad_alloc&)
        {
            spdlog::error("[Session:{}] 메모리 할당 실패 (body_size={}), 연결 종료", id_, header_buf_.body_size);
            Disconnect();
            return;
        }


        auto self = shared_from_this();
        boost::asio::async_read(
            socket_,
            boost::asio::buffer(body_buf_),
            [this, self](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    spdlog::error("[Session:{}] 바디 읽기 오류: {}", id_, ec.message());
                    Disconnect(ec);
                    return;
                }
                if (!IsValidPacketType(header_buf_.type))
                {
                    spdlog::warn("[Session:{}] 유효하지 않은 패킷 타입={}, 연결 종료", id_, header_buf_.type);
                    Disconnect();
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
                    spdlog::error("[Session:{}] 쓰기 오류: {}", id_, ec.message());
                    Disconnect(ec);
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

    void YisoSession::Disconnect(boost::system::error_code ec)
    {
        if (disconnected_) return;
        disconnected_ = true;

        // ec가 없거나 EOF면 정상 종료, 그 외는 비정상
        if (!ec || ec == boost::asio::error::eof)
            spdlog::info("[Session:{}] 세션 종료", id_);
        else
            spdlog::warn("[Session:{}] 비정상 세션 종료: {}", id_, ec.message());

        boost::system::error_code ignored;
        socket_.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ignored);
        socket_.close(ignored);
        on_disconnect_(id_);
    }

}
