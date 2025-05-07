#include <boost/asio.hpp>
#include <iostream>
#include <thread>
#include <vector>

using boost::asio::ip::tcp;

void handle_client(std::shared_ptr<tcp::socket> client_socket, 
    std::vector<std::shared_ptr<tcp::socket>>& clients) {
    std::vector<char> buffer(1024);
    try {
        while (true) {
            size_t length = client_socket->read_some(boost::asio::buffer(buffer));
            if (length > 0) {
                std::cout << "Message: " << std::string(buffer.begin(), buffer.end()) << std::endl;
                for (const auto& client : clients) {
                    if (client != client_socket) {
                        boost::asio::write(*client, boost::asio::buffer(buffer, length));
                    }
                }
            }
            else {
                break;
            }
        }
    }
    catch (...) {}
}

int main() {
    boost::asio::io_context io_context;
    tcp::acceptor acceptor(io_context, tcp::endpoint(tcp::v4(), 5000));

    std::cout << "Server is running" << std::endl;
    std::vector<std::shared_ptr<tcp::socket>> clients;
    while (true) {
        auto client_socket = std::make_shared<tcp::socket>(io_context);
        acceptor.accept(*client_socket);
        clients.emplace_back(client_socket);
        std::cout << "Client connected" << std::endl;
        std::thread(handle_client, client_socket, std::ref(clients)).detach();
    }
    return 0;
}
