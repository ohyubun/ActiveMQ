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
using System.ComponentModel;

namespace Client1
{
    public class AppSettings
    {
        public const string ActiveMQ_URI = "tcp://RobertWang-PC:61616/";

        public const string MY_NAME= "Clinet1";

    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Chat> ChatList { get; set; }
        private object _lock = new object();

        Publisher publisher;
        Listener consumer;
        public MainWindow()
        {
            InitializeComponent();
            ChatList = new ObservableCollection<Chat>();

            //Chat加上 back color,binding時有跨執行緒的exception,爬很多文都沒用,還是要改用MVVM的架構,先放棄不做
            //BindingOperations.EnableCollectionSynchronization(ChatList, _lock);

            consumer = new Listener(this);
            publisher = new Publisher();
            this.chatContentView.ItemsSource = ChatList;

            IntializeActiveMQ();
        }

        public void IntializeActiveMQ()
        {
            consumer.AsynchronousReceive();
        }


        public void UpdateCollection(Chat chat)
        {
            //lock (_lock)
            //{
            //    ChatList.Add(chat);
            //}

            //this.chatContentView.Dispatcher.Invoke(()=> ChatList.Add(chat));
            //Action action = () => ChatList.Add(chat);
            //var dispatcher = this.chatContentView.Dispatcher;
            ////if (dispatcher.CheckAccess())
            //    action();
            //else
            //    dispatcher.Invoke(action);

            //App.Current.MainWindow.Dispatcher.Invoke(new Action(() => ChatList.Add(chat)));

            App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
            {
                ChatList.Add(chat);
            });
        }


        private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            string chtContent = contentTextBox.Text;
            if (!string.IsNullOrEmpty(chtContent))
            {
                //Chat chat = new Chat() { Content = chtContent, Date = DateTime.Now.ToShortTimeString(), Name = "Client1", BackColor = Brushes.LightSkyBlue };
                Chat chat = new Chat() { Content = chtContent, Date = DateTime.Now.ToShortTimeString(), Name = "Client1"};
                ChatList.Add(chat);
                string jsonString = JsonConvert.SerializeObject(chat);
                publisher.SendMessage(jsonString);
                contentTextBox.Text = string.Empty;
            }
        }
    }

    public class Listener : BaseClass
    {
        MainWindow mainWindow;
        public Listener(MainWindow main)
        {
            mainWindow = main;
        }

        //Syncro receive
        public void Initialize()
        {
            try
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(AppSettings.ActiveMQ_URI);
                IConnection _connection = connectionFactory.CreateConnection();
                _connection.Start();
                ISession _session = _connection.CreateSession();
                IDestination dest = _session.GetDestination("queue://AGV.BROADCAST");
                using (IMessageConsumer consumer = _session.CreateConsumer(dest))
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
                                Console.WriteLine(textMessage.Text);
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
            IDestination dest = _session.GetQueue("AGV.BROADCAST");

            ///selector variable ~
            ///seletor syntax
            ///http://timjansen.github.io/jarfiller/guide/jms/selectors.xhtml
            ///message Attributes name
            ///http://activemq.apache.org/activemq-message-properties.html
            ///
            IMessageConsumer consumer = _session.CreateConsumer(dest, "JMSCorrelationID = 'Client1-808080'");
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
    public class Chat2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private Brush _backColor;
        public Brush BackColor
        {
            get
            { return _backColor; }
            set
            { _backColor = value; NotifyPropertyChanged("BackColor"); }
        }
        private string _name;
        public string Name
        { 
            get
            { return _name; }
            set
            { _name = value; NotifyPropertyChanged("Name"); }
        }
        private string _date;
        public string Date
         { 
            get
            { return _date; }
            set
            {
                _date = value; NotifyPropertyChanged("Date");
            }
        }
        private string _content;
        public string Content
        {
            get
            { return _content; }
            set
            {
                _content = value; NotifyPropertyChanged("Content");
            }
        }
    }
    public class Chat
    {
        //public Brush BackColor { get; set; }

        public string Name { get; set; }

        public string Date { get; set; }

        public string Content { get; set; }
    }

    public class BaseClass
    {
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
                IDestination destination = _session.GetQueue("VMS.BROADCAST");
                using (IMessageProducer producer = _session.CreateProducer(destination))
                {
                    var textMessage = producer.CreateTextMessage(message);
                    textMessage.NMSMessageId = $"MSG_{DateTime.Now.Ticks}";
                    textMessage.NMSCorrelationID = "Client2-070707";
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
