Imports System.IO
Imports System.Windows.Threading
Imports NAudio.Wave

' Todo: audio doesn't stop on exiting

Public Class AudioPlayer
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        RefreshList()

        ' https://stackoverflow.com/questions/7291461/
        AddHandler audio_timer.Tick, AddressOf Audio_Timer_Tick
    End Sub


    Sub RefreshList()
        lsbAudioFiles.Items.Clear()

        Dim dir_info = FileIO.FileSystem.GetDirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                MainWindow.generated_dir$)

        For Each file In dir_info.EnumerateFiles.Where(Function(x) x.Extension = ".mp3")
            Dim lbi As New ListBoxItem With {
                .Content = file.Name
            }

            lsbAudioFiles.Items.Add(lbi)
        Next
    End Sub


    Sub Audio_Timer_Tick(sender As Object, e As EventArgs)
        If mp3_reader Is Nothing Then
            lblPlayerTime.Content = "0:00 / 0:00"
        Else
            ' https://stackoverflow.com/questions/39548466
            Dim bytes = wave_out.OutputWaveFormat.AverageBytesPerSecond

            Dim seconds# = mp3_reader.Position / bytes
            Dim mins = seconds \ 60
            Dim secs = Int(seconds Mod 60)

            Dim len_seconds# = mp3_reader.Length / bytes
            Dim len_mins = len_seconds \ 60
            Dim len_secs = Int(len_seconds Mod 60)

            lblPlayerTime.Content = $"{mins}:{secs:00} / {len_mins}:{len_secs:00}"

            If Not isDraggingSeek Then
                sldPlayer.Maximum = mp3_reader.Length / bytes
                sldPlayer.Value = mp3_reader.Position / bytes
            End If
        End If
    End Sub

    Dim audio_timer As New DispatcherTimer With {
        .Interval = TimeSpan.FromSeconds(1)
    }

    Dim current_filename$

    Function resourceUri(relative_path$) As Uri
        ' https://stackoverflow.com/questions/12862416
        ' alt: Windows.Application.ResourceAssembly.FullName
        Dim assembly_name$ = Reflection.Assembly.GetExecutingAssembly.GetName.Name

        ' https://stackoverflow.com/questions/350027
        resourceUri = New Uri($"pack://application:,,,/{assembly_name};component/{relative_path}")
    End Function

    Sub PlayOrPause()
        Dim lbi As ListBoxItem = lsbAudioFiles.SelectedItem

        If lbi Is Nothing Then Exit Sub


        Try
            Dim new_filename = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                    MainWindow.generated_dir + Path.DirectorySeparatorChar +
                    lbi.Content

            If new_filename = current_filename Then
                ' Play / Pause
                If wave_out.PlaybackState = PlaybackState.Paused Then
                    imgPlayPause.Source = New BitmapImage(resourceUri("Images/pause_Inkubators.png"))
                    wave_out.Play()
                Else
                    imgPlayPause.Source = New BitmapImage(resourceUri("Images/play-button-arrowhead_Freepik.png"))
                    wave_out.Pause()
                End If
            Else
                imgPlayPause.Source = New BitmapImage(resourceUri("Images/pause_Inkubators.png"))
                PlaySelectedItem()
            End If

            audio_timer.Start()
        Catch ex As Exception
            MessageBox.Show(
                $"Couldn't play the selected item: {lbi.Content}." + vbCrLf + ex.StackTrace.ToString + vbCrLf + "Reason: " + ex.Message,
                "Failure",
                MessageBoxButton.OK,
                MessageBoxImage.Error)

            RefreshList()
        End Try
    End Sub

    Dim mp3_reader As Mp3FileReader
    Dim wave_out As WaveOut


    Sub DisposeMp3Reader()
        If mp3_reader Is Nothing Then Exit Sub
        mp3_reader.Dispose()
        wave_out.Dispose()
        mp3_reader = Nothing
        wave_out = Nothing
    End Sub


    Sub PlaySelectedItem()
        ' Play new file

        Dim lbi As ListBoxItem = lsbAudioFiles.SelectedItem

        If lbi Is Nothing Then Exit Sub

        Dim new_filename$ = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                    MainWindow.generated_dir + Path.DirectorySeparatorChar +
                    lbi.Content

        DisposeMp3Reader

        ' https://stackoverflow.com/questions/2488426
        mp3_reader = New Mp3FileReader(new_filename)
        wave_out = New WaveOut
        wave_out.Init(mp3_reader)
        wave_out.Play()

        current_filename = new_filename

        lblCurrentItem.Content = lbi.Content
    End Sub

    Private Sub lsbAudioFiles_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lsbAudioFiles.MouseDoubleClick
        PlayOrPause()
    End Sub

    Private Sub lsbAudioFiles_KeyDown(sender As Object, e As KeyEventArgs) Handles lsbAudioFiles.KeyDown
        If e.Key = Key.Enter Then
            PlayOrPause()
        End If
    End Sub

    Private Sub btnStop_Click(sender As Object, e As RoutedEventArgs) Handles btnStop.Click
        If wave_out.PlaybackState = PlaybackState.Stopped Then Exit Sub

        imgPlayPause.Source = New BitmapImage(resourceUri("Images/play-button-arrowhead_Freepik.png"))
        wave_out.Stop()
        DisposeMp3Reader()
    End Sub

    Private Sub btnPlay_Click(sender As Object, e As RoutedEventArgs) Handles btnPlayPause.Click
        If lsbAudioFiles.Items.IsEmpty Then Exit Sub

        lsbAudioFiles.SelectedIndex = 0

        PlayOrPause()
    End Sub

    ' https://stackoverflow.com/a/1064852
    Private Sub sldPlayer_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs) Handles sldPlayer.PreviewMouseUp
        isDraggingSeek = False

        ' https://stackoverflow.com/questions/10371741/
        mp3_reader.Position = CLng(sldPlayer.Value * wave_out.OutputWaveFormat.AverageBytesPerSecond)
    End Sub

    Dim isDraggingSeek = False

    Private Sub sldPlayer_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs) Handles sldPlayer.PreviewMouseDown
        isDraggingSeek = True
    End Sub
End Class
