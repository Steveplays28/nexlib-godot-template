using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NExLib.Common;

namespace NExLib.Server
{
	public class Server
	{
		public const int MaxPacketsReceivedPerTick = 5;

		public UdpClient UdpClient;
		public Dictionary<IPEndPoint, int> ConnectedClientsIpToId = new Dictionary<IPEndPoint, int>();
		public Dictionary<int, IPEndPoint> ConnectedClientsIdToIp = new Dictionary<int, IPEndPoint>();
		public Dictionary<IPEndPoint, int> SavedClientsIpToId = new Dictionary<IPEndPoint, int>();
		public Dictionary<int, IPEndPoint> SavedClientsIdToIp = new Dictionary<int, IPEndPoint>();
		public bool hasStarted;
		public delegate void PacketCallback(Packet packet, IPEndPoint clientIPEndPoint);
		public event PacketCallback PacketReceived;
		public readonly LogHelper LogHelper;

		private readonly IPEndPoint serverEndPoint;

		public Server(int port)
		{
			serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
			UdpClient = new UdpClient(serverEndPoint);

			LogHelper = new LogHelper("[NExLib (Server)]: ");
		}

		public void Start()
		{
			PacketReceived += PacketReceivedHandler;

			hasStarted = true;
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Server started on {serverEndPoint}.");
		}

		public void Stop()
		{
			try
			{
				UdpClient.Close();

				hasStarted = false;
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Successfully closed the UdpClient!");
			}
			catch (SocketException e)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Failed closing the UdpClient: {e}");
			}
		}

		public void Tick()
		{
			ReceivePackets();
		}

		/// <summary>
		/// Sends a packet to all clients.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		public void SendPacketToAll(Packet packet)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to all connected clients
			foreach (IPEndPoint connectedClient in ConnectedClientsIdToIp.Values)
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}

		/// <summary>
		/// Sends a packet to a client.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <param name="recipientId">The client that the packet should be sent to.</param>
		/// <returns></returns>
		public void SendPacketTo(Packet packet, int recipientId)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the specified client
			if (ConnectedClientsIdToIp.TryGetValue(recipientId, out IPEndPoint connectedClient))
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}

		/// <summary>
		/// Receives up to MaxPacketsReceivedPerTick asynchronously.
		/// </summary>
		private async void ReceivePackets()
		{
			for (int i = 0; i < MaxPacketsReceivedPerTick && UdpClient.Available > 0; i++)
			{
				// Extract data from the received packet
				UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				byte[] packetData = udpReceiveResult.Buffer;

				// Create new Packet object from the received packet data and invoke PacketReceived event
				using (Packet packet = new Packet(packetData))
				{
					PacketReceived?.Invoke(packet, remoteEndPoint);
				}
			}
		}

		private void PacketReceivedHandler(Packet packet, IPEndPoint clientIPEndPoint)
		{
			if (packet.ConnectedMethod == (int)PacketMethod.Connect)
			{
				ClientConnected(clientIPEndPoint);
				return;
			}

			if (packet.ConnectedMethod == (int)PacketMethod.Disconnect)
			{
				ClientDisconnected(clientIPEndPoint);
				return;
			}
		}

		private void ClientConnected(IPEndPoint clientIPEndPoint)
		{
			// Check if client is already connected
			if (ConnectedClientsIdToIp.ContainsValue(clientIPEndPoint))
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Client with IP {clientIPEndPoint} tried to connect, but is already connected!");
				return;
			}

			// Accept the client's connection request
			int clientId = ConnectedClientsIdToIp.Count;
			ConnectedClientsIdToIp.Add(clientId, clientIPEndPoint);
			ConnectedClientsIpToId.Add(clientIPEndPoint, clientId);

			// Send a new packet back to the newly connected client
			using (Packet newPacket = new Packet((int)PacketMethod.Connect))
			{
				// Write the client ID to the packet
				newPacket.Writer.Write(clientId);

				SendPacketTo(newPacket, clientId);
			}

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"New client connected from {clientIPEndPoint}");
		}

		private void ClientDisconnected(IPEndPoint clientIPEndPoint)
		{
			// Check if client is already disconnected
			if (!ConnectedClientsIdToIp.ContainsValue(clientIPEndPoint))
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Client with IP {clientIPEndPoint} tried to disconnect, but is already disconnected!");
				return;
			}

			// Disconnect the client
			ConnectedClientsIdToIp.Remove(ConnectedClientsIpToId[clientIPEndPoint]);
			ConnectedClientsIpToId.Remove(clientIPEndPoint);

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Client {clientIPEndPoint} disconnected.");
		}
	}
}
