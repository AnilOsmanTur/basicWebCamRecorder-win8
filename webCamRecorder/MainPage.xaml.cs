using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation; // *
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging; // *
using Windows.UI.Xaml.Navigation;
using Windows.Media; // *
using Windows.Media.Capture; // *
using Windows.Media.MediaProperties; // *
using Windows.Storage; // * 


namespace webCamRecorder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private MediaCapture mediaCaptureMgr;
        private StorageFile recordStorageFile;
        private bool isRecording;
        private bool isSuspended;
        private bool isPreviewing;

        private TypedEventHandler<SystemMediaTransportControls, SystemMediaTransportControlsPropertyChangedEventArgs> mediaPropertyChanged;

        private readonly String VIDEO_FILE_NAME = "video.mp4";

        public MainPage()
        {
            this.InitializeComponent();
            initScenario();
            mediaPropertyChanged += new TypedEventHandler<SystemMediaTransportControls, SystemMediaTransportControlsPropertyChangedEventArgs>(SystemMediaControls_PropertyChanged);
        
        }

        // initialization process
        private void initScenario()
        {
            startDeviceBtn.IsEnabled = true;
            startPreviewBtn.IsEnabled = false;
            recordBtn.IsEnabled = false;
            recordBtn.Content = "Start Recording";

            isRecording = false;
            isSuspended = false;
            isPreviewing = false;

            previewElement.Source = null;
            recordedElement.Source = null;

            ShowStatusMessage("");
        }

        // closing process
        private async void closingScenario()
        {
            try
            {
                if(isRecording)
                {
                    ShowStatusMessage("Stopping record");

                    await mediaCaptureMgr.StopRecordAsync();
                    isRecording = false;
                }
                if(isPreviewing)
                {
                    ShowStatusMessage("Stopping record");

                    await mediaCaptureMgr.StopPreviewAsync();
                    isRecording = false;
                }
                if(mediaCaptureMgr != null)
                {
                    ShowStatusMessage("Stopping Camera");
                    previewElement.Source = null;
                    mediaCaptureMgr.Dispose();
                }

            }
            catch (Exception e)
            {

            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemMediaTransportControls systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
            systemMediaControls.PropertyChanged += mediaPropertyChanged;

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SystemMediaTransportControls systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
            systemMediaControls.PropertyChanged -= mediaPropertyChanged;
            closingScenario();

            base.OnNavigatingFrom(e);
        }

        private async void SystemMediaControls_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs e)
        {
            switch (e.Property)
            {
                case SystemMediaTransportControlsProperty.SoundLevel:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (sender.SoundLevel != SoundLevel.Muted)
                        {
                            initScenario();
                        }
                        else
                        {
                            initScenario();
                        }
                    });
                    break;

                default:
                    break;
            }
        }


        public async void RecordLimitationExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        {

            if (isRecording)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        ShowStatusMessage("Stopping Record on exceeding max record duration");
                        await mediaCaptureMgr.StopRecordAsync();
                        isRecording = false;
                        recordBtn.Content = "Start Recording";
                        recordBtn.IsEnabled = true;
                        ShowStatusMessage("Stopped record on exceeding max record duration:" + recordStorageFile.Path);

                    }
                    catch (Exception e)
                    {
                        ShowExceptionMessage(e);
                    }
                });
            }
        }

        public async void Failed(Windows.Media.Capture.MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ShowStatusMessage("Fatal error" + currentFailure.Message);
                });
            }
            catch (Exception e)
            {
                ShowExceptionMessage(e);
            }
        }

        internal async void startDeviceBtn_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                startDeviceBtn.IsEnabled = false;
                ShowStatusMessage("Starting device");
                mediaCaptureMgr = new MediaCapture();
                await mediaCaptureMgr.InitializeAsync();

                if (mediaCaptureMgr.MediaCaptureSettings.VideoDeviceId != "" && mediaCaptureMgr.MediaCaptureSettings.AudioDeviceId != "")
                {
                    startPreviewBtn.IsEnabled = true;
                    recordBtn.IsEnabled = true;

                    ShowStatusMessage("Device initialized successful");

                    mediaCaptureMgr.RecordLimitationExceeded += new RecordLimitationExceededEventHandler(RecordLimitationExceeded);
                    mediaCaptureMgr.Failed += new MediaCaptureFailedEventHandler(Failed);
                }
                else
                {
                    startDeviceBtn.IsEnabled = true;
                    ShowStatusMessage("No VideoDevice/AudioDevice Found");
                }
            }
            catch (Exception exception)
            {
                ShowExceptionMessage(exception);
            }
        }

        internal async void startPreviewBtn_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            isPreviewing = false;
            try
            {
                ShowStatusMessage("Starting preview");
                startPreviewBtn.IsEnabled = false;

                previewElement.Source = mediaCaptureMgr;
                await mediaCaptureMgr.StartPreviewAsync();
                ShowStatusMessage("Start preview successful");

            }
            catch (Exception exception)
            {
                isPreviewing = false;
                previewElement.Source = null;
                startPreviewBtn.IsEnabled = true;
                ShowExceptionMessage(exception);
            }
        }

        private async void recording()
        {
            try
            {
                ShowStatusMessage("Starting Record");
                String fileName;
                fileName = VIDEO_FILE_NAME;

                recordStorageFile = await KnownFolders.VideosLibrary.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                ShowStatusMessage("Create record file successful");

                MediaEncodingProfile recordProfile = null;
                recordProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                await mediaCaptureMgr.StartRecordToStorageFileAsync(recordProfile, recordStorageFile);
                isRecording = true;
                recordBtn.IsEnabled = true;
                recordBtn.Content = "Stop Record";

                ShowStatusMessage("Start Record successful");
            }
            catch
            {

            }
        }

        internal async void recordBtn_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                recordBtn.IsEnabled = false;
                recordedElement.Source = null;

                if (recordBtn.Content.ToString() == "Start Recording")
                {
                    recording();
                }
                else // stop recording
                {
                    ShowStatusMessage("Stopping Record");

                    await mediaCaptureMgr.StopRecordAsync();
                    recordBtn.IsEnabled = true;
                    recordBtn.Content = "Start Recording";

                    ShowStatusMessage("Stop record successful");
                    if(!isSuspended)
                    {
                        var stream = await recordStorageFile.OpenAsync(FileAccessMode.Read);

                        ShowStatusMessage("Record file opened");
                        ShowStatusMessage(this.recordStorageFile.Path);
                        recordedElement.AutoPlay = true;
                        recordedElement.SetSource(stream, this.recordStorageFile.FileType);
                        recordedElement.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowExceptionMessage(ex);
                isRecording = false;
                recordBtn.IsEnabled = true;
                recordBtn.Content = "Start Recording";
            }
        }


        private void ShowStatusMessage(String text)
        {
            statusText.Text = text;
        }

        private void ShowExceptionMessage(Exception ex)
        {
            statusText.Text = ex.Message;
        }

    }
}
