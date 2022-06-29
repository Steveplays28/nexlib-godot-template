using Godot;
using NExLib.Common;
using NExLib.Server;

public class ServerManager : Node
{
	private Server server;
	private bool isServer;
	private Label label;

	public override void _Ready()
	{
		base._Ready();

		if (OS.GetCmdlineArgs().Length <= 0 || OS.GetCmdlineArgs()[0] != "client")
		{
			isServer = true;
		}
		else
		{
			return;
		}

		server = new Server(26665);
		label = GetNode<Label>("../Label");
		server.LogHelper.Log += Log;
		server.Start();
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

	public override void _Notification(int what)
	{
		if (!isServer)
		{
			return;
		}

		if (what == MainLoop.NotificationWmQuitRequest)
		{
			server.Stop();
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
