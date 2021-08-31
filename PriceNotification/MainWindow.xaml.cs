using Binance;
using Binance.Cache;
using Binance.Utility;
using Binance.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PriceNotification
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BinanceApi binanceApi;
        private List<string> symbols;
        private IEnumerable<Symbol> symbolRoots;
        private string botChannel;
        private string botKey;
        private TelegramBot.TelegramBot telegramBot;
        private Dictionary<string, Dictionary<CandlestickInterval, CandlestickCache>> candlestickPriceCaches = new Dictionary<string, Dictionary<CandlestickInterval, CandlestickCache>>();
        private Dictionary<string, Dictionary<CandlestickInterval, RetryTaskController>> retryTaskPriceControllers = new Dictionary<string, Dictionary<CandlestickInterval, RetryTaskController>>();
        private Dictionary<string, Dictionary<CandlestickInterval, DateTime>> TimeCallPriceCaches = new Dictionary<string, Dictionary<CandlestickInterval, DateTime>>();
        private List<string> timeFrames = new List<string>() { "1m", "3m", "5m", "15m", "1h", "2h", "4h", "6h", "8h", "12h", "1d", "3d", "1w", "1M" };

        public MainWindow()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false)
                .Build();
            InitializeComponent();
            binanceApi = new BinanceApi();
            DataContext = this;
            botKey = configuration.GetSection("Telegram")["botKey"];
            botChannel = configuration.GetSection("Telegram")["channelKey"];
            telegramBot = new TelegramBot.TelegramBot(botKey);
            StopSubCommand = new RelayCommand(StopSub);
        }
        public ICommand StopSubCommand { get; set; }

        public ObservableCollection<SubPrice> SubPrices { get; set; } = new ObservableCollection<SubPrice>();

        private void StopSub(object obj)
        {
            SubPrice subcribe = obj as SubPrice;
            if (subcribe != null)
            {
                Dispatcher.Invoke(() => { SubPrices.Remove(subcribe); });
                subcribe.PropertyChanged -= SubPrice_PropertyChanged;
                var client = candlestickPriceCaches[subcribe.Symbol][subcribe.Candlestick];
                client.Unsubscribe();
                var task = retryTaskPriceControllers[subcribe.Symbol][subcribe.Candlestick];
                task.CancelAsync().Wait();
                TimeCallPriceCaches[subcribe.Symbol][subcribe.Candlestick] = DateTime.MinValue;
                if (candlestickPriceCaches[subcribe.Symbol].ContainsKey(subcribe.Candlestick))
                    candlestickPriceCaches[subcribe.Symbol].Remove(subcribe.Candlestick);
            }
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Verification.  
                if (string.IsNullOrEmpty(this.searchText.Text))
                {
                    // Disable.  
                    this.CloseAutoSuggestionBox();

                    // Info.  
                    return;
                }
                if (symbols == null || !symbols.Any()) return;
                // Enable.  
                this.OpenAutoSuggestionBox();

                // Settings.  
                this.autoList.ItemsSource = symbols.Where(p => p.ToLower().Contains(this.searchText.Text.ToLower())).ToList();
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void AutoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Verification.  
                if (this.autoList.SelectedIndex <= -1)
                {
                    // Disable.  
                    this.CloseAutoSuggestionBox();

                    // Info.  
                    return;
                }

                // Disable.  
                this.CloseAutoSuggestionBox();

                // Settings.  
                this.searchText.Text = this.autoList.SelectedItem.ToString();
                this.searchText.Focus();
                this.autoList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void OpenAutoSuggestionBox()
        {
            try
            {
                if (autoListPopup == null) return;
                // Enable.  
                this.autoListPopup.Visibility = Visibility.Visible;
                this.autoListPopup.IsOpen = true;
                this.autoList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void CloseAutoSuggestionBox()
        {
            try
            {
                // Enable.  
                this.autoListPopup.Visibility = Visibility.Collapsed;
                this.autoListPopup.IsOpen = false;
                this.autoList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private async void LoadData()
        {
            timeFrame.ItemsSource = timeFrames;
            symbolRoots = await binanceApi.GetSymbolsAsync();
            var symbolRootClone = symbolRoots.Where(x => x.QuoteAsset == Asset.USDT).ToList();
            symbols = symbolRootClone.Select(x => x.SymbolRoot).ToList();
        }

        private void AddNew_Clicked(object sender, RoutedEventArgs e)
        {
            SubPrice subPrice = new SubPrice();
            if (!symbols.Any(a => a == searchText.Text)) return;
            subPrice.Symbol = searchText.Text;
            string selectedText = timeFrame.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedText)) return;
            subPrice.CandlestickString = selectedText;
            subPrice.Percent = percentText.Text;
            if (SubPrices.Any(s => s.Symbol == subPrice.Symbol && s.Candlestick == subPrice.Candlestick)) return;
            SubPrices.Add(subPrice);
            SubPrice(subPrice);
            subPrice.PropertyChanged += SubPrice_PropertyChanged;
        }

        private void SubPrice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Percent")
            {
                SubPrice subprice = sender as SubPrice;
                if (TimeCallPriceCaches.ContainsKey(subprice.Symbol) && TimeCallPriceCaches[subprice.Symbol].ContainsKey(subprice.Candlestick))
                {
                    TimeCallPriceCaches[subprice.Symbol][subprice.Candlestick] = DateTime.MinValue;
                }
            }
        }

        private void SubPrice(SubPrice subPrice)
        {
            #region websocket
            //foreach (var symbol in symbols)
            {
                if (!candlestickPriceCaches.ContainsKey(subPrice.Symbol))
                {
                    candlestickPriceCaches.Add(subPrice.Symbol, new Dictionary<CandlestickInterval, CandlestickCache>());
                }
                if (candlestickPriceCaches[subPrice.Symbol].ContainsKey(subPrice.Candlestick)) return;
                var client = new CandlestickCache();
                var webSocket = new BinanceWebSocketStream();
                var controller = new RetryTaskController(webSocket.StreamAsync);
                try
                {

                    client.Subscribe(subPrice.Symbol, subPrice.Candlestick, 2, SubcriberVolumeAction);
                    candlestickPriceCaches[subPrice.Symbol].Add(subPrice.Candlestick, client);
                    webSocket.Uri = BinanceWebSocketStream.CreateUri(client);
                    webSocket.Message += (s, e) => client.HandleMessage(e.Subject, e.Json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                controller.Begin();

                if (retryTaskPriceControllers.ContainsKey(subPrice.Symbol))
                {
                    if (retryTaskPriceControllers[subPrice.Symbol].ContainsKey(subPrice.Candlestick)) retryTaskPriceControllers[subPrice.Symbol][subPrice.Candlestick] = controller;
                    else retryTaskPriceControllers[subPrice.Symbol].Add(subPrice.Candlestick, controller);
                }
                else
                {
                    var dic = new Dictionary<CandlestickInterval, RetryTaskController>();
                    dic.Add(subPrice.Candlestick, controller);
                    retryTaskPriceControllers.Add(subPrice.Symbol, dic);
                }
            }
            #endregion
        }
        private void SubcriberVolumeAction(CandlestickCacheEventArgs args)
        {
            string symbol = args.Symbol;
            CandlestickInterval candlestickInterval = args.CandlestickInterval;
            var lastCandle = args.Candlesticks.LastOrDefault();
            if (TimeCallPriceCaches.ContainsKey(symbol) && TimeCallPriceCaches[symbol].ContainsKey(candlestickInterval))
            {
                if (TimeCallPriceCaches[symbol][candlestickInterval] == lastCandle.OpenTime) return;
            }
            var subPrice = SubPrices.FirstOrDefault(a => a.Symbol == symbol && candlestickInterval == a.Candlestick);
            if (subPrice != null)
            {
                string message = string.Empty;
                if (lastCandle.Close >= (lastCandle.High * subPrice.PercentRoot / 100))
                {
                    message = $"{symbol} price down {subPrice.PercentRoot}% from high price TF:{subPrice.CandlestickString} ";
                }
                if (lastCandle.Close <= (lastCandle.Low * subPrice.PercentRoot / 100))
                {
                    message = $"{symbol} price up {subPrice.PercentRoot}% from low price TF:{subPrice.CandlestickString} ";
                }
                if (!string.IsNullOrEmpty(message))
                {
                    telegramBot.SendMessage(message, botChannel).ConfigureAwait(false);
                    if (TimeCallPriceCaches.ContainsKey(symbol))
                    {
                        if (TimeCallPriceCaches[symbol].ContainsKey(candlestickInterval))
                            TimeCallPriceCaches[symbol][candlestickInterval] = lastCandle.OpenTime;
                        else
                        {
                            TimeCallPriceCaches[symbol].Add(candlestickInterval, lastCandle.OpenTime);
                        }
                    }
                    else
                    {
                        var dic = new Dictionary<CandlestickInterval, DateTime>();
                        dic.Add(candlestickInterval, lastCandle.OpenTime);
                        TimeCallPriceCaches.Add(symbol, dic);
                    }
                }
            }
        }
    }

    public class SubPrice : NotifyChanged
    {
        private decimal percent = 7.1m;
        private string candlestickString;
        public Guid Id { get; set; } = new Guid();
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public long ChatId { get; set; } = 0;
        public CandlestickInterval Candlestick { get; set; }
        public string CandlestickString
        {
            get => candlestickString; set
            {
                candlestickString = value;
                Candlestick = value.AsCandlestickInterval();
            }
        }
        public string Percent
        {
            get => percent.ToString(); set
            {
                decimal.TryParse(value, out percent);
                OnPropertyChanged("Percent");
            }
        }
        public decimal PercentRoot
        {
            get { return percent; }
            set
            {
                percent = value;
                OnPropertyChanged("Percent");
            }
        }
    }
    public class NotifyChanged : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            // Log.DebugFormat("{0}.{1} = {2}", this.GetType().Name, propertyName, storage);
            this.OnPropertyChanged(propertyName);
            return true;
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
}
