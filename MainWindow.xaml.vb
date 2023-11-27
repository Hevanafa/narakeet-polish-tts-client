Imports System.Net
Imports System.Net.Http
Imports System.IO
Imports Newtonsoft.Json
Imports System.Text

Class MainWindow
    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim lbi As New ListBoxItem With {
            .Content = "Loading voice list..."
        }

        lsbVoices.Items.Add(lbi)
        lsbVoices.SelectedIndex = 0

        Await ReadApiKey()

        If String.IsNullOrWhiteSpace(api_key) Then
            MessageBox.Show("Please make the file for API key: ""api_key.txt"", which contains 1 line of your Narakeet API key.")
            lbi.Content = "API key is not ready!"
        Else
            Await RetrieveVoiceList()
        End If
    End Sub

    Dim api_key$

    Async Function ReadApiKey() As Task(Of Boolean)
        If Not File.Exists("api_key.txt") Then _
            Return False

        Using sr As New StreamReader("api_key.txt")
            api_key = Await sr.ReadLineAsync()
        End Using

        Return True
    End Function

    Dim VoiceList As List(Of VoiceListItem)

    Const VoiceListFile$ = "voice_list.json"

    Async Function RetrieveVoiceList() As Task(Of Boolean)
        Try
            Dim list$

            If Not File.Exists(VoiceListFile) Then
                Dim endpoint$ = "https://api.narakeet.com/voices"
                Dim req As HttpWebRequest = WebRequest.Create(endpoint)
                req.Timeout = 30000
                req.Headers.Add("x-api-key", api_key)
                req.AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate

                Using res As HttpWebResponse = Await req.GetResponseAsync
                    Using stream = res.GetResponseStream
                        Using reader As New StreamReader(stream)
                            list = Await reader.ReadToEndAsync
                        End Using
                    End Using
                End Using

                Using sw As New StreamWriter(VoiceListFile)
                    Await sw.WriteAsync(list)
                End Using
            Else
                Using sr As New StreamReader(VoiceListFile)
                    list = Await sr.ReadToEndAsync
                End Using
            End If

            VoiceList = JsonConvert.DeserializeObject(Of List(Of VoiceListItem))(list)

            UpdateVoiceList()

            Return True
        Catch ex As Exception
            Dim lbi As ListBoxItem = lsbVoices.Items(0)
            lbi.Content = ex.Message
            Return False
        End Try
    End Function

    Sub UpdateVoiceList()
        lsbVoices.Items.Clear()

        For Each item In VoiceList.Where(Function(x) x.languageCode = "pl-PL")
            Dim lbi As New ListBoxItem With {
                .Content = Char.ToUpper(item.name.FirstOrDefault) + item.name.Substring(1)
            }

            lsbVoices.Items.Add(lbi)
        Next

        lsbVoices.SelectedIndex = 0
    End Sub


    Function GetValidNextFilename$()
        Dim dir_info = FileIO.FileSystem.GetDirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)

        Dim filenames$() =
            dir_info.EnumerateFiles.Where(
                Function(file)
                    Return file.Name.StartsWith("audio_") AndAlso Path.GetExtension(file.Name) = ".mp3"
                End Function).Select(Function(file) file.Name).ToArray

        Dim highest$ = filenames.OrderBy(
            Function(filename$)
                Return Val(Path.GetFileNameWithoutExtension(filename).Split("_")(1))
            End Function).FirstOrDefault()

        If String.IsNullOrWhiteSpace(highest) Then
            Return "audio_1.mp3"
        End If

        Dim new_idx% = Val(highest.Split("_")(1))
        new_idx += 1

        Return $"audio_{new_idx}.mp3"
    End Function


    Async Function AttemptSubmitScript() As Task(Of Boolean)
        ' try to download the audio in mp3 format
        Dim item As ListBoxItem = lsbVoices.SelectedItem
        If item Is Nothing Then Return False

        Dim voice$ = item.Content
        Dim script$ = txbScript.Text
        Dim result_file$ = GetValidNextFilename()

        If String.IsNullOrWhiteSpace(script) Then
            MessageBox.Show("Please provide the script.")
            Return False
        End If

        Try
            Using client As New HttpClient With {.Timeout = New TimeSpan(30000000000)}

                Dim req As New HttpRequestMessage(HttpMethod.Post, $"https://api.narakeet.com/text-to-speech/mp3?voice={voice}")
                With client.DefaultRequestHeaders
                    .Accept.Clear()
                    .Add("accept", "application/octet-stream")
                    .Add("x-api-key", api_key)
                End With

                req.Content = New StringContent(script$, Encoding.UTF8, "text/plain")

                Using res As HttpResponseMessage = Await client.SendAsync(req)
                    res.EnsureSuccessStatusCode()

                    Using dest As FileStream = File.Create(result_file)
                        Await res.Content.CopyToAsync(dest)
                    End Using
                End Using
            End Using

            MessageBox.Show("Saved as " + result_file)
        Catch ex As Exception
            MessageBox.Show("Failed to generate the TTS." + vbCrLf + "Reason: " + ex.Message)

            Using sw As New StreamWriter("failure.txt")
                sw.WriteLine(ex.Message)
            End Using
        End Try

        Return True
    End Function

    Private Async Sub btnSubmit_Click(sender As Object, e As RoutedEventArgs) Handles btnSubmit.Click
        btnSubmit.IsEnabled = False
        btnSubmit.Content = "Waiting..."

        Await AttemptSubmitScript()

        'If Not Await AttemptSubmitScript() Then
        '    Using sr As New StreamReader("failure.txt")
        '        Dim reason$ = Await sr.ReadLineAsync
        '        MessageBox.Show()
        '    End Using
        'End If

        btnSubmit.IsEnabled = True
        btnSubmit.Content = "Submit"
    End Sub

    Private Sub Canvas_MouseDown(sender As Object, e As MouseButtonEventArgs)
        Process.Start("https://www.narakeet.com/")
    End Sub
End Class
