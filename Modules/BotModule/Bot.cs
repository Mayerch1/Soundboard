﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataManagement;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;

namespace BotModule
{
    /// <summary>
    /// Basic Bot class, directly communicates with the api, no fail safe, throws for everything
    /// </summary>
    /// <seealso cref="BotHandle"/>
    /// <remarks>
    /// can used as standalone but requires to change all 'protected' keywords to 'public' in order to be accessed, also it is not recommended
    /// if bot is not connected to server or channel, it throws a <see cref="BotException"/>
    /// </remarks>
    public class Bot
    {
        #region config

        private const int channelCount = 2;
        private const int sampleRate = 48000;
        private const int sampleQuality = 60;
        private const int packagesPerSecond = 50;

        //if changed, also change applyVolume();
        private const int bitDepth = 16;

        #endregion config

        #region event Handlers

        /// <summary>
        /// EndOfFileHandler delegate
        /// </summary>
        public delegate void EndOfFileHandler();

        /// <summary>
        /// EndOfFile field
        /// </summary>
        public EndOfFileHandler EndOfFile;


        /// <summary>
        /// StreamStateHandler delegate
        /// </summary>
        /// <param name="newState">new Streaming state</param>
        /// <param name="songName">Name of the current Song</param>
        public delegate void StreamStateHandler(bool newState, string songName);

        /// <summary>
        /// StreamStateChanged filed
        /// </summary>
        public StreamStateHandler StreamStateChanged;

        /// <summary>
        /// EarrapeStateHandler delegate
        /// </summary>
        /// <param name="isEarrape">new isEarrape value</param>
        public delegate void EarrapeStateHandler(bool isEarrape);

        /// <summary>
        /// EarrapeStateChanged field
        /// </summary>
        public EarrapeStateHandler EarrapeStateChanged;

        /// <summary>
        /// LoopStateHandler delegate
        /// </summary>
        /// <param name="isLoop">new isLoop value</param>
        public delegate void LoopStateHandler(bool isLoop);

        /// <summary>
        /// LoopStateChanged field
        /// </summary>
        public LoopStateHandler LoopStateChanged;

        #endregion event Handlers

        #region status fields

        private bool isStreaming = false;
        private bool isChannelConnected = false;
        private string currentSong = "";
        private float pitch = 1.0f;
        private bool isEarrape = false;

        #endregion status fields

        #region status propertys

        /// <summary>
        /// Volume property
        /// </summary>
        /// <remarks>
        /// 1.0 is 100%, 10.0 is static noise
        /// </remarks>
        public float Volume { get; set; }

        /// <summary>
        /// Pitch property
        /// </summary>
        /// <remarks>
        /// 0.0 is default and will not change pitch
        /// </remarks>
        public float Pitch
        {
            get => pitch;
            set
            {
                pitch = value;
                PitchChanged(value);
            }
        }

        /// <summary>
        /// IsServerConnected property
        /// </summary>
        public bool IsServerConnected { get; private set; }

        /// <summary>
        /// IsChannelConnected property
        /// </summary>
        public bool IsChannelConnected
        {
            get
            {
                //check if client is timed out
                if (Client.ConnectionState != ConnectionState.Connected && isChannelConnected)
                {
                    isChannelConnected = false;
                }

                return isChannelConnected;
            }
            private set { isChannelConnected = value; }
        }

        /// <summary>
        /// IsStreaming property, calls StreamStateChanged delegate
        /// </summary>
        public bool IsStreaming
        {
            get { return isStreaming; }
            private set
            {
                if (value != isStreaming)
                {
                    isStreaming = value;
                    StreamStateChanged(isStreaming, currentSong);
                }
            }
        }

        /// <summary>
        /// CurrentTime property
        /// </summary>
        public TimeSpan CurrentTime
        {
            get
            {
                if (Reader != null) return Reader.CurrentTime;
                else return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// TitleLength property
        /// </summary>
        public TimeSpan TitleLenght
        {
            get
            {
                if (Reader != null) return Reader.TotalTime;
                else return TimeSpan.Zero;
            }
        }

       

        /// <summary>
        /// IsBufferEmpty property
        /// </summary>
        public bool IsBufferEmpty { get; private set; }


        /// <summary>
        /// IsLoop property
        /// </summary>
        public bool IsLoop { get; set; } = false;

        /// <summary>
        /// is set if the bot is paused
        /// </summary>
        /// <remarks>unlocks it self, but must be set from outside</remarks>
        public bool IsPause { get; set; } = false;

        /// <summary>
        /// IsToAbort property
        /// </summary>
        private bool IsToAbort { get; set; } = false;

        private uint SkipTracks { get; set; }

        /// <summary>
        /// IsEarrape property
        /// </summary>
        public bool IsEarrape
        {
            get => isEarrape;
            set
            {
                isEarrape = value;
                EarrapeChanged(value);
            }
        }

        private bool CanSeek { get; set; } = true;

        #endregion status propertys

        #region other vars

        private DiscordSocketClient Client { get; set; }
        private IAudioClient AudioCl { get; set; }

        

        private MediaFoundationReader Reader { get; set; }
        private MediaFoundationResampler ActiveResampler { get; set; }       

        private MediaFoundationResampler NormalResampler { get; set; }
        private MediaFoundationResampler SourceResampler { get; set; }

        private MediaFoundationResampler BoostResampler { get; set; }
        private WaveFormat OutFormat { get; set; }

        private AudioOutStream OutStream { get; set; } = null;
        

        #endregion other vars

        /// <summary>
        /// constructor inits important properties
        /// </summary>
        public Bot()
        {
            IsStreaming = false;
            IsChannelConnected = false;
            IsServerConnected = false;
            IsBufferEmpty = true;
        }

        #region controll stuff

        /// <summary>
        /// enqueues a btn into the queue, if queue is empy directly gather stream
        /// </summary>
        /// <param name="data"></param>
        protected async Task loadFileAsync(BotData data)
        {
            if (IsStreaming)
                await stopStreamAsync(true, false);

            getStream(data);
        }

        /// <summary>
        /// start the stream
        /// </summary>
        /// <returns>Task</returns>
        protected async Task resumeStream()
        {
            await startStreamAsync();
        }

        /// <summary>
        /// skips the track, can be cummulated to skip multiple tracks
        /// </summary>
        public void skipTrack()
        {
            if (IsStreaming)
                SkipTracks += 1;
        }


        private void PitchChanged(float val)
        {
            if (SourceResampler != null)
            {
                //delete current resamplers
                NormalResampler = null;

                var pSampler = new SmbPitchShiftingSampleProvider(SourceResampler.ToSampleProvider());
                pSampler.PitchFactor = val;
             
                NormalResampler = new MediaFoundationResampler(pSampler.ToWaveProvider(), OutFormat);             
            }
        }

        private void EarrapeChanged(bool val)
        {
            //will play the boosted version, ignores pitch
            //local stream will not be boosted
            if (val)
                ActiveResampler = BoostResampler;
            else
                ActiveResampler = NormalResampler;
        }

        /// <summary>
        /// skips ahead to a timespan
        /// </summary>
        /// <param name="newTime">new Time</param>
        /// <param name="enforce">enforce the skip, even if nothing is playing</param>
        public void skipToTime(TimeSpan newTime, bool enforce = false)
        {
            if ((IsStreaming || enforce) && CanSeek)
            {
                Reader.CurrentTime = newTime;
            }
        }

        /// <summary>
        /// skip over a time period
        /// </summary>
        /// <param name="skipTime">timeSpan to skip over</param>
        public void skipOverTime(TimeSpan skipTime)
        {
            if (IsStreaming && CanSeek)
            {
                Reader.CurrentTime = Reader.CurrentTime.Add(skipTime);
            }
        }

        /// <summary>
        /// sets the GameState of the bot
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="streamUrl">Url to twitch-stream, only relevant when isStreamin is true</param>
        /// <param name="isStreaming">bool, if bot is streaming on twitch or not</param>
        /// <returns>Task</returns>
        protected async Task setGameState(string msg, string streamUrl = "", bool isStreaming = false)
        {
            if (!IsServerConnected)
                throw new BotException(BotException.type.connection,
                    "Not connected to the Servers, while Setting GameState", BotException.connectionError.NoServer);

            ActivityType type = ActivityType.Watching;
            if (isStreaming)
                type = ActivityType.Streaming;

            if (streamUrl == "")
                await Client.SetGameAsync(msg, streamUrl, type);
            else
                await Client.SetGameAsync(msg);
        }

        #endregion controll stuff

        #region play stuff

        /// <summary>
        /// gets the stream saved in btn.File
        /// </summary>
        /// <param name="data">BotData object</param>
        private void getStream(BotData data)
        {
            //see if file or uri was provided
            if (!String.IsNullOrWhiteSpace(data.uri))
            {
                Reader = new MediaFoundationReader(data.uri);
            }
            else if (!String.IsNullOrEmpty(data.deviceId))
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDevice(data.deviceId);

                var x = new WasapiCapture(device);
                //TODO use WasapiCapture and pipe to bot
            }
            else if (File.Exists(data.filePath))
            {
                Reader = new MediaFoundationReader(data.filePath);
            }
            else
                return;

            CanSeek = Reader.CanSeek;

            OutFormat = new WaveFormat(sampleRate, bitDepth, channelCount);


            //create source and finally used resampler
            SourceResampler = new MediaFoundationResampler(Reader, OutFormat);

            //apply pitch to the resampler, will also set NormalResampler
            PitchChanged(Pitch);

            /*
             * Generate one normal resampler,
             * Generate one boosted resampler,
             * in applyVolume() the matching resampler is assigned to activeResampler
             */

            var volumeSampler = new VolumeWaveProvider16(NormalResampler);
            //this means 10,000%
            volumeSampler.Volume = 100;
            BoostResampler = new MediaFoundationResampler(volumeSampler, OutFormat);

            ActiveResampler = NormalResampler;

            IsBufferEmpty = false;

            //will apply Earrape and loop
            loadOverrideSettings(data);

            //set name of loaded song
            currentSong = data.name;
        }

        /// <summary>
        /// starts the stream
        /// </summary>
        /// <returns>Task</returns>
        /// <remarks>
        /// calls itself again as long as isLoop is true
        /// </remarks>
        private async Task startStreamAsync()
        {
            //IsChannelConnected gaurantees, to have IsServerConnected
            if (!IsChannelConnected)
            {
                if (!IsServerConnected)
                    throw new BotException(BotException.type.connection,
                        "Not connected to Server, while trying to start stream", BotException.connectionError.NoServer);
                else
                    throw new BotException(BotException.type.connection,
                        "Not connected to a channel, while trying to start stream",
                        BotException.connectionError.NoChannel);
            }

            if (!IsStreaming && IsServerConnected && AudioCl != null)
            {
                if (OutStream == null)
                    OutStream = AudioCl.CreatePCMStream(AudioApplication.Music);

                if (ActiveResampler == null)
                {
                    OutStream.Close();
                    OutStream = null;
                    return;
                }

                //send stream in small packages
                int blockSize = OutFormat.AverageBytesPerSecond / packagesPerSecond;
                byte[] buffer = new byte[blockSize];
                int byteCount;

                IsStreaming = true;
                IsPause = false;
            
                //repeat, read new block into buffer -> stream buffer
                while ((byteCount = ActiveResampler.Read(buffer, 0, blockSize)) > 0)
                {
                    applyVolume(ref buffer);

                    if (IsToAbort || SkipTracks > 0)
                        break;

                    if (byteCount < blockSize)
                    {
                        //fill rest of stream with '0'
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                        IsBufferEmpty = true;
                    }

                    await OutStream.WriteAsync(buffer, 0, blockSize);
                }


                IsStreaming = false;

                //reopen the same file
                if (IsLoop && !IsToAbort && SkipTracks == 0)
                {
                    //move head to begin of file
                    skipToTime(TimeSpan.Zero, true);
                    await startStreamAsync();
                }
                //exit stream
                else
                {
                    //can't skip a track if nothing is running
                    SkipTracks = 0;

                    //wait until last packages are played
                    await OutStream.FlushAsync();


                    OutStream.Close();
                    OutStream = null;

                    IsToAbort = false;

                    //trigger end of file delegate, needed e.g. for playlist processing
                    if (!IsPause)
                        EndOfFile();
                }
            }
        }

        /// <summary>
        /// load all specific button settings, raise events to call back to ui for visual indication
        /// </summary>
        /// <param name="data">BotData object</param>
        private void loadOverrideSettings(BotData data)
        {
            //if earrape changes
            if (IsEarrape != data.isEarrape)
            {
                if (data.isEarrape)
                {
                    EarrapeStateChanged(true);
                }
                else
                {
                    EarrapeStateChanged(false);
                }
            }
            //trigger this every time

            //IsLoop will be set from outside

            LoopStateChanged(data.isLoop);
        }

        /// <summary>
        /// split buffer and apply volume to each byte pair
        /// </summary>
        /// <param name="buffer">ref to byte array package of the current stream</param>
        private void applyVolume(ref byte[] buffer)
        {
            if (IsEarrape)
            {
                ActiveResampler = BoostResampler;
            }
            else
            {
                ActiveResampler = NormalResampler;
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    //convert a byte-Pair into one word

                    short bytePair = (short) ((buffer[i + 1] & 0xFF) << 8 | (buffer[i] & 0xFF));

                    //float floatPair = bytePair * Volume;

                    var customVol = Volume;

                    bytePair = (short) (bytePair * customVol);

                    //convert char back to 2 bytes
                    buffer[i] = (byte) bytePair;
                    buffer[i + 1] = (byte) (bytePair >> 8);
                }
            }
        }

        #endregion play stuff

        #region start stuff

        /// <summary>
        /// connect to server
        /// </summary>
        /// <param name="token">bot token used to login</param>
        /// <returns>Task</returns>
        protected async Task connectToServerAsync(string token)
        {
            if (IsServerConnected)
                await disconnectFromServerAsync();

            Client = new DiscordSocketClient();

            await Client.LoginAsync(TokenType.Bot, token);


            await Client.StartAsync();

            IsServerConnected = true;
        }

        /// <summary>
        /// connect to specific channel
        /// </summary>
        /// <param name="channelId">id of channel to join</param>
        /// <returns>Task</returns>
        protected async Task connectToChannelAsync(ulong channelId)
        {
            if (IsChannelConnected)
                await disconnectFromChannelAsync();

            if (!IsServerConnected)
            {
                throw new BotException(BotException.type.connection,
                    "Not connected to the servers, while trying to connect to a channel",
                    BotException.connectionError.NoServer);
            }

            AudioCl = await ((ISocketAudioChannel) Client.GetChannel(channelId)).ConnectAsync(true);

            IsChannelConnected = true;
        }

        #endregion start stuff

        #region stop stuff

        /// <summary>
        /// stop running streams and disconnect from channel
        /// </summary>
        /// <returns>Task</returns>
        /// <see cref="stopStreamAsync(bool, bool)"/>
        public async Task disconnectFromChannelAsync()
        {
            if (!IsChannelConnected)
                return;

            await stopStreamAsync(false, true);

            await AudioCl.StopAsync();

            IsChannelConnected = false;
        }

        /// <summary>
        /// disconnect from channel and close connection to sevrer
        /// </summary>
        /// <returns>Task</returns>
        /// <see cref="disconnectFromChannelAsync()"/>
        public async Task disconnectFromServerAsync()
        {
            if (!IsServerConnected)
                return;

            await disconnectFromChannelAsync();

            //wait until last packet is played
            while (IsChannelConnected)
                await Task.Delay(5);

            await Client.StopAsync();
            await Client.LogoutAsync();

            IsServerConnected = false;
        }

        /// <summary>
        /// stop a running stream
        /// </summary>
        /// <param name="flushStream">flushes the current stream</param>
        /// <param name="closeStream">closes the current stream</param>
        /// <returns>Task</returns>
        public async Task stopStreamAsync(bool flushStream, bool closeStream)
        {
            if (!IsStreaming)
                return;

            IsToAbort = true;

            //wait until last package is read in
            while (IsStreaming)
                await Task.Delay(5);

            if (OutStream != null)
            {
                if (flushStream)
                    await OutStream.FlushAsync();

                if (closeStream)
                {
                    OutStream.Close();
                    OutStream = null;
                }
            }


            //make sure to not block future streams
            IsToAbort = false;
        }

        #endregion stop stuff

        #region get data

        /// <summary>
        /// get a list of all channels of all servers
        /// </summary>
        /// <returns>list of all servers, each contains a list of all channels</returns>
        protected List<List<SocketVoiceChannel>> getAllChannels()
        {
            if (!IsServerConnected)
                throw new BotException(BotException.type.connection,
                    "Not connected to the servers, while trying to get channel list",
                    BotException.connectionError.NoServer);

            List<List<SocketVoiceChannel>> guildList = new List<List<SocketVoiceChannel>>();

            //get all Servers the bot is connectet to
            var guilds = Client.Guilds;

            foreach (var gElement in guilds)
            {
                //get all channels of a server, add them to list

                List<SocketVoiceChannel> subList = new List<SocketVoiceChannel>();

                var vChannels = gElement.VoiceChannels;
                foreach (var vElement in vChannels)
                {
                    subList.Add(vElement);
                }

                guildList.Add(subList);
            }

            return guildList;
        }

        /// <summary>
        /// get a list of all clients of all servers
        /// </summary>
        /// <param name="acceptOffline">incude users which are offline</param>
        /// <returns>list of all servers, each contains a list of all clients, regarding acceptOffline</returns>
        //returns a List<List>, all online clients of all servers are contained
        protected List<List<SocketGuildUser>> getAllClients(bool acceptOffline)
        {
            if (!IsServerConnected)
                throw new BotException(BotException.type.connection,
                    "Not connected to the servers, while trying to get clint list",
                    BotException.connectionError.NoServer);

            List<List<SocketGuildUser>> guildList = new List<List<SocketGuildUser>>();

            var guilds = Client.Guilds;
            foreach (var gElement in guilds)
            {
                List<SocketGuildUser> subList = new List<SocketGuildUser>();
                var users = gElement.Users;

                foreach (var singleUser in users)
                {
                    if (singleUser.VoiceChannel != null || acceptOffline)
                        subList.Add(singleUser);
                }

                guildList.Add(subList);
            }

            return guildList;
        }

        #endregion get data
    }
}