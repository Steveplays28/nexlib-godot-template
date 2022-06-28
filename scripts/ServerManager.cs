using Godot;
using NExLib.Server;

public class ServerManager : Node
{
	private Server server;
	private bool isServer;

	public override void _Ready()
	{
		base._Ready();

		if (OS.GetCmdlineArgs()[0] == "server")
		{
			isServer = true;
		}
		else
		{
			return;
		}

		server = new Server(26665);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (!isServer)
		{
			return;
		}

		server.Tick();
	}
}
