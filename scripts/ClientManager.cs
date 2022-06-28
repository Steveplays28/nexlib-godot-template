using Godot;
using NExLib.Client;

public class ClientManager : Node
{
	private Client client;
	private bool isClient;

	public override void _Ready()
	{
		base._Ready();

		if (OS.GetCmdlineArgs()[0] == "client")
		{
			isClient = true;
		}
		else
		{
			return;
		}

		client = new Client();

		client.Connect("127.0.0.1", 26665);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (!isClient)
		{
			return;
		}

		client.Tick();
	}
}
