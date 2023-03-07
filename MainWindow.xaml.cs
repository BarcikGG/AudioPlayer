using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        private string path;
        private static string[] MusicTypes = { ".mp3", ".wav", ".m4a" };
        private List<string> music = new List<string>();
        private int index;
        private int lastIndex;
        private bool isSorted = false;
        private bool isRepeat = false;
        private bool isPlaying = false;
        private AudioFileReader audioFile;
        private TimeSpan audioLength;
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            Media.Volume = 0.7;
        }

        private void openAlbom_Click(object sender, RoutedEventArgs e)
        {
            string audioTitle;
            CommonOpenFileDialog folder = new CommonOpenFileDialog { IsFolderPicker = true };
            var result = folder.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                AudioList.Items.Clear();
                music.Clear();
                string[] audio = Directory.GetFiles(folder.FileName);
                path = audio[0].Replace(audio[0].Split('\\').Last(), "");

                foreach (string audioFile in audio)
                {
                    audioTitle = audioFile.Substring(path.Length);
                    foreach (string format in MusicTypes)
                    {
                        if (audioTitle.EndsWith(format))
                        {
                            music.Add(path + audioTitle);
                            AudioList.Items.Add(audioTitle.Remove(audioTitle.Length - 4, 4));
                        }
                    }
                }
                AudioList.Items.Refresh();
                AudioList.SelectedItem = music.ElementAt(0);
                Play(0);
            }
        }

        private void audioValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Media.Position = new TimeSpan(Convert.ToInt64(audioSlider.Value));
        }

        private void media_Opened(object sender, RoutedEventArgs e)
        {
            audioSlider.Maximum = Media.NaturalDuration.TimeSpan.Ticks;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (music.Count>0)
            {
                if (PlayButton.Content.Equals(">"))
                {
                    PlayButton.Content = "||";
                    Media.Play();
                    isPlaying = true;

                    cancellationTokenSource = new CancellationTokenSource();
                    _ = UpdatePositionAsync(cancellationTokenSource.Token);
                }
                else
                {
                    Media.Pause();
                    isPlaying= false;
                    cancellationTokenSource.Cancel();
                    PlayButton.Content = ">";
                }
            }
            else MessageBox.Show("Песня не выбрана");
        }

        private void Play(int index)
        {
            cancellationTokenSource = new CancellationTokenSource();
            _ = UpdatePositionAsync(cancellationTokenSource.Token);
            Media.Play();

            audioSlider.Value = 0;
            string musicName = music.ElementAt(index).Substring(path.Length);

            Media.Source = new Uri(music.ElementAt(index));
            audioFile = new AudioFileReader(music.ElementAt(index));
            audioLength = audioFile.TotalTime;
            endlabel.Content = audioLength.ToString(@"mm\:ss");

            Title.Text = musicName.Remove(musicName.Length - 4, 4);
            PlayButton.Content = "||";

            if (index == 0)
            {
                previosButton.IsEnabled = false;
                nextButton.IsEnabled = true;
            }
            else if (index == music.Count - 1)
            {
                nextButton.IsEnabled = false;
                previosButton.IsEnabled = true;
            }
            else
            {
                previosButton.IsEnabled = true;
                nextButton.IsEnabled = true;
            }
        }

        private async Task UpdatePositionAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    audioSlider.Value = Media.Position.Ticks;
                });
                startLabel.Content = Media.Position.ToString(@"mm\:ss");
                endlabel.Content = (Media.NaturalDuration.TimeSpan - Media.Position).ToString(@"mm\:ss");
                
                if (Media.Position == Media.NaturalDuration.TimeSpan & isRepeat == false)
                {
                    nextPlay();
                }
                else if (Media.Position == Media.NaturalDuration.TimeSpan)
                {
                    Play(index);
                }
            }
        }

        private void AudioList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            index = AudioList.SelectedIndex;
            if (index == -1) index = 0;
            lastIndex = index;
            Play(index);
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            nextPlay();
        }

        private void previosButton_Click(object sender, RoutedEventArgs e)
        {
            previousPlay();
        }

        private void nextPlay()
        {
            if (lastIndex < music.Count - 1)
            {
                lastIndex += 1;
                Play(lastIndex);
            }
        }

        private void previousPlay()
        {
            if (lastIndex > 0)
            {
                lastIndex -= 1;
                Play(lastIndex);
            }
        }

        private void ShuffleButton_Click(object sender, EventArgs e)
        {
            if (!isSorted)
            {
                Random rng = new Random();
                music = music.OrderBy(item => rng.Next()).ToList();
            }
            else
            {
                music = music.OrderBy(item => item).ToList();
            }

            UpdateListBox();
            isSorted = !isSorted;
        }

        private void UpdateListBox()
        {
            AudioList.Items.Clear();

            path = music[0].Replace(music[0].Split('\\').Last(), "");
            foreach (string audioFile in music)
            {
                string audioTitle = audioFile.Substring(path.Length);
                AudioList.Items.Add(audioTitle.Remove(audioTitle.Length - 4, 4));
            }
            
            AudioList.Items.Refresh();
            index = 0;
            AudioList.SelectedItem = music[index];
            Play(index);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            Repeat();
        }

        private int Repeat()
        {
            isRepeat = !isRepeat;

            if (isRepeat)
            {
                RepeatButton.Content = "1";
                return 1;
            }
            else
            {
                RepeatButton.Content = "";
                return 0;
            }
        }
    }
}
