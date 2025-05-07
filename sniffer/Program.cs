//Для захвата пакетов сокетом
//он должен иметь тип raw
//с протоколом IP
using System.Net.Sockets;
using System.Net;
using System.Reflection.PortableExecutable;
using System;
using System.Net.NetworkInformation;
using System.Linq.Expressions;

Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

Console.WriteLine(get_ip());
// Привязываем сокет к выбранному IP
mainSocket.Bind(new IPEndPoint(IPAddress.Parse(get_ip()), 0));
//Устанавливаем опции у сокета
mainSocket.SetSocketOption(SocketOptionLevel.IP,  //Принимать только IP пакеты
SocketOptionName.HeaderIncluded, //Включать заголовок
true);
byte[] byTrue = new byte[4]{1, 0, 0, 0};
byte[] byOut = new byte[4];
//Socket.IOControl это аналог метода WSAIoctl в Winsock 2
mainSocket.IOControl(IOControlCode.ReceiveAll,  //SIO_RCVALL of Winsock
byTrue, byOut);
byte[] byteData = new byte[4096];
//Начинаем приём асинхронный приём пакетов
mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
new AsyncCallback(OnReceive), byteData);
Console.ReadKey();

string get_ip() {
    var udpSocket = new UdpClient();
    var hostEntry = Dns.GetHostEntry("google.com");
    var ipAddress = hostEntry.AddressList[0];
    IPEndPoint endPoint = new IPEndPoint(ipAddress, 0);
    udpSocket.Connect(endPoint);
    var localEndPoint = udpSocket.Client.LocalEndPoint as IPEndPoint;
    return localEndPoint.Address.ToString();
}

void OnReceive(IAsyncResult ar) {
    byte[] byteData = (byte[])ar.AsyncState;
    try {
        int bytesRead = mainSocket.EndReceive(ar);
        IPHeader ipHeader = new IPHeader(byteData, bytesRead);
        Console.WriteLine("Пакет:");
        Console.WriteLine($"  Источник IP: {new IPAddress(ipHeader.uiSourceIPAddress)}");
        Console.WriteLine($"  Назначение IP: {new IPAddress(ipHeader.uiDestinationIPAddress)}");
        Console.WriteLine($"  Размер: {ipHeader.usTotalLength} байт");
        Console.WriteLine($"  Контрольная сумма: {ipHeader.sChecksum}");
        string result = ipHeader.byProtocol switch {
            1 => "ICMP",
            6 => "TCP",
            17 => "UDP",
            53 => "DNS",
            _ => "Unknown"
        };
        Console.WriteLine($"  Протокол: {result}");
        // Начинаем новый прием пакетов
        mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), byteData);
    }
    catch (Exception ex) {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

public class IPHeader {
//Поля IP заголовка
private byte byVersionAndHeaderLength; // Восемь бит для версии 
// и длины 
private byte byDifferentiatedServices; // Восемь бит для дифференцированного 
                                       // сервиса
public ushort usTotalLength;
private ushort usIdentification;
private ushort usFlagsAndOffset;
private byte byTTL;
public byte byProtocol;
public short sChecksum;
public uint uiSourceIPAddress;
// 16 бит для общей длины 
// 16 бит для идентификатора
// 16 бит для флагов, фрагментов 
// смещения 
// 8 бит для TTL (Time To Live) 
// 8 бит для базового протокола
// 16 бит для контрольной суммы 
//  заголовка 
// 32 бита для адреса источника IP 
public uint uiDestinationIPAddress;   // 32 бита для IP назначения 
//Конец полей IP заголовка   
private byte byHeaderLength;
//Длина заголовка
private byte[] byIPData = new byte[4096]; //Данные в дейтаграмме
public IPHeader(byte[] byBuffer, int nReceived) {
    try {
        //Создаём MemoryStream для принимаемых данных
        MemoryStream memoryStream = new MemoryStream(byBuffer, 0, nReceived);
        //Далее создаем BinaryReader для чтения MemoryStream
        BinaryReader binaryReader = new BinaryReader(memoryStream);
        //Первые 8 бит содержат верисю и длину заголовка
        //считываем 8 бит = 1 байт
        byVersionAndHeaderLength = binaryReader.ReadByte();
        //Следующие 8 бит содержат дифф. сервис
        byDifferentiatedServices = binaryReader.ReadByte();
        //Следующие 8 бит содержат общую длину дейтаграммы
        usTotalLength =
       (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        //16 байт для идентификатора
        usIdentification =
       (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        //8 бит для флагов, фрагментов, смещений
        usFlagsAndOffset =
       (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        //8 бит для TTL
        byTTL = binaryReader.ReadByte();
        //8 бит для базового протокола
        byProtocol = binaryReader.ReadByte();
        //16 бит для контрольной суммы
        sChecksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        //32 бита для IP источника
        uiSourceIPAddress = (uint)(binaryReader.ReadInt32());
        //32 бита IP назначения
        uiDestinationIPAddress = (uint)(binaryReader.ReadInt32());
        //Высчитываем длину заголовка
        byHeaderLength = byVersionAndHeaderLength;
        //Последние 4 бита в версии и длине заголовка содержат длину заголовка
        //выполняем простые арифметические операции для их извлечения
        byHeaderLength <<= 4;
        byHeaderLength >>= 4;
        //Умножаем на 4 чтобы получить точную длину заголовка
        byHeaderLength *= 4;
        //Копируем данные (которые содержат информацию в соответствии с типом 
        //основного протокола) в другой массив
        Array.Copy(byBuffer,
        byHeaderLength, //копируем с конца заголовка
        byIPData, 0, usTotalLength - byHeaderLength);
    }
    catch (Exception ex) {
        // MessageBox.Show(ex.Message, "MJsniff", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
}
