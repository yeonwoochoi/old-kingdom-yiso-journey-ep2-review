#include "Network/PacketHeader.h"
#include "Network/PacketCodec.h"
#include "game_packet.pb.h"
#include <boost/asio.hpp>
#include <iostream>
#include <sstream>
#include <string>
#include <thread>
#include <vector>
#include <windows.h>
#include <fcntl.h>
#include <io.h>

using boost::asio::ip::tcp;
using namespace Yiso::Network;

// 커맨드 파싱 헬퍼
// 입력 형식:
//   (그냥 텍스트)           -> 글로벌 채팅
//   /w <sid> <msg>         -> 귓속말
//   /cr <name>             -> 채팅방 생성
//   /dr <room_id>          -> 채팅방 삭제
//   /jr <room_id>          -> 채팅방 입장
//   /lr <room_id>          -> 채팅방 퇴장
//   /rc <room_id> <msg>    -> 채팅방 채팅
//   /help                  -> 커맨드 목록 출력

static void PrintHelp()
{
    std::cout <<
        "Commands:\n"
        "  (text)              - 글로벌 채팅\n"
        "  /w <sid> <msg>      - 귓속말\n"
        "  /cr <name>          - 채팅방 생성\n"
        "  /dr <room_id>       - 채팅방 삭제\n"
        "  /jr <room_id>       - 채팅방 입장\n"
        "  /lr <room_id>       - 채팅방 퇴장\n"
        "  /rc <room_id> <msg> - 채팅방 채팅\n"
        "  /help               - 이 도움말\n";
}

class DummyClient
{
public:
    DummyClient(boost::asio::io_context& io, const std::string& host, uint16_t port)
        : io_(io), socket_(io)
    {
        tcp::resolver resolver(io);
        auto endpoints = resolver.resolve(host, std::to_string(port));
        boost::asio::connect(socket_, endpoints);
        std::cout << "[Client] Connected to " << host << ":" << port << "\n";
        PrintHelp();

        DoReadHeader();
    }

    // stdin 스레드에서 호출 → io_context 스레드로 전달
    void HandleInput(const std::string& line)
    {
        if (line.empty()) return;

        if (line == "/help")
        {
            PrintHelp();
            return;
        }

        std::istringstream ss(line);
        std::string cmd;
        ss >> cmd;

        if (cmd == "/w")
        {
            uint32_t sid; std::string msg;
            if (!(ss >> sid) || !std::getline(ss >> std::ws, msg))
            {
                std::cout << "[usage] /w <sid> <msg>\n";
                return;
            }
            boost::asio::post(io_, [this, sid, msg]()
            {
                yiso::game::C2S_Whisper req;
                req.set_target_session_id(sid);
                req.set_message(msg);
                Send(PacketType::C2S_WHISPER, req);
            });
        }
        else if (cmd == "/cr")
        {
            std::string name;
            if (!std::getline(ss >> std::ws, name))
            {
                std::cout << "[usage] /cr <name>\n";
                return;
            }
            boost::asio::post(io_, [this, name]()
            {
                yiso::game::C2S_CreateRoom req;
                req.set_room_name(name);
                Send(PacketType::C2S_CREATE_ROOM, req);
            });
        }
        else if (cmd == "/dr")
        {
            uint32_t room_id;
            if (!(ss >> room_id))
            {
                std::cout << "[usage] /dr <room_id>\n";
                return;
            }
            boost::asio::post(io_, [this, room_id]()
            {
                yiso::game::C2S_DeleteRoom req;
                req.set_room_id(room_id);
                Send(PacketType::C2S_DELETE_ROOM, req);
            });
        }
        else if (cmd == "/jr")
        {
            uint32_t room_id;
            if (!(ss >> room_id))
            {
                std::cout << "[usage] /jr <room_id>\n";
                return;
            }
            boost::asio::post(io_, [this, room_id]()
            {
                yiso::game::C2S_JoinRoom req;
                req.set_room_id(room_id);
                Send(PacketType::C2S_JOIN_ROOM, req);
            });
        }
        else if (cmd == "/lr")
        {
            uint32_t room_id;
            if (!(ss >> room_id))
            {
                std::cout << "[usage] /lr <room_id>\n";
                return;
            }
            boost::asio::post(io_, [this, room_id]()
            {
                yiso::game::C2S_LeaveRoom req;
                req.set_room_id(room_id);
                Send(PacketType::C2S_LEAVE_ROOM, req);
            });
        }
        else if (cmd == "/rc")
        {
            uint32_t room_id; std::string msg;
            if (!(ss >> room_id) || !std::getline(ss >> std::ws, msg))
            {
                std::cout << "[usage] /rc <room_id> <msg>\n";
                return;
            }
            boost::asio::post(io_, [this, room_id, msg]()
            {
                yiso::game::C2S_RoomChat req;
                req.set_room_id(room_id);
                req.set_message(msg);
                Send(PacketType::C2S_ROOM_CHAT, req);
            });
        }
        else if (!cmd.empty() && cmd[0] == '/')
        {
            std::cout << "[unknown command] " << cmd << " (/help 참고)\n";
        }
        else
        {
            // 일반 텍스트 → 글로벌 채팅
            boost::asio::post(io_, [this, line]()
            {
                yiso::game::C2S_Chat req;
                req.set_message(line);
                Send(PacketType::C2S_CHAT, req);
            });
        }
    }

private:
    template<typename T>
    void Send(PacketType type, const T& msg)
    {
        auto frame = PacketCodec::Encode(type, msg);
        boost::asio::write(socket_, boost::asio::buffer(frame));
    }

    void DoReadHeader()
    {
        boost::asio::async_read(
            socket_,
            boost::asio::buffer(&header_buf_, HEADER_SIZE),
            [this](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    std::cerr << "[Client] read error: " << ec.message() << "\n";
                    return;
                }
                DoReadBody();
            }
        );
    }

    void DoReadBody()
    {
        body_buf_.resize(header_buf_.body_size);
        boost::asio::async_read(
            socket_,
            boost::asio::buffer(body_buf_),
            [this](boost::system::error_code ec, auto)
            {
                if (ec)
                {
                    std::cerr << "[Client] read error: " << ec.message() << "\n";
                    return;
                }

                HandlePacket(static_cast<PacketType>(header_buf_.type));
                DoReadHeader();
            }
        );
    }

    void HandlePacket(PacketType type)
    {
        const uint8_t* data = body_buf_.data();
        const int size = static_cast<int>(body_buf_.size());

        switch (type)
        {
        case PacketType::S2C_CHAT:
        {
            yiso::game::S2C_Chat msg;
            if (msg.ParseFromArray(data, size))
                std::cout << "[글로벌] [" << msg.session_id() << "] " << msg.message() << "\n";
            break;
        }
        case PacketType::S2C_WHISPER:
        {
            yiso::game::S2C_Whisper msg;
            if (msg.ParseFromArray(data, size))
                std::cout << "[귓속말] from=" << msg.from_session_id() << " : " << msg.message() << "\n";
            break;
        }
        case PacketType::S2C_CREATE_ROOM:
        {
            yiso::game::S2C_CreateRoom msg;
            if (!msg.ParseFromArray(data, size)) break;
            if (msg.success())
                std::cout << "[방 생성] room_id=" << msg.room_id() << " name=" << msg.room_name() << "\n";
            else
                std::cout << "[방 생성 실패] " << msg.error() << "\n";
            break;
        }
        case PacketType::S2C_DELETE_ROOM:
        {
            yiso::game::S2C_DeleteRoom msg;
            if (!msg.ParseFromArray(data, size)) break;
            if (msg.success())
                std::cout << "[방 삭제] room_id=" << msg.room_id() << "\n";
            else
                std::cout << "[방 삭제 실패] " << msg.error() << "\n";
            break;
        }
        case PacketType::S2C_JOIN_ROOM:
        {
            yiso::game::S2C_JoinRoom msg;
            if (!msg.ParseFromArray(data, size)) break;
            if (msg.success())
                std::cout << "[방 입장] room_id=" << msg.room_id() << " session=" << msg.joined_session() << "\n";
            else
                std::cout << "[방 입장 실패] " << msg.error() << "\n";
            break;
        }
        case PacketType::S2C_LEAVE_ROOM:
        {
            yiso::game::S2C_LeaveRoom msg;
            if (msg.ParseFromArray(data, size))
                std::cout << "[방 퇴장] room_id=" << msg.room_id() << " session=" << msg.left_session() << "\n";
            break;
        }
        case PacketType::S2C_ROOM_CHAT:
        {
            yiso::game::S2C_RoomChat msg;
            if (msg.ParseFromArray(data, size))
                std::cout << "[방 " << msg.room_id() << "] [" << msg.from_session_id() << "] " << msg.message() << "\n";
            break;
        }
        default:
            std::cerr << "[Client] unknown packet type: " << static_cast<uint16_t>(type) << "\n";
            break;
        }
    }

    boost::asio::io_context& io_;
    tcp::socket socket_;
    PacketHeader header_buf_{};
    std::vector<uint8_t> body_buf_;
};

int main(int argc, char* argv[])
{
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);
    _setmode(_fileno(stdin), _O_U16TEXT);  // stdin을 UTF-16으로 읽어 CP949 오염 방지

    std::string host = "127.0.0.1";
    uint16_t port = 7777;
    if (argc > 1) host = argv[1];
    if (argc > 2) port = static_cast<uint16_t>(std::stoi(argv[2]));

    try
    {
        boost::asio::io_context io;
        DummyClient client(io, host, port);

        std::thread input_thread([&client]()
        {
            std::wstring wline;
            while (std::getline(std::wcin, wline))
            {
                // UTF-16 -> UTF-8 변환
                int bytes = WideCharToMultiByte(CP_UTF8, 0, wline.c_str(), (int)wline.size(), nullptr, 0, nullptr, nullptr);
                std::string line(bytes, '\0');
                WideCharToMultiByte(CP_UTF8, 0, wline.c_str(), (int)wline.size(), &line[0], bytes, nullptr, nullptr);
                client.HandleInput(line);
            }
        });
        input_thread.detach();

        io.run();
    }
    catch (std::exception& e)
    {
        std::cerr << "[Client] Exception: " << e.what() << "\n";
        return 1;
    }

    return 0;
}
