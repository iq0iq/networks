#include <boost/asio.hpp>
#include <iostream>
#include <thread>

using boost::asio::ip::tcp;

void receive_messages(tcp::socket& socket) {
    std::vector<char> buffer(1024);
    try {
        while (true) {
            std::size_t length = socket.read_some(boost::asio::buffer(buffer));
            std::cout << "Message: " << std::string(buffer.begin(), buffer.end()) << std::endl;
        }
    }
    catch (...) {}
}

int main() {
    boost::asio::io_context io_context;
    tcp::socket socket(io_context);
    tcp::resolver resolver(io_context);
    boost::asio::connect(socket, resolver.resolve("127.0.0.1", "5000"));

    std::thread(receive_messages, std::ref(socket)).detach();

    std::string message;
    while (std::getline(std::cin, message)) {
        boost::asio::write(socket, boost::asio::buffer(message, message.size()));
        if (message == "exit") break;
    }
    socket.close();
    return 0;
}
