﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using MediaManager.Audio;
using MediaManager.Media;
using MediaManager.Platforms.Apple;
using MediaManager.Platforms.Apple.Media;
using MediaManager.Playback;
using MediaManager.Video;
using MediaManager.Volume;

namespace MediaManager
{
    public abstract class AppleMediaManagerBase<TMediaPlayer> : MediaManagerBase<TMediaPlayer, AVQueuePlayer> where TMediaPlayer : AppleMediaPlayer, IMediaPlayer<AVQueuePlayer>, new()
    {
        private IMediaPlayer _mediaPlayer;
        public override AppleMediaPlayer MediaPlayer
        {
            get
            {
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new TMediaPlayer();
                }
                return _mediaPlayer;
            }
            set
            {
                _mediaPlayer = value;
            }
        }

        private IMediaExtractor _mediaExtractor;
        public override IMediaExtractor MediaExtractor
        {
            get
            {
                if (_mediaExtractor == null)
                {
                    _mediaExtractor = new AppleMediaExtractor();
                }
                return _mediaExtractor;
            }
            set
            {
                _mediaExtractor = value;
            }
        }

        private IVolumeManager _volumeManager;
        public override IVolumeManager VolumeManager
        {
            get
            {
                if (_volumeManager == null)
                    _volumeManager = new VolumeManager(NativeMediaPlayer.Player);
                return _volumeManager;
            }
            set
            {
                _volumeManager = value;
            }
        }

        public override MediaPlayerState State
        {
            get
            {
                return MediaPlayer.State;
            }
        }

        public override TimeSpan Position
        {
            get
            {
                if (NativeMediaPlayer.Player.CurrentItem == null)
                {
                    return TimeSpan.Zero;
                }
                return TimeSpan.FromSeconds(NativeMediaPlayer.Player.CurrentItem.CurrentTime.Seconds);
            }
        }

        public override TimeSpan Duration
        {
            get
            {
                if (NativeMediaPlayer.Player.CurrentItem == null)
                {
                    return TimeSpan.Zero;
                }
                if (double.IsNaN(NativeMediaPlayer.Player.CurrentItem.Duration.Seconds))
                {
                    return TimeSpan.Zero;
                }
                return TimeSpan.FromSeconds(NativeMediaPlayer.Player.CurrentItem.Duration.Seconds);
            }
        }

        public override TimeSpan Buffered
        {
            get
            {
                var buffered = TimeSpan.Zero;
                if (NativeMediaPlayer.Player.CurrentItem != null)
                {
                    buffered =
                        TimeSpan.FromSeconds(
                            NativeMediaPlayer.Player.CurrentItem.LoadedTimeRanges.Select(
                                tr => tr.CMTimeRangeValue.Start.Seconds + tr.CMTimeRangeValue.Duration.Seconds).Max());
                }

                return buffered;
            }
        }

        public override float Speed
        {
            get
            {
                if (NativeMediaPlayer.Player != null)
                    return NativeMediaPlayer.Player.Rate;
                return 0.0f;
            }
            set
            {
                if (NativeMediaPlayer.Player != null)
                    NativeMediaPlayer.Player.Rate = value;
            }
        }

        private bool _repeat { get; set; } = false;

        public override void Init()
        {
            MediaPlayer.Initialize();
            IsInitialized = true;
            _repeat = false;
        }

        public override Task Pause()
        {
            this.MediaPlayer.Pause();
            return Task.CompletedTask;
        }

        public override Task Play(IMediaItem mediaItem)
        {
            IsInitialized = true;
            this.MediaPlayer.Play(mediaItem);
            return Task.CompletedTask;
        }

        public override async Task<IMediaItem> Play(string uri)
        {
            MediaQueue.Clear();
            var mediaItem = await MediaExtractor.CreateMediaItem(uri);

            this.MediaQueue.Add(mediaItem);
            await this.MediaPlayer.Play();
            return mediaItem;
        }

        public override async Task Play(IEnumerable<IMediaItem> items)
        {
            MediaQueue.Clear();
            foreach (var item in items)
            {
                MediaQueue.Add(item);
            }
            await this.MediaPlayer.Play();
        }

        public override async Task<IEnumerable<IMediaItem>> Play(IEnumerable<string> items)
        {
            MediaQueue.Clear();

            foreach (var uri in items)
            {
                var mediaItem = await MediaExtractor.CreateMediaItem(uri);
                MediaQueue.Add(mediaItem);
            }

            await this.MediaPlayer.Play();
            return MediaQueue;
        }

        public override async Task<IMediaItem> Play(FileInfo file)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IMediaItem>> Play(DirectoryInfo directoryInfo)
        {
            throw new NotImplementedException();
        }

        public override Task Play()
        {
            this.MediaPlayer.Play();
            return Task.CompletedTask;
        }

        public override Task PlayNext()
        {
            NativeMediaPlayer.Player.AdvanceToNextItem();
            return Task.CompletedTask;
        }

        public override Task PlayPrevious()
        {
            // TODO: For the previous we have to do some manual labour!
            throw new NotImplementedException();
        }

        public override Task SeekTo(TimeSpan position)
        {
            return this.MediaPlayer.Seek(position);
        }

        public override Task StepBackward()
        {
            throw new NotImplementedException();
        }

        public override Task StepForward()
        {
            throw new NotImplementedException();
        }

        public override Task Stop()
        {
            return this.MediaPlayer.Stop();
        }

        public override void ToggleRepeat()
        {
            this._repeat = !this._repeat;
            this.MediaPlayer.Repeat = this._repeat;
        }

        public override void ToggleShuffle()
        {
            throw new NotImplementedException();
        }
    }
}
