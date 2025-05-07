#include <boost/asio.hpp>
#include <iostream>
#include <thread>

using boost::asio::ip::tcp;

std::string log_in(tcp::socket& socket) {
    std::string username, password, result;
    while (result != "Accessed") {
        std::cout << "Enter username:\n";
        std::getline(std::cin, username);
        std::cout << "Enter password:\n";
        std::getline(std::cin, password);

        std::string user_data = username + ": " + password;
        boost::asio::write(socket, boost::asio::buffer(user_data, user_data.size()));
        std::vector<char> response(10);
        std::size_t length = socket.read_some(boost::asio::buffer(response));
        result = std::string(response.begin(), response.begin() + length);
        std::cout << result << std::endl;
    }
    return username;
}

void receive_messages(tcp::socket& socket) {
    std::vector<char> buffer(1024);
    try {
        while (true) {
            std::size_t length = socket.read_some(boost::asio::buffer(buffer));
            std::cout << std::string(buffer.begin(), buffer.begin() + length) << std::endl;
        }
    }
    catch (...) {}
}

int main() {
    boost::asio::io_context io_context; // объект, который управляет асинхронными операциями
    tcp::socket socket(io_context);
    tcp::resolver resolver(io_context); // объект, который будет использоваться для преобразования адреса и порта сервера в конечную точку (endpoint)
    boost::asio::connect(socket, resolver.resolve("127.0.0.1", "5000"));

    std::string username = log_in(socket);

    std::thread(receive_messages, std::ref(socket)).detach();
    std::string message;
    while (std::getline(std::cin, message)) {
        boost::asio::write(socket, boost::asio::buffer(username + ": " + message, username.size() + message.size() + 2));
        if (message == "exit") break;
    }
    socket.close();
    return 0;
}
