using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleChat
{
	public partial class MainWindow: Window
	{
		bool alive = false;
		TcpClient client;
		const int PORT = 8888;
		const string HOST = "192.168.100.20";
		NetworkStream stream;
		string userName; 
		private SynchronizationContext context;

		public MainWindow()
		{
			InitializeComponent();

			EnterChatBtn.IsEnabled = true; // кнопка входа
			LeaveChatBtn.IsEnabled = false; // кнопка выхода
			SendMessageBtn.IsEnabled = false; // кнопка отправки
			MainChatTextBox.IsEnabled = false; // поле для сообщений
			context = SynchronizationContext.Current;
		}

		/// <summary>
		/// Заходит в чат с ником, указанным в NickTextBox или с уникальным ником типа "Guest...".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void EnterChatBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!String.IsNullOrEmpty(NickTextBox.Text))
			{
				userName = NickTextBox.Text;
			}
			else
			{
				userName = "Guest" + Guid.NewGuid().ToString();
			}

			try
			{
				client = new TcpClient();
				client.Connect(HOST, PORT);
				stream = client.GetStream();

				byte[] data = Encoding.Unicode.GetBytes(userName);
				stream.Write(data, 0, data.Length);

				// запускаем задачу на прием сообщений (предпочтительно)
				Task receiveTask = new Task(ReceiveMessages);
				receiveTask.Start();

				// так тоже работает...вроде XD
				//Thread receiveTask = new Thread(new ThreadStart(ReceiveMessages));
				//receiveTask.Start();

				MainChatTextBox.AppendText(Environment.NewLine + "Добро пожаловать " + userName);

				ChangeViewElementsAccessibility(true);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				Disconnect();
			}
		}

		private void SendMessageBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string message = String.Format(MessageTextBox.Text);
				byte[] data = Encoding.Unicode.GetBytes(message);
				stream.Write(data, 0, data.Length);
				MessageTextBox.Clear();
				MainChatTextBox.AppendText(Environment.NewLine + "Вы: " + message);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Слушает входящие сообщения пока "живо" соединение.
		/// </summary>
		private void ReceiveMessages()
		{
			alive = true;
			try
			{
				while (alive)
				{
					byte[] data = new byte[64];
					StringBuilder builder = new StringBuilder();
					int bytes = 0;
					do
					{
						bytes = stream.Read(data, 0, data.Length);
						builder.Append(Encoding.Unicode.GetString(data, 0, data.Length));
					} while (stream.DataAvailable);

					string message = builder.ToString();

					// добавляем полученное сообщение в текстовое поле
					context.Post(delegate (object state)
					{
						MainChatTextBox.AppendText(message + Environment.NewLine);
					}, null);
				}
			}
			catch (Exception ex)
			{
				if (!alive)
					return;
				MessageBox.Show(ex.Message);
				Disconnect();
			}
		}

		private void LeaveChatBtn_Click(object sender, RoutedEventArgs e)
		{
			Disconnect();
		}

		/// <summary>
		/// Разрывает соединение с сервером (выход из чата)
		/// </summary>
		private void Disconnect()
		{
			alive = false;

			// уведомляем сервер о разрыве соединения
			byte[] data = Encoding.Unicode.GetBytes("ext0");
			stream.Write(data, 0, data.Length);

			if (stream != null && stream.CanWrite)
			{
				stream.Close();
			}
			if (client != null)
			{
				client.Close();
			}

			MainChatTextBox.AppendText(Environment.NewLine + "Вы вышли из чата.");

			ChangeViewElementsAccessibility(false);
		}

		/// <summary>
		/// Меняет доступность элементов на форме в зависимости от состояния соединения.
		/// </summary>
		/// <param name="isConnected">Указывает на состояние соединения.</param>
		private void ChangeViewElementsAccessibility(bool isConnected)
		{
			if (isConnected)
			{
				EnterChatBtn.IsEnabled = false;
				LeaveChatBtn.IsEnabled = true;
				SendMessageBtn.IsEnabled = true;
				NickTextBox.IsEnabled = false;
			}
			else
			{
				EnterChatBtn.IsEnabled = true;
				LeaveChatBtn.IsEnabled = false;
				SendMessageBtn.IsEnabled = false;
				NickTextBox.IsEnabled = true;
			}
		}
	}
}
