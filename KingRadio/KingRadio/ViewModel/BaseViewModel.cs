using KingRadio.Commands;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KingRadio.Model;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using KingRadio.Properties;

namespace KingRadio.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private IEnumerable<Radiostation> _listUrl;
        private bool _play;
        public bool RadioChange { get; set; }
        public Task Task { get; set; }
        private int _volume;
        private Radiostation _selectedItem;
        private string _packIcon;
        private bool _topmost;

        private JsonSerializerOptions Options { get; } = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true
        };

        #region GetSet

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public IEnumerable<Radiostation> ListUrl
        {
            get => _listUrl;
            set
            {
                _listUrl = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public Radiostation SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                Settings.Default.SelectedItem = value.Url;
                Settings.Default.Save();
                if (Play)
                {
                    Play = false;
                    RadioChange = true;
                }
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public string PackIcon
        {
            get => _packIcon;
            set
            {
                _packIcon = value;
                Settings.Default.PackIcon = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PackIcon));
            }
        }

        public bool Topmost
        {
            get => _topmost;
            set
            {
                _topmost = value;
                OnPropertyChanged(nameof(Topmost));
            }
        }

        public bool Play
        {
            get => _play;
            set
            {
                _play = value;
                Settings.Default.Play = Play;
                Settings.Default.Save();
                OnPropertyChanged(nameof(Play));
            }
        }

        #endregion

        public BaseViewModel()
        {
            Play = Settings.Default.Play;
            RadioChange = false;
            PackIcon = Settings.Default.PackIcon;
            Topmost = true;
            FilesStationOpen();
            if (string.IsNullOrEmpty(Settings.Default.SelectedItem) || ListUrl.All(x => x.Url != Settings.Default.SelectedItem))
            {
                SelectedItem = ListUrl.First();
                using var mf = new MediaFoundationReader(ListUrl.First().Url);
                using var wo = new WaveOutEvent();
                Volume = (int)(wo.Volume * 100);
                Task = new Task(PlayMp3FromUrl);
                Task.Start();
            }
            else
            {
                SelectedItem = ListUrl.First(x => x.Url == Settings.Default.SelectedItem);
                using var mf = new MediaFoundationReader(ListUrl.First(x => x.Url == Settings.Default.SelectedItem).Url);
                using var wo = new WaveOutEvent();
                Volume = (int)(wo.Volume * 100);
                Task = new Task(PlayMp3FromUrl);
                Task.Start();
            }
        }

        #region Команды
        private RelayCommand _playCommand;
        public RelayCommand PlayCommand
        {
            get
            {
                return _playCommand ??= new RelayCommand((o) =>
                {
                    Play = !Play;
                    PackIcon = Play ? "Pause" : "Play";
                });
            }
        }

        private RelayCommand _topmostCommand;

        public RelayCommand TopmostCommand
        {
            get { return _topmostCommand ??= new RelayCommand((o) => { Topmost = !Topmost; }); }
        }
        #endregion

        public void FilesStationOpen()
        {
            using var x = new HttpClient();
            var y = x.GetAsync($"https://raw.githubusercontent.com/Kingaz22/KingRadio/main/Station.json").Result;
            var json = y.Content.ReadAsStringAsync().Result;
            ListUrl = JsonSerializer.Deserialize<IEnumerable<Radiostation>>(json);
        }

        public void PlayMp3FromUrl()
        {
            while (true)
            {
                using var mf = new MediaFoundationReader(SelectedItem.Url);
                using var wo = new WaveOutEvent();
                wo.Init(mf);
                if (RadioChange)
                {
                    Play = true;
                    RadioChange = false;
                }
                wo.Volume = (float)Volume / 100;
                if (Play)
                {
                    wo.Play();
                    while (wo.PlaybackState == PlaybackState.Playing)
                    {
                        wo.Volume = (float)Volume / 100;
                        if (!Play)
                        {
                            wo.Stop();
                            break;
                        }
                        Thread.Sleep(100);
                    }
                }
                Thread.Sleep(100);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
