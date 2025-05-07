#include <boost/asio.hpp>
#include <iostream>
#include <thread>
#include <vector>
#include <fstream>

using boost::asio::ip::tcp;

bool validate_data(const std::string& user_data) {
    std::ifstream file("data.txt");
    std::string line;
    while (std::getline(file, line)) {
        if (line == user_data) {
            return true;
        }
    }
    return false;
}

void handle_client(std::shared_ptr<tcp::socket> client_socket,
    std::vector<std::shared_ptr<tcp::socket>>& clients) {
    try {
        std::vector<char> buffer(1024);
        std::size_t length = 0;
        std::string user_data;
        bool logged_in = false;
        while (!logged_in) {
            length = client_socket->read_some(boost::asio::buffer(buffer));
            user_data = std::string(buffer.begin(), buffer.begin() + length);
            logged_in = validate_data(user_data);
            if (!logged_in) boost::asio::write(*client_socket, boost::asio::buffer("Failed", 6));
        }
        std::string username = user_data.substr(0, user_data.find(':'));
        std::cout << "Client logged in: " << username << std::endl;
        boost::asio::write(*client_socket, boost::asio::buffer("Accessed", 8));
            
        while (true) {
            length = client_socket->read_some(boost::asio::buffer(buffer));
            if (length > 0) {
                std::cout << std::string(buffer.begin(), buffer.begin() + length) << std::endl;
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
    catch (...) {
        clients.erase(std::find(clients.begin(), clients.end(), client_socket));
    }
}

int main() {
    boost::asio::io_context io_context;
    tcp::acceptor acceptor(io_context, tcp::endpoint(tcp::v4(), 5000)); 
    // объект, который будет слушать входящие соединения на IPv4-адресе (tcp::v4()) и порту 5000. Принимает входящие соединения и создает сокеты для общения с клиентами.

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
