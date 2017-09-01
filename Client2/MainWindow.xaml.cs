using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.Windows.Threading;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Newtonsoft.Json;

namespace Client2
{
    public class AppSettings
    {
        public const string ActiveMQ_URI = "tcp://RobertWang-PC:61616/";

    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Listener listener;
        Publisher publisher;
        public MainWindow()
        {
            InitializeComponent();
            ChatList = new ObservableCollection<Chat>();
            listener = new Listener(this);
            publisher = new Publisher();
            this.chatContentView.ItemsSource = ChatList;
            IntializeActiveMQ();
        }

        public void IntializeActiveMQ()
        {
            listener.AsynchronousReceive();
            //Thread t1 = new Thread(new ThreadStart(listener.Initialize));
            //t1.Start();
        }

        public void UpdateCollection(Chat chat)
        {
            App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
           {
               ChatList.Add(chat);
           });
        }

        public ObservableCollection<Chat> ChatList { get; set; }

        private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            string chtContent = contentTextBox.Text;
            if (!string.IsNullOrEmpty(chtContent))
            {
                Chat chat = new Chat() { Content = chtContent, Date = DateTime.Now.ToShortTimeString(), Name = "Client2", BackColor = Brushes.LightSkyBlue };
                ChatList.Add(chat);
                string jsonString = JsonConvert.SerializeObject(chat);
                publisher.SendMessage(jsonString);
                contentTextBox.Text = string.Empty;
            }
        }
    }

    public class Listener : BaseClass
    {
        public const string DESTINATION = "queue://VMS.BROADCAST";
        MainWindow mainWindow;
        public Listener(MainWindow main)
        {
            mainWindow = main;
        }

        public void Initialize()
        {
            try
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(AppSettings.ActiveMQ_URI);
                IConnection _connection = connectionFactory.CreateConnection();
                _connection.Start();
                ISession _session = _connection.CreateSession();
                IDestination dest = _session.GetDestination(DESTINATION);
                using (IMessageConsumer consumer = _session.CreateConsumer(dest, "JMSCorrelationID = 'Client2-070707'"))
                {
                    Console.WriteLine("Listener started.");
                    Console.WriteLine("Listener created.rn");
                    IMessage message;
                    while (true)
                    {
                        message = consumer.Receive();
                        if (message != null)
                        {
                            ITextMessage textMessage = message as ITextMessage;
                            if (!string.IsNullOrEmpty(textMessage.Text))
                            {
                                Console.WriteLine($"Text={textMessage.Text},TimeStamp={textMessage.NMSTimestamp},MessageId={textMessage.NMSMessageId}");
                                Chat pMesg = JsonConvert.DeserializeObject<Chat>(textMessage.Text);
                                mainWindow.UpdateCollection(pMesg);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Press <ENTER> to exit.");
                Console.Read();
            }
        }


        //Asynchro receive
        public void AsynchronousReceive()
        {
            IConnectionFactory connectionFactory = new ConnectionFactory(AppSettings.ActiveMQ_URI);
            IConnection _connection = connectionFactory.CreateConnection();
            _connection.Start();
            ISession _session = _connection.CreateSession();
            IDestination dest = _session.GetQueue("VMS.BROADCAST");

            ///selector variable ~
            ///seletor syntax
            ///http://timjansen.github.io/jarfiller/guide/jms/selectors.xhtml
            ///message Attributes name
            ///http://activemq.apache.org/activemq-message-properties.html
            ///
            IMessageConsumer consumer = _session.CreateConsumer(dest, "JMSCorrelationID = 'Client2-070707'");
            Console.WriteLine("Listener started.");
            Console.WriteLine("Listener created.rn");
            consumer.Listener += new MessageListener(OnMessage);
        }
        protected void OnMessage(IMessage receivedMsg)
        {
            ITextMessage textMessage = receivedMsg as ITextMessage;
            if (!string.IsNullOrEmpty(textMessage.Text))
            {
                Console.WriteLine(textMessage.Text);
                Chat pMesg = JsonConvert.DeserializeObject<Chat>(textMessage.Text);
                mainWindow.UpdateCollection(pMesg);
            }
        }
    }

    public class Chat
    {
        public string Name { get; set; }

        public string Date { get; set; }

        public string Content { get; set; }
    }

    public class BaseClass
    {
        //public const string DESTINATION = "test.queue";
        //public const string DESTINATION = "topic://test";
        public IConnectionFactory connectionFactory;
        public IConnection _connection;
        public ISession _session;

        public BaseClass()
        {
            connectionFactory = new ConnectionFactory(AppSettings.ActiveMQ_URI);
            if (_connection == null)
            {
                _connection = connectionFactory.CreateConnection();
                _connection.Start();
                _session = _connection.CreateSession();
            }
        }
    }

    public class Publisher : BaseClass
    {
        public Publisher()
        {
        }

        public string SendMessage(string message)
        {
            string result = string.Empty;
            try
            {
                //IDestination destination = _session.GetDestination("queue://VMS.BROADCAST");
                IDestination destination = _session.GetQueue("AGV.BROADCAST");
                using (IMessageProducer producer = _session.CreateProducer(destination))
                {
                    var textMessage = producer.CreateTextMessage(message);
                    textMessage.NMSCorrelationID = "Client1-808080";
                    producer.Send(textMessage);
                }
                result = "Message sent successfully.";
            }
            catch (Exception ex)
            {
                result = ex.Message;
                Console.WriteLine(ex.ToString());
            }
            return result;
        }

    }
}
