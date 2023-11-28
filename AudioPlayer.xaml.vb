Imports System.IO
Imports System.Windows.Threading

' Todo: fix seeking related bugs
' Todo: learn how to play audio properly with clock controller
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
        lblPlayerTime.Content = If(
            media_player.Source IsNot Nothing AndAlso media_player.NaturalDuration.HasTimeSpan,
            media_player.Position.ToString("mm\:ss") + " / " + media_player.NaturalDuration.TimeSpan.ToString("mm\:ss"),
            "0:00 / 0:00")

        sldPlayer.Value = media_player.Position.TotalSeconds
        sldPlayer.Maximum = media_player.NaturalDuration.TimeSpan.TotalSeconds
    End Sub

    Dim audio_timer As New DispatcherTimer With {
        .Interval = TimeSpan.FromSeconds(1)
    }

    Dim media_player As New MediaPlayer With {
        .ScrubbingEnabled = True    ' https://stackoverflow.com/questions/2993733
    }

    Dim current_filename$

    Sub PlayOrPause()
        Dim lbi As ListBoxItem = lsbAudioFiles.SelectedItem

        If lbi Is Nothing Then Exit Sub


        Try
            Dim new_filename = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                    MainWindow.generated_dir + Path.DirectorySeparatorChar +
                    lbi.Content

            If new_filename = current_filename Then
                ' Play / Pause
                If media_player.Clock.CurrentState = Animation.ClockState.Stopped Then
                    media_player.Clock.Controller.Seek(TimeSpan.FromSeconds(0), Animation.TimeSeekOrigin.BeginTime)
                    media_player.Clock.Controller.Begin()
                Else
                    If media_player.Clock.IsPaused Then
                        ' Resume playback
                        media_player.Clock.Controller.Resume()
                        audio_timer.Start()
                    Else
                        media_player.Clock.Controller.Pause()
                        audio_timer.Stop()
                    End If
                End If
            Else
                PlaySelectedItem()
            End If

            audio_timer.Start()
        Catch ex As Exception
            MessageBox.Show(
                $"Couldn't play the selected item: {lbi.Content}." + vbCrLf + "Reason: " + ex.Message,
                "Failure",
                MessageBoxButton.OK,
                MessageBoxImage.Error)

            RefreshList()
        End Try
    End Sub

    Sub PlaySelectedItem()
        Dim lbi As ListBoxItem = lsbAudioFiles.SelectedItem

        If lbi Is Nothing Then Exit Sub

        ' Play new file
        If media_player.Clock IsNot Nothing Then
            media_player.Clock.Controller.Stop()
        End If

        Dim new_filename = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                    MainWindow.generated_dir + Path.DirectorySeparatorChar +
                    lbi.Content

        media_player.Clock = Nothing
        current_filename = new_filename

        Dim target_uri = New Uri(current_filename)
        Dim tl As New MediaTimeline(target_uri)
        Dim media_clock As MediaClock = tl.CreateClock(True)
        media_player.Clock = media_clock
        media_player.Clock.Controller.Stop()

        lblCurrentItem.Content = lbi.Content
        If media_player.Clock.CurrentState = Animation.ClockState.Stopped Then
            media_player.Clock.Controller.Begin()
        End If
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
        media_player.Clock.Controller.Stop()
        audio_timer.Stop()
    End Sub

    Private Sub btnPlay_Click(sender As Object, e As RoutedEventArgs) Handles btnPlayPause.Click
        PlayOrPause()
    End Sub

    Private Sub sldPlayer_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles sldPlayer.MouseUp
        media_player.Clock.Controller.Seek(TimeSpan.FromSeconds(sldPlayer.Value), Animation.TimeSeekOrigin.BeginTime)
    End Sub
End Class
