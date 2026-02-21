#include "Network/PacketHeader.h"
#include "Network/PacketCodec.h"
#include "game_packet.pb.h"
#include <boost/asio.hpp>
#include <iostream>
#include <string>
#include <thread>
#include <vector>

using boost::asio::ip::tcp;
using namespace Yiso::Network;

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
        std::cout << "[Client] 채팅 입력 후 Enter:\n";

        DoReadHeader();
    }

    // stdin 스레드에서 호출 → io_context 스레드로 전달
    void SendChat(const std::string& text)
    {
        boost::asio::post(io_, [this, text]()
        {
            yiso::game::C2S_Chat msg;
            msg.set_message(text);
            auto frame = PacketCodec::Encode(PacketType::C2S_CHAT, msg);
            boost::asio::write(socket_, boost::asio::buffer(frame));
        });
    }

private:
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

                if (static_cast<PacketType>(header_buf_.type) == PacketType::S2C_CHAT)
                {
                    yiso::game::S2C_Chat msg;
                    if (msg.ParseFromArray(body_buf_.data(), static_cast<int>(body_buf_.size())))
                        std::cout << "[Session " << msg.session_id() << "] " << msg.message() << "\n";
                }

                DoReadHeader();
            }
        );
    }

    boost::asio::io_context& io_;
    tcp::socket socket_;
    PacketHeader header_buf_{};
    std::vector<uint8_t> body_buf_;
};

int main(int argc, char* argv[])
{
    std::string host = "127.0.0.1";
    uint16_t port = 7777;
    if (argc > 1) host = argv[1];
    if (argc > 2) port = static_cast<uint16_t>(std::stoi(argv[2]));

    try
    {
        boost::asio::io_context io;
        DummyClient client(io, host, port);

        // stdin은 별도 스레드에서 blocking read
        std::thread input_thread([&client]()
        {
            std::string line;
            while (std::getline(std::cin, line))
            {
                if (!line.empty())
                    client.SendChat(line);
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
