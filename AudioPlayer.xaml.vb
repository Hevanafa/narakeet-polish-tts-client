Imports System.IO
Imports System.Windows.Threading

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
            media_player.Source IsNot Nothing,
            media_player.Position.ToString("mm\:ss") + " / " + media_player.NaturalDuration.TimeSpan.ToString("mm\:ss"),
            "0:00 / 0:00")
    End Sub

    Dim audio_timer As New DispatcherTimer With {
        .Interval = TimeSpan.FromSeconds(1)
    }

    Dim media_player As New MediaPlayer

    Sub PlaySelectedItem()
        Dim lbi As ListBoxItem = lsbAudioFiles.SelectedItem

        If lbi Is Nothing Then Exit Sub


        Try
            media_player.Open(
                New Uri(
                    AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar +
                    MainWindow.generated_dir + Path.DirectorySeparatorChar +
                    lbi.Content))

            media_player.Play()
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

    Private Sub lsbAudioFiles_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lsbAudioFiles.MouseDoubleClick
        PlaySelectedItem()
    End Sub

    Private Sub lsbAudioFiles_KeyDown(sender As Object, e As KeyEventArgs) Handles lsbAudioFiles.KeyDown
        If e.Key = Key.Enter Then
            PlaySelectedItem()
        End If
    End Sub

    Private Sub btnPause_Click(sender As Object, e As RoutedEventArgs) Handles btnPause.Click
        media_player.Pause()
        audio_timer.Stop()
    End Sub

    Private Sub btnStop_Click(sender As Object, e As RoutedEventArgs) Handles btnStop.Click
        media_player.Stop()
        audio_timer.Stop()
    End Sub

    Private Sub btnPlay_Click(sender As Object, e As RoutedEventArgs) Handles btnPlay.Click
        PlaySelectedItem()
    End Sub
End Class
