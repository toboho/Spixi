﻿using Android.Media;
using IXICore.Meta;
using SPIXI.VoIP;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderAndroid))]

public class AudioRecorderAndroid : IAudioRecorder
{
    private Action<byte[]> OnSoundDataReceived;

    private AudioRecord audioRecorder = null;

    bool stopRecording = false;
    bool running = false;

    int bufferSize = 8192;

    public AudioRecorderAndroid()
    {

    }

    public void start()
    {
        if(running)
        {
            Logging.warn("Audio recorder is already running.");
            return;
        }
        stopRecording = false;
        running = true;

        bufferSize = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, Encoding.Pcm16bit) * 10;

        audioRecorder = new AudioRecord(
            // Hardware source of recording.
            AudioSource.Mic,
            // Frequency
            44100,
            // Mono or stereo
            ChannelIn.Mono,
            // Audio encoding
            Encoding.Pcm16bit,
            // Length of the audio clip.
            bufferSize
        );


        audioRecorder.StartRecording();

        Thread recordingThread = new Thread(readLoop);
        recordingThread.Start();
    }

    void readLoop()
    {
        byte[] buffer = new byte[bufferSize];
        while (!stopRecording)
        {
            try
            {
                int num_bytes = audioRecorder.Read(buffer, 0, buffer.Length);

                byte[] data_to_send = new byte[num_bytes];
                Array.Copy(buffer, data_to_send, num_bytes);

                Task.Run(() =>
                {
                    OnSoundDataReceived(data_to_send);
                });
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while recording audio stream: " + e);
                break;
            }
            Thread.Sleep(10);
        }
        audioRecorder.Stop();
        audioRecorder.Release();
        audioRecorder.Dispose();
        audioRecorder = null;
        running = false;
    }
    public void stop()
    {
        stopRecording = true;
    }

    public void Dispose()
    {
        stop();
    }

    public bool isRunning()
    {
        return running;
    }

    public void setOnSoundDataReceived(Action<byte[]> on_sound_data_received)
    {
        OnSoundDataReceived = on_sound_data_received;
    }
}