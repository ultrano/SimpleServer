using System;
using Gtk;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;

public partial class MainWindow : Gtk.Window
{
	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	// Thread signal.
	public static ManualResetEvent allDone = new ManualResetEvent(false);

	// Golobal Sockeet define for listener 
	public System.Net.Sockets.Socket g_listener = null;

	// waitting player
	//StateObject waitingPlayer = null;

	public void LetListen()
	{
		ListenButton.Sensitive = false;

		IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 8000);
		g_listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		try
		{
			g_listener.Bind(localEndPoint);
			g_listener.Listen(100);

			ThreadStart acceptThread = delegate ()
			{
				while (true)
				{
					var args = new SocketAsyncEventArgs();
					args.Completed += OnAccepted;
					StartAccept(args);
				}
			};

			new Thread(acceptThread).Start();

			Log("Listening has been started");
		}
		catch (SocketException se)
		{
			Log((string.Format("StartListening [SocketException] Error : {0} ", se.Message.ToString())));
		}
		catch (Exception ex)
		{
			Log((string.Format("StartListening [Exception] Error : {0} ", ex.Message.ToString())));
		}
	}

	private void StartAccept(SocketAsyncEventArgs args)
	{
		args.AcceptSocket = null;

		if (!g_listener.AcceptAsync(args))
			ProcessAccept(args);
	}

	private void ProcessAccept(SocketAsyncEventArgs args)
	{
		Player player = new Player(args.AcceptSocket);
		player.SendAsync(Encoding.UTF8.GetBytes("hello there?"));
	}

	private void OnAccepted(object sender, SocketAsyncEventArgs args)
	{
		ProcessAccept(args);
	}

	public void Log(string message)
	{
		LogView.Buffer.Text += message;
		LogView.Buffer.Text += "\n";
	}

	protected void OnListenButtonClicked(object sender, EventArgs e)
	{
		LetListen();
	}
}

/* // determine which type of operation just completed and call the associated handler
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a receive or send");
        }   */
//EventHandler<SocketAsyncEventArgs>
// State object for reading client data asynchronously
public class Player
{
	public const int BufferSize = 64;
	public Player opponent = null;

	private System.Net.Sockets.Socket socket = null;
	private SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
	private SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
	private byte[] sendBuf = new byte[BufferSize];
	private byte[] recvBuf = new byte[BufferSize];

	public Player(System.Net.Sockets.Socket socket)
	{
		this.socket = socket;

		recvArgs.SetBuffer(recvBuf, 0, recvBuf.Length);
		recvArgs.Completed += OnReceived;

		sendArgs.SetBuffer(sendBuf, 0, sendBuf.Length);
		sendArgs.Completed += OnSended;
	}

	public void CloseSocket()
	{
		try
		{
			socket.Shutdown(SocketShutdown.Send);
		}
		catch (Exception) { }

		socket.Close();
	}

	public void ReceiveAsync()
	{
		if (!socket.ReceiveAsync(recvArgs))
			ProcessReceive(recvArgs);
	}

	public void SendAsync(byte[] buf)
	{
		Buffer.BlockCopy(buf, 0, sendBuf, 0, sendBuf.Length);

		if (!socket.SendAsync(recvArgs))
			ProcessSend(recvArgs);
	}

	private void ProcessReceive(SocketAsyncEventArgs args)
	{
		if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
		{
			if (opponent != null)
				opponent.SendAsync(args.Buffer);
			
			ReceiveAsync();
		}
		else
		{
			CloseSocket();
		}
	}

	private void ProcessSend(SocketAsyncEventArgs args)
	{
		if (args.SocketError == SocketError.Success)
		{
		}
		else
		{
			CloseSocket();
		}
	}

	private void OnReceived(object sender, SocketAsyncEventArgs args)
	{
		ProcessReceive(args);
	}

	private void OnSended(object sender, SocketAsyncEventArgs args)
	{
		ProcessSend(args);
	}

}