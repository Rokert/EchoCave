using System;
using System.Collections.Generic;
using Rokert.Core;

namespace EchoCave.Game
{
    // Listens for wins on the EventBus and saves them, and keeps the audio service in step with the switches
    // in the pause popup. Plain C#: the composition root creates it on enable and disposes it on disable.
    public sealed class ProgressRecorder : IDisposable
    {
        private readonly EventBus _events;
        private readonly ISaveService _save;
        private readonly IAudioService _audio;
        private readonly IReadOnlyList<int> _levelIds;

        // levelIds[i] is the stable id of the level at catalog position i: progress is saved by id, the flow speaks in positions.
        public ProgressRecorder(EventBus events, ISaveService save, IAudioService audio, IReadOnlyList<int> levelIds)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _levelIds = levelIds ?? throw new ArgumentNullException(nameof(levelIds));

            _events.Subscribe<LevelWon>(OnLevelWon);
            _events.Subscribe<AudioToggled>(OnAudioToggled);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelWon>(OnLevelWon);
            _events.Unsubscribe<AudioToggled>(OnAudioToggled);
        }

        private void OnLevelWon(LevelWon won)
        {
            _save.RecordWin(_levelIds[won.LevelIndex], won.MovesUsed, won.Stars);
        }

        private void OnAudioToggled(AudioToggled toggled)
        {
            _audio.SetEnabled(toggled.Channel, toggled.Enabled);
        }
    }
}
