using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		bool alive = false; // будет ли работать поток для приема
		UdpClient client;
		const int LOCALPORT = 8001; // порт для приема сообщений
		const int REMOTEPORT = 8001; // порт для отправки сообщений
		const int TTL = 20;
		const string HOST = "239.0.0.222"; // хост для групповой рассылки
		IPAddress groupAddress; // адрес для групповой рассылки
		string userName; // имя пользователя в чате
		private SynchronizationContext context; // контекст синхронизации

		public MainWindow()
		{
			InitializeComponent();

			EnterChatBtn.IsEnabled = true; // кнопка входа
			LeaveChatBtn.IsEnabled = false; // кнопка выхода
			SendMessageBtn.IsEnabled = false; // кнопка отправки
			MainChatTextBox.IsEnabled = false; // поле для сообщений
			context = SynchronizationContext.Current;
			groupAddress = IPAddress.Parse(HOST);
		}

		// метод приема сообщений
		private void ReceiveMessages()
		{
			alive = true;
			try
			{
				while (alive)
				{
					IPEndPoint remoteIp = null;
					byte[] data = client.Receive(ref remoteIp);
					string message = Encoding.Unicode.GetString(data);

					// добавляем полученное сообщение в текстовое поле
					context.Post(delegate (object state) {
						MainChatTextBox.AppendText(message + Environment.NewLine);
					}, null);
				}
			}
			catch (ObjectDisposedException)
			{
				if (alive)
					throw;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Enters to chat with nick name specified in the NickTextBox or with unique "Guest..." nick.
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

			NickTextBox.IsEnabled = false;

			try
			{
				client = new UdpClient(LOCALPORT);
				// присоединяемся к групповой рассылке
				client.JoinMulticastGroup(groupAddress, TTL);

				// запускаем задачу на прием сообщений
				Task receiveTask = new Task(ReceiveMessages);
				receiveTask.Start();

				// отправляем первое сообщение о входе нового пользователя
				string message = userName + " вошел в чат";
				byte[] data = Encoding.Unicode.GetBytes(message);
				client.Send(data, data.Length, HOST, REMOTEPORT);

				EnterChatBtn.IsEnabled = false;
				LeaveChatBtn.IsEnabled = true;
				SendMessageBtn.IsEnabled
 = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SendMessageBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string message = String.Format("{0}: {1}", userName, MessageTextBox.Text);
				byte[] data = Encoding.Unicode.GetBytes(message);
				client.Send(data, data.Length, HOST, REMOTEPORT);
				MessageTextBox.Clear();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Exit from chat.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LeaveChatBtn_Click(object sender, RoutedEventArgs e)
		{
			ExitChat();
		}

		// выход из чата
		private void ExitChat()
		{
			string message = userName + " Leaves chat";
			byte[] data = Encoding.Unicode.GetBytes(message);
			client.Send(data, data.Length, HOST, REMOTEPORT);
			client.DropMulticastGroup(groupAddress);

			alive = false;
			client.Close();

			EnterChatBtn.IsEnabled = true; // кнопка входа
			LeaveChatBtn.IsEnabled = false; // кнопка выхода
			SendMessageBtn.IsEnabled = false; // кнопка отправки
			MainChatTextBox.IsEnabled = false; // поле для сообщений
		}


	}
}
