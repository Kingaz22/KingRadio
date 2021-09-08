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

namespace KingRadio.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private IEnumerable<Radiostation> _listUrl;
        public bool _play { get; set; }
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
                FilesSelectedItemCreate(value);
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
                OnPropertyChanged(nameof(Play));
            }
        }

        #endregion

        public BaseViewModel()
        {
            Play = false;
            RadioChange = false;
            PackIcon = "Play";
            Topmost = true;
            //Volume = 0.1f;
            //Task = new Task(() => PlayMp3FromUrl("https://a1.radioheart.ru:8001/radiogomelfm"));
            //Json();
            //Task.Start();
            FilesStationOpen();
            FilesSelectedItemOpen();
            //SelectedItem = ListUrl.ToList()[0];
            using var mf = new MediaFoundationReader(SelectedItem.Url);
            using var wo = new WaveOutEvent();
            Volume = (int)(wo.Volume * 100);

            Task = new Task(PlayMp3FromUrl);

            Task.Start();

            //Json();

            //JsonWrite();
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
            get
            {
                return _topmostCommand ??= new RelayCommand((o) =>
                {
                    Topmost = !Topmost;
                });
            }
        }

        #endregion

        public void FilesStationOpen()
        {
            using var x = new HttpClient();
            var y = x.GetAsync($"https://raw.githubusercontent.com/Kingaz22/KingRadio/main/Station.json").Result;
            var json = y.Content.ReadAsStringAsync().Result;

            ListUrl = JsonSerializer.Deserialize<IEnumerable<Radiostation>>(json);

            //var asd = File.ReadAllText($"https://raw.githubusercontent.com/Kingaz22/KingRadio/main/Station.json");
            //try
            //{
            //    ListUrl = JsonSerializer.Deserialize<IEnumerable<Radiostation>>($"https://raw.githubusercontent.com/Kingaz22/KingRadio/main/Station.json");
            //}
            //catch (JsonException)
            //{
            //    FilesStationCreate(new List<Radiostation> { new Radiostation() { Name = "Радио Unistar", Url = "https://advertizer.hoster.by/unistar/unistar-128kb/icecast.audio" } });
            //    FilesStationOpen();
            //}
        }

        public void FilesStationCreate(IEnumerable<Radiostation> a)
        {
            //File.WriteAllText("Station.json", JsonSerializer.Serialize(a, Options));
        }

        public void FilesSelectedItemOpen()
        {
            if (new FileInfo("SelectedItem.json").Exists)
            {
                var asd = File.ReadAllText("SelectedItem.json");
                try
                {
                    var a = JsonSerializer.Deserialize<Radiostation>(asd);
                    SelectedItem = ListUrl.First(x => x.Url == a.Url);
                }
                catch (JsonException)
                {
                    FilesSelectedItemCreate(ListUrl.ToList()[0]);
                    FilesSelectedItemOpen();
                }
            }
            else
            {
                FilesSelectedItemCreate(ListUrl.ToList()[0]);
                FilesSelectedItemOpen();
            }
        }
        public void FilesSelectedItemCreate(Radiostation a)
        {
            File.WriteAllText("SelectedItem.json", JsonSerializer.Serialize(a, Options));
        }

        public void Json()
        {
            
            var sdList = new List<Radiostation>
            {
                new Radiostation() { Name = "Гомель FM", Url = "https://a1.radioheart.ru:8001/radiogomelfm" },
                new Radiostation() { Name = "Радио Unistar", Url = "https://advertizer.hoster.by/unistar/unistar-128kb/icecast.audio" },
                new Radiostation() { Name = "Русское Радио", Url = "https://stream.hoster.by/rusradio/russkoe/icecast.audio" },
                new Radiostation() { Name = "Юмор FM", Url = "http://live.humorfm.by:8000/veseloe" },
                new Radiostation() { Name = "Новое Радио", Url = "https://live.novoeradio.by:444/live/novoeradio_aac128/icecast.audio" },
                new Radiostation() { Name = "Душевное Радио", Url = "https://stream.hoster.by/pilotfm/audio/icecast.audio" },
                new Radiostation() { Name = "Радио РОКС", Url = "http://de.streams.radioplayer.by:8000/live" },
                new Radiostation() { Name = "Радио Record Rock", Url = "https://icecast228.ptspb.ru/rock_128" }
            };
            File.WriteAllText("Station.json", JsonSerializer.Serialize<IEnumerable<Radiostation>>(sdList, Options));
        }

        public void JsonWrite()
        {
            var asd = File.ReadAllText("user.json");
            ListUrl = JsonSerializer.Deserialize<IEnumerable<Radiostation>>(asd);
            
            SelectedItem = ListUrl.ToList()[3];
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
