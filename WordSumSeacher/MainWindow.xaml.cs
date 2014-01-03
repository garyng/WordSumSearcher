using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading;

namespace WordSumSeacher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		WordSumHelper _wshSeach = new WordSumHelper();

		private void btnSearch_Click(object sender, RoutedEventArgs e)
		{
			btnSearch.IsEnabled = false;
			txtSum.IsEnabled = false;
			int sum;
			if (int.TryParse(txtSum.Text, out sum))
			{
				_wshSeach.Sum = sum;
				_wshSeach.Search();
			}
			else
			{
				txtSum.Text = "";
				tbStatus.Text = "Please enter a valid number.";
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_wshSeach.OnFileLoaded += () =>
				{
					tbStatus.Text = String.Format("Loaded {0} with {1} words", "words.txt", _wshSeach.Words.Count);
				};
			_wshSeach.OnSearching += (int current, int total) =>
				{
					tbStatus.Dispatcher.Invoke((Action)delegate()
					{
						tbStatus.Text = String.Format("Searching... {0} / {1}", current, total);
					});
				};
			_wshSeach.OnCompleted += (List<string> match) =>
				{
					this.Dispatcher.Invoke((Action)delegate()
					{
						tbStatus.Text = String.Format("Done. Found {0} results. Copied to clipboard.", match.Count);
						lbResult.Items.Clear();
						btnSearch.IsEnabled = true;
						txtSum.IsEnabled = true;
						Clipboard.Clear();
						Clipboard.SetText(string.Join(Environment.NewLine, match));
					});
					match.ForEach(delegate(string item)
					{
						lbResult.Dispatcher.Invoke((Action)delegate()
							{
								lbResult.Items.Add(item);
							});
					});
				};
			_wshSeach.Load("words.txt");
			
		}
	}

	public class WordSumHelper
	{

		Dictionary<char, int> _alphabetCode = new Dictionary<char, int>();
		string _alphabet = "abcdefghijklmnopqrstuvwxyz";

		public delegate void NoParamsHandler();
		public event NoParamsHandler OnFileLoaded;

		public delegate void OnSearchingHandler(int current, int total);
		public event OnSearchingHandler OnSearching;

		public delegate void OnCompletedHandler(List<string> match);
		public event OnCompletedHandler OnCompleted;

		public WordSumHelper()
		{
			int i = 1;
			_alphabet.ToList().ForEach(delegate(char item)
			{
				_alphabetCode.Add(item, i);
				i++;
			});
		}

		public void Load(string filename)
		{
			LoadFile(filename);
		}

		public void Load()
		{
			LoadFile(this.Filename);
		}

		private void LoadFile(string filename)
		{
			StreamReader sr = new StreamReader(filename);
			this.Words = new HashSet<string>(Regex.Split(sr.ReadToEnd(), Environment.NewLine));
			sr.Dispose();
			if (OnFileLoaded != null)
			{
				OnFileLoaded();
			}			
		}

		public void Search()
		{	
			new Thread(delegate() {

				int i = 1;
				int sum = this.Sum;
				HashSet<string> words = this.Words;
				HashSet<string>.Enumerator enumerator = words.GetEnumerator();

				List<string> match = new List<string>();

				while (enumerator.MoveNext())
				{
					int total = 0;
					enumerator.Current.ToLower().ToList().ForEach(delegate(char item)
					{
						int code;
						if (_alphabetCode.TryGetValue(item, out code))
						{
							total += code;
						}
					});
					if (total == sum)
					{
						match.Add(enumerator.Current);
					}
					if (OnSearching != null)
					{
						OnSearching(i, words.Count);
					}
					i++;
				}

				if (OnCompleted != null)
				{
					OnCompleted(match);
				}

			}) { IsBackground = true }.Start() ;
		}

		public string Filename { get; set; }
		public HashSet<string> Words { get; set; }
		public int Sum { get; set; }
	}
}
