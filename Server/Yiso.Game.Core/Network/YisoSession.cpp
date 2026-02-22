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
                    on_disconnect_(id_); // 콜백 호출
                    return; // 소켓은 열린 상태로 방치
                    // 왜 문제인가?
                    // shard_ptr의 참조 카운트가 0이 되면 소켓이 닫히긴 하지만, 비동기 콜백에서 shared_from_this로 참조를 잡고 있어서 소켓이 바로 닫히지 않을 수 있음
                    // on_disconnect_ 콜백이 여러번 호출 될 수 있음: 읽기 에러 -> disconnect 콜백 -> 쓰기 에러 -> disconnect 콜백 -> ...
                    // 이중 RemoveSession 호출은 에러는 아니지만, 이중 OnDiscconeted 콜백은 게임 로직 버그를 만들 수 있음
                    
                    // 어떻게 수정?
                    // bool disconnected_ = false;
                    
                    // Disconnect() {
                    //     if (disconnected_) return; // 중복 방지
                    //     disconnected_ = true;
                    //     socket_.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ec);
                    //     socket.close(ec);
                    //     on_disconnect_(id);
                    // }
                }
                DoReadBody();
            }
        );
    }

    void YisoSession::DoReadBody()
    {
        body_buf_.resize(header_buf_.body_size);
        // 검증 없이 바로 리사이즈 -> 클라이언트가 보내는 값을 그대로 신뢰해서 리사이즈를 함
        // 악의적인 클라이언트가 0xFFFFFFFF 약 4GB -> 서버에서 메모리를 4GB 할당 시도
        // 메모리 부족 -> std::bad_alloc 예외 발생 -> 서버 크래시
        // DoS 공격 벡터
        // 어떻게 수정하느냐?
        // 게임서버에서는 MAX_PACKET_SIZE
        // 상수 추가
        // static constexpr uint32_t MAX_PACKET_SIZE = 64 * 1024; // 64k 게임 패킷 기준 충분
        
        // 패킷 크기 검증
        // if (header_buf_.body_size == 0 || header_buf_.body_size > MAX_PACKET_SIZE)
        // {
        //     // Log
        //     on_disconnect_(id_);
        //     return;
        // }
        
        
        try
        {
            body_buf_.resize(header_buf_.body_size);
        }
        catch (const std::bad_alloc& e)
        {
            on_disconnect_(id_);
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
                    on_disconnect_(id_);
                    return;
                }
                // PacketType 검증 없음
                // 클라이언트가 type = 9999를 보내면, 유효하지 않은 enum값이 됨
                // 추후 핸들러가 늘어나면 잘못된 PacketTpye이 예상치 못한 핸들러로 전달 될 수 있음
                // switch 문 에서 처리되지 않는 값이 들어오면서 정의되지 않은 동작을 할 수 있음
                // 서버측에서 유효하지 않은 패킷을 보내는 클라이언트를 감지할 수 없음
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
                    // ec를 무시하고 있어서 연결이 왜 끊겼는지 알 수 없음.
                    // 로그로 추적할 필요가 있음
                    // 클라이언트가 정상 종료 한건지 eof
                    // 네트워크가 끊긴건지, connection_reset
                    // 서버쪽 문제인지 등등..
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
