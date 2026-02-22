#include "YisoSession.h"
#include <spdlog/spdlog.h>

namespace Yiso::Network
{
    YisoSession::YisoSession(SessionId id, Socket socket, OnRecv onRecv, OnDisconnect onDisconnect)
        : id_(id),
          socket_(std::move(socket)),
          timer_(socket_.get_executor()),
          on_recv_(onRecv),
          on_disconnect_(onDisconnect)
    {
    }

    void YisoSession::Start()
    {
        ResetTimer();
        DoReadHeader();
    }

    void YisoSession::ResetTimer()
    {
        timer_.expires_after(std::chrono::seconds(TIMEOUT_SEC));
        timer_.async_wait([this, self = shared_from_this()](boost::system::error_code ec)
        {
            if (ec == boost::asio::error::operation_aborted) return; // Disconnect()에서 cancel됨
            spdlog::warn("[Session:{}] {}초 타임아웃, 연결 종료", id_, TIMEOUT_SEC);
            Disconnect();
        });
    }

    // post로 감싸 항상 io_context 스레드에서 실행 → writing_, send_queue_ 접근이 단일 스레드로 보장
    // 추후 멀티 스레드 io_context 전환 시 strand로 교체
    void YisoSession::Send(std::vector<uint8_t> frame)
    {
        boost::asio::post(socket_.get_executor(),
            [this, self = shared_from_this(), frame = std::move(frame)]() mutable
            {
                if (send_queue_.size() >= MAX_SEND_QUEUE_SIZE)
                {
                    spdlog::warn("[Session:{}] send_queue_ 한도 초과 ({}개), 연결 종료", id_, send_queue_.size());
                    Disconnect();
                    return;
                }
                send_queue_.push_back(std::move(frame));
                if (!writing_)
                    DoWrite();
            });
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
                ResetTimer(); // 완전한 패킷 수신 시마다 타임아웃 리셋
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

        timer_.cancel(); // 타임아웃 타이머 취소 (operation_aborted로 콜백 완료됨)

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
