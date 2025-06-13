# The SipLib.Media Namespace
The SipLib.Media namespace contains classes for working with audio media. This namespace includes classes for encoding and decoding audio (codec classes) and classes for sending and receiving audio data in RTP packets over RtpChannels.

The [SipLib.Video](~/articles/SipLibVideo.md) namespace contains classes for working with video media.

The [SipLib.RealTimeText](~/articles/RealTimeText.md) namespace provides classes for working with Real Time Text (RTT) media.

The [SipLib.Msrp](~/articles/MessageSessionRelayProtocol.md) namespace provides classes for working with Message Session Rely Protocol (MSRP) text media.

# Audio Codecs

The SipLib.Media namespace provides the following classes for encoding and decoding audio sample data.

| Codec Type | Encoder Class | Decoder Class | Clock Rate (Sample Rate in samples/second) | Bit Rate (kbits/second) |
|------------|---------------|---------------|
| G.711 PCMU | [PcmuEncoder](~/api/SipLib.Media.PcmuEncoder.yml)   | [PcmuDecoder](~/api/SipLib.Media.PcmuDecoder.yml)   | 8000 | 64 |
| G.711 PCMA | [PcmaEncoder](~/api/SipLib.Media.PcmaEncoder.yml)   | [PcmaDecoder](~/api/SipLib.Media.PcmaDecoder.yml)   | 8000 | 64 |
| G.722      | [G722Encoder](~/api/SipLib.Media.G722Encoder.yml)   | [G722Decoder](~/api/SipLib.Media.G722Decoder.yml)   | 8000/16000 (see Note 1) | 64 |
| G.729      | [G729Encoder](~/api/SipLib.Media.G729Encoder.yml)   | [G729Decoder](~/api/SipLib.Media.G729Decoder.yml)   | 8000 | 8 |
| AMR-WB     | [AmrWbEncoder](~/api/SipLib.Media.AmrWbEncoder.yml) | [AmrWbDecoder](~/api/SipLib.Media.AmrWbDecoder.yml) | 16000 | 6.60, 8.85, 12.65, 14.25, 15.85, 18.25, 19.85, 23.05, 23.85 (see Note 2)  |

Notes:
1. The sample rate of the G.722 codec is 16000 but the clock rate used in the SDP media description is 8000.
2. The AmrWbDecoder automatcially adapts to the bit rate of the data it receives. The AmrWbEncoder operates at a fixed bit rate of 12.65 kbits/second (mode 2). Mode 2 is generally considered to deliver toll quality audio.

# <a name="AudioSourceClass">The AudioSource Class</a>
The [AudioSource](~/api/SipLib.Media.AudioSource.yml) class performs the following tasks.
1. Receives blocks of linear 16-bit Pulse Code Modulated (PCM) audio samples.
2. Encodes each block of linear audio samples.
3. Builds RTP packets containing the encoded sample data and sends them using an RtpChannel channel object.
4. Receives DTMF digits from the application.
5. Builds RTP packets containing DTMF events (per RFC 4733) and sends them using an RtpChannel object.

An application can create an instance of this class once a SIP call dialog has been established as follows.

```
IAudioEncoder? encoder = AudioMediaUtils.GetAudioEncoder(AudioAnswerMd);
AudioSource audioSource = new AudioSource(AudioAnswerMd, encoder, rtpChannel);
```
In the above code sample, the AudioAnswerMd parameter is the instance of a [MediaDescription](~/api/SipLib.Sdp.MediaDescription.yml) object that was used to answer the offer of audio media. If the application is a client then this is the MediaDescription that the client received from the server. If the application is the server, then this is the MediaDescription that it sent to the client.

The rtpChannel parameter is the instance of an [RtpChannel](~/api/SipLib.Rtp.RtpChannel.yml) object that was created to handle audio for the call.

Applications should store the instance of the AudioSource class with the instance of the call object.

The next step is to provide the AudioSource object with a audio sample source by calling the SetAudioSampleSource() method.

```
public void SetAudioSampleSource(IAudioSampleSource audioSampleSource);
```

The source of audio samples that are sent to the remote endpoint can change during the call. For instance, the source can be set to the microphone of a PC when the call is initially answered but then changed to a music on hold (MOH) source when the call is put on-hold. It can then be changed back to the microphone when call hold is terminated. This can be done by calling the SetAudioSampleSource() method with different classes that implement the [IAudioSampleSource](~/api/SipLib.Media.IAudioSampleSource.yml) interface.

When the audio session ends (i.e., the call ends), the application must perform the following steps.
1. Call the ClearAudioSampleSource() method of the AudioSource class
2. Call the Stop method of the IAudioSampleSource interface

To use a class that implements the [IAudioSampleSource](~/api/SipLib.Media.IAudioSampleSource.yml) interface:
1. Create an instance of the class.
2. Pass the instance of the class to the AudioSource object by calling the AudioSource.SetAudioSampleSource() method.
3. Call the Start() method of the IAudioSampleSource interface.

Classes that implement the IAudioSampleSource interface must provide audio samples with the following characteristics.
- Sample Format: 16-bit linear
- Sample Rate: 8000 or 16000 samples/second
- Number of Channels: 1

The AudioSource class will automatically convert the sample rate of the samples provided by the IAudioSampleSource interface to the sample rate required by the encoer object used to encode samples send via the RtpChannel.

When the IAudioSampleSource object is no longer required, the application must call its Stop() method.

The IAudioSampleSource interface has an event called AudioSamplesReady. The declaration of this event is:
```
public delegate void AudioSamplesReadyDelegate(short[] NewSamples, int SampleRate);
```
The AudioSource class hooks this event when the application calls its SetAudioSampleSource() method. Classes that implement the IAudioSampleSource interface fire this event every 20 milliseconds to provide the AudioSource class with new samples to send in RTP packets.

## Microphone Audio Source
In most applications there is a need to acquire audio from a microphone and send it to the remote party of a call. However the SipLib class library does not provide a built-in implementation of the IAudioSampleSource interface that acquires audio from a microphone because such an implementation is very operating system specific.

## Built-In Audio Sample Sources

### <a name="FileAudioSource">The FileAudioSource Class</a>
The [FileAudioSource](~/api/SipLib.Media.FileAudioSource.yml) class plays audio sample data that was read from a recording file. This class is intended for uses in applications that need to play a pre-recorded message to a caller. It can also be used for playing music-on-hold recordings. This is a simple audio source that simply loops around to the beginning of a sample array when it reaches the end of its sample array.

Perform the following steps to use the FileAudioSource class.
1. Call the constructor
2. Pass the new instance of the FileAudioSource object to the AudioSource class via the SetAudioSampleSource() method.
3. Call the Start() method of the FileAudioSource object.

Call the Stop() method of the FileAudioSource object when the audio session ends, or when the FileAudioSource object is no longer needed.

The declaration of the constructor of the FileAudioSource class is:
```
public FileAudioSource(AudioSampleData audioSampleData, HighResolutionTimer? highResolutionTimer);
```

The [HighResolutionTimer](~/api/SipLib.Media.HighResolutionTimer.yml) parameter is optional. If a high resolution timer is not provided then the FileAudioSource class creates and uses a System.Threading.Timer object for each instance of the FileAudioSource class.

In most applications a System.Threading.Timer class is adequate. Within a 16 RTP packet window (320 milliseconds) the average jitter will be a few milliseconds. However the maximum jitter may be as high as 10-15 milliseconds.

The HighResolutionTimer uses a single dedicated, high priority thread to generate periodic events. This timer may be shared with multiple instances of the FileAudioSource class. The maximum jitter of the HighResolutionTimer class is typically less than one millisecond and the average jitter can be as low as 0.1 milliseconds. The disadvantage of this class is that it consumes a lot of CPU time.

The [AudioSampleData](~/api/SipLib.Media.AudioSampleData.yml) class provides a fixed length array of audio sample values and a property called SampleRate that specifies the sample rate in samples per second. The method of reading audio samples from a file is operating system specific. The SipLib class library does not provide in classes to do this.

A single instance of the AudioSampleData class can be used by multiple instances of the FileAudioSouce class.

The following code sample shows how to create AudioSampleData object from a WAVE file in Windows using the [NAudio](https://github.com/naudio/NAudio) class library.
```
using NAudio.Wave;

public static AudioSampleData ReadWaveFile(string FilePath)
{
    short[] Samples;
    if (File.Exists(FilePath) == false)
    {
        throw new FileNotFoundException($"Wave file: '{FilePath}' not found");
    }

    WaveFileReader Wfr = new WaveFileReader(FilePath);
    WaveFormat waveFormat = Wfr.WaveFormat;
    if (waveFormat == null || waveFormat.Channels != 1 || (waveFormat.SampleRate != 8000 &&
        waveFormat.SampleRate != 16000) || waveFormat.BitsPerSample != 16)
    {
        throw new ArgumentException($"Invalid wave file format for file: '{FilePath}'");
    }

    byte[] buffer = new byte[Wfr.Length];
    Wfr.ReadExactly(buffer);
    Samples = new short[Wfr.SampleCount];

    MemoryStream memoryStream = new MemoryStream(buffer);
    BinaryReader binaryReader = new BinaryReader(memoryStream);
    for (long i = 0; i < Wfr.Length / 2; i++)
        Samples[i] = binaryReader.ReadInt16();

    binaryReader.Dispose();
    memoryStream.Dispose();
    return new AudioSampleData(Samples, waveFormat.SampleRate);
}
```

### The SilenceAudioSampleSource Class
The [SilenceAudioSampleSource](~/api/SipLib.Media.SilenceAudioSampleSource.yml) class generates silent audio samples that can be sent to a caller. This class can be used in applications where it is necessary to mute the audio that is sent without pausing transmission of RTP packets sent via the RtpChannel.

Perform the following steps to use the SilenceAudioSampleSource class.
1. Call the constructor
2. Pass the new instance to the AudioSource class via the SetAudioSampleSource() method.
3. Call the Start() method of the SilenceAudioSampleSource object.

Call the Stop() method of the SilenceAudioSampleSource object when the audio session ends, or the FileAudioSource object is no longer needed.

A single instance of the SilenceAudioSampleSource class may be used by multiple calls that use the AudioSource class by following the following procedure.
1. Call the constructor of the SilenceAudioSampleSource class.
2. Store the reference to new SilenceAudioSampleSource instance outside the context of a single call.
2. Call the Start() method of the new SilenceAudioSampleSource instance.
3. Pass the SilenceAudioSampleSource instance to one or more instances of the AudioSource class.

Call the Stop() method of the SilenceAudioSampleSource class when the application terminates or when there is no longer a need to send silence to callers.

## Sending DTMF Digit Events
An application can send DTMF digits over the RtpChannel (see RFC 4733) by calling the SendDtmfEvent() method of the AudioSource class.

```
public void SendDtmfEvent(DtmfEventEnum dtmfEvent);
```

The [DtmfEventEnum](~/api/SipLib.Rtp.DtmfEventEnum.yml) enumeration defines values for 0-9, #, * and A-F. The AudioSource class will only send DTMF event RTP packets if it is currently running.

DTMF events are queued and sent out at a packet rate equal to the packet rate of audio data. If the application needs to send multiple digits then it can call the SendDemfEvent() method multiple times. However, the AudioSource class does not insert an inter-digit gap between DTMF events so the calling application must ensure there is at least a 40 millisecond gap between digits. This is normally not an issue because DTMF digits are usally entered by a human being in response to voice prompts.

The AudioSource class sends four DTMF event RTP packets followed by three DTMF end event packets, so the DTMF event duration is 80 milliseconds.

# <a name="AudioDestinationClass">The AudioDestination Class</a>
The [AudioDestination](~/api/SipLib.Media.AudioDestination.yml) class performs the following functions.
1. Receives RTP packets from an RtpChannel.
2. Decodes the data into linear 16-bit PCM samples and sends the decoded samples to the user via a user-provided callback delegate.
3. Performs sample rate correction between the received data and the data required by the user's audio handler.
4. Receives RTP packets containing DTMF events and sends them to the user via an event.
5. Allows the user to change the destination handler for the received audio samples via a method call.

An application can create an instance of this class once a SIP call dialog has been established as follows.
```
IAudioDecoder? decoder = AudioMediaUtils.GetAudioDecoder(AudioAnswerMd);
if (decoder != null)
{
    AudioDestination audioDestination = new AudioDestination(AudioAnswerMd, decoder,
        rtpChannel, OnAudioDataAvailable, DestinationSampleRate);
    audioDestination.DtmfDigitReceived += OnDtmfDigitReceived;
}
else
{
    // TODO: Handle this error
}

private void OnAudioDataAvailable(short[] PcmSamples)
{
    // TODO: handle the received array of audio data here
}

private void OnDtmfDigitReceived(DtmfEventEnum digit)
{
    // TODO: Handle the DTMF digit here.
}
```

In the above code sample, the AudioAnswerMd parameter is the instance of a [MediaDescription](~/api/SipLib.Sdp.MediaDescription.yml) object that was used to answer the offer of audio media. If the application is a client then this is the MediaDescription that the client received from the server. If the application is the server, then this is the MediaDescription that it sent to the client.

The rtpChannel parameter is the instance of an [RtpChannel](~/api/SipLib.Rtp.RtpChannel.yml) object that was created to handle audio for the call.

The OnAudioDataAvailable() function is where the application handles the new audio data by sending it to the system speakers or a headset. If this parameter of the constructor is null, then the AudioDestination class will process RTP packets but it will ignore the audio data.

## Changing the Audio Destination Handler
The application can use the SetDestinationHandler() method to change how it handles the received audio samples.
```
public void SetDestinationHandler(AudioDestinationDelegate? destinationHandler);
```

If the [AudioDestinationDelegate](~/api/SipLib.Media.AudioDestinationDelegate.yml) parameter is null, then received audio samples will be ignored.

This method may be called at any time to change the destination handler.





