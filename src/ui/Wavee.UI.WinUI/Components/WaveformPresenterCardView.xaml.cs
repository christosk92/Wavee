using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Windows.System.Threading;
using AudioEffectsLib;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NAudio.Wave;
using Microsoft.UI.Xaml.Hosting;
using UIElement = Microsoft.UI.Xaml.UIElement;

namespace Wavee.UI.WinUI.Components;

public sealed partial class WaveformPresenterCardView : UserControl
{
    List<Rectangle> lstBands;
    public static readonly DependencyProperty CardViewProperty = DependencyProperty.Register(nameof(CardView), typeof(CardView), typeof(WaveformPresenterCardView), new PropertyMetadata(default(CardView)));
    private readonly DispatcherQueue _dispatcherQueue;
    private static readonly NAudio.Wave.IWavePlayer PreviewPlayer = new WaveOutEvent();
    public WaveformPresenterCardView()
    {
        this.InitializeComponent();
        _dispatcherQueue = this.DispatcherQueue;
    }
    private InterveneSampleProvider? _interveneSampleProvider;

    public CardView CardView
    {
        get => (CardView)GetValue(CardViewProperty);
        set => SetValue(CardViewProperty, value);
    }

    private ConcurrentDictionary<UIElement, bool> _inRectangle = new ConcurrentDictionary<UIElement, bool>();

    private async void FirstControl_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        //If the pointer hovered over the card for 4 seconds, then show the tooltip
        //Otherwise, do nothing
        var item = sender as UIElement;
        _inRectangle[item] = false;
        await Task.Delay(1500);
        if (_inRectangle.TryGetValue(item, out _))
        {
            var transition = (TransitionHelper)this.Resources["MyTransitionHelper"];
            transition.Source = (FrameworkElement)item;
            transition.Target = SecondControl;

            _inRectangle[item] = true;
            SecondControlPopup.IsOpen = true;
            await transition.StartAsync();

            lstBands = GenerateBands(40, 6, 5);
            await Task.Run(async () =>
            {
                try
                {
                    if (PreviewPlayer.PlaybackState == PlaybackState.Playing)
                    {
                        PreviewPlayer.Stop();
                        _interveneSampleProvider?.Dispose();
                        _interveneSampleProvider = null;
                    }
                    var bs = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets",
                        "4dba53850d6bfdb9800d53d65fe2e5f1369b9040.mp3");

                    var waveProvider = new Mp3FileReader(bs);
                    _interveneSampleProvider = new InterveneSampleProvider(waveProvider, 2, 65, 40, 20, 500, 46);
                    InterveneSampleProvider.SpectrumDataReady += Capture_AudioDataAvailable;
                    PreviewPlayer.Init(_interveneSampleProvider);
                    PreviewPlayer.Play();
                    while (PreviewPlayer.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception x)
                {

                }
                finally
                {
                    _interveneSampleProvider?.Dispose();
                }
            });

            if (lstBands != null)
            {
                foreach (Rectangle rect in lstBands)
                {
                    rootp.Children.Remove(rect);
                }
                lstBands.Clear();
                lstBands = null;
                GC.Collect();
            }
        }
    }

    private async void FirstControl_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var uiElement = sender as UIElement;
        _inRectangle.Remove(uiElement, out var transitioned);
        if (transitioned)
        {
            var transition = (TransitionHelper)this.Resources["MyTransitionHelper"];
            _interveneSampleProvider?.Dispose();
            PreviewPlayer.Stop();
            await transition.ReverseAsync();
            SecondControlPopup.IsOpen = false;
        }
    }

    private void SecondControl_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        FirstControl_OnPointerExited(FirstControl, null);
    }

    public List<Rectangle> GenerateBands(int amount, double width, double gapwidth)
    {
        if (lstBands != null)
        {
            foreach (Rectangle rect in lstBands)
            {
                rootp.Children.Remove(rect);
            }
            lstBands.Clear();
            GC.Collect();
        }

        List<Rectangle> lstRects = new List<Rectangle>();
        for (int i = 0; i < amount; i++)
        {
            Rectangle rect = new Rectangle();
            rect.Height = 0;
            rect.Width = width;

            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.VerticalAlignment = VerticalAlignment.Bottom;

            double twidth = width + gapwidth;

            rect.Margin = new Thickness((twidth * i), 0, 0, 0);

            AcrylicBrush awb = (AcrylicBrush)Application.Current.Resources["SystemControlAccentAcrylicWindowAccentMediumHighBrush"];
            rect.Fill = awb;
            //rect.Fill = new SolidColorBrush(Colors.Green);
            rootp.Children.Add(rect);

            lstRects.Add(rect);
        }

        return lstRects;
    }

    private void Capture_AudioDataAvailable(object sender, object e)
    {
        try
        {

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, delegate ()
            {
                double[] audioData = new double[1];
                double elapsedTime = 0;
                try
                {
                    audioData = (double[])e;
                }
                catch
                {
                    return;
                }
                for (int i = 0; i < audioData.Length; i += 1)
                {
                    if (lstBands is null) break;
                    //double logamp = 100 + (100 * Math.Log10(e.AudioData[i] / 100));
                    double height = audioData[i] * 200;// *10000;//(e.AudioData[i] * polyheight) / (capture.WaveFormat.SampleRate *100);
                                                       //Debug.WriteLine(height);
                                                       //Debug.WriteLine(height);
                                                       //height = 30* Math.Log(height);

                    if (height < 0)
                    {
                        height = 0;
                        //height = Math.Abs(height);
                    }
                    else if (height > 200)
                    {
                        height = 200;
                        //height = Math.Abs(height);
                    }

                    try
                    {

                        lstBands[i].Height = height;

                    }
                    catch { }
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

        }
    }
}

internal class InterveneSampleProvider : IWaveProvider, IDisposable
{
    private readonly Mp3FileReader _toSampleProvider;
    AudioSpectrum spectrum;
    public InterveneSampleProvider(Mp3FileReader toSampleProvider, double attack, double decay, int bands, double freqMin, double freqMac, double sensitivy)
    {
        _toSampleProvider = toSampleProvider;
        spectrum = new AudioSpectrum(WaveFormat.SampleRate, WaveFormat.BitsPerSample, WaveFormat.Channels);
        spectrum.Attack = attack;
        spectrum.Decay = decay;
        spectrum.Bands = bands;
        spectrum.FreqMax = freqMin;
        spectrum.FreqMax = freqMac;
        spectrum.Sensitivity = sensitivy;
        spectrum.UseFFT = true;
        spectrum.Window = AudioEffectsLib.FastFourierTransform.WFWindow.HammingWindow;
        spectrum.FFTSize = 8192;
        spectrum.FFTBufferSize = 32768;
        spectrum.Channel = 0;
        spectrum.SpectrumDataAvailable += Spectrum_SpectrumDataAvailable;


        spectrum.Start();
    }
    public static event EventHandler<object> SpectrumDataReady;

    private void Spectrum_SpectrumDataAvailable(object sender, AudioVisEventArgs e)
    {
        SpectrumDataReady?.Invoke(sender, e.AudioData);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var read = _toSampleProvider.Read(buffer, offset, count);
        //cast to bytes
        if (read is 0) return 0;
        //copy to new array, make sure offset is respected
        var audioData = new byte[read];
        for (int i = 0; i < read; i++)
        {
            audioData[i] = buffer[i + offset];
        }


        var audioBufferSize = audioData.Length;

        int realbuffersize;
        for (realbuffersize = audioBufferSize - 1; audioData[realbuffersize] == 0; realbuffersize--)
        {

        }

        Debug.WriteLine("Removed " + (audioBufferSize - realbuffersize) + " bytes of zero padding");

        byte[] data = new byte[realbuffersize];

        for (int i = 0; i < realbuffersize; i++)
        {
            data[i] = audioData[i];
        }


        //calculate how many milliseconds of data we have
        double milliseconds = (double)realbuffersize / (double)_toSampleProvider.WaveFormat.AverageBytesPerSecond * 1000.0;
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        ThreadPoolTimer tppt = ThreadPoolTimer.CreateTimer((source) =>
        {
            try
            {
                (spectrum.AudioClient as AudioStreamWaveSource).Write(data, 0, realbuffersize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }, ts);

        return read;
    }

    public WaveFormat WaveFormat => _toSampleProvider.WaveFormat;

    public void Dispose()
    {
        spectrum.SpectrumDataAvailable -= Spectrum_SpectrumDataAvailable;
        spectrum?.Dispose();
        _toSampleProvider.Dispose();
    }
}
