using Godot;
using NExLib.Client;
using NExLib.Common;

public class ClientManager : Node
{
	private Client client;
	private bool isClient;
	private Label label;

	public override void _Ready()
	{
		base._Ready();

		if (OS.GetCmdlineArgs().Length > 0 && OS.GetCmdlineArgs()[0] == "client")
		{
			isClient = true;
		}
		else
		{
			return;
		}

		client = new Client();
		label = GetNode<Label>("../Label");
		client.LogHelper.Log += Log;
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

	public override void _Notification(int what)
	{
		if (!isClient)
		{
			return;
		}

		if (what == MainLoop.NotificationWmQuitRequest)
		{
			client.Disconnect();
			client.Close();
		}
	}

	private void Log(LogHelper.LogLevel logLevel, string logMessage)
	{
		if (logLevel == LogHelper.LogLevel.Info)
		{
			GD.Print(logMessage);
			label.Text += $"{logMessage}\n";
		}
		else if (logLevel == LogHelper.LogLevel.Warning)
		{
			GD.PushWarning(logMessage);
			label.Text += $"{logMessage}\n";
		}
		else if (logLevel == LogHelper.LogLevel.Error)
		{
			GD.PushError(logMessage);
			label.Text += $"{logMessage}\n";
		}
	}
}
