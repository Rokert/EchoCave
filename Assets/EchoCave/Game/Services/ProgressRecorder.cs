using System;
using System.Collections.Generic;
using Rokert.Core;

namespace EchoCave.Game
{
    // Listens for wins on the EventBus and saves them, and keeps the audio service and the save in step with the
    // switches in the pause popup. Plain C#: the composition root creates it on enable and disposes it on disable.
    public sealed class ProgressRecorder : IDisposable
    {
        private readonly EventBus _events;
        private readonly ISaveService _save;
        private readonly IAudioService _audio;
        private readonly IReadOnlyList<int> _levelIds;
        private readonly HintRewardService _rewards;

        // rewards is optional: without it a win is only saved.
        // levelIds[i] is the stable id of the level at catalog position i: progress is saved by id, the flow speaks in positions.
        public ProgressRecorder(EventBus events, ISaveService save, IAudioService audio, IReadOnlyList<int> levelIds, HintRewardService rewards = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _levelIds = levelIds ?? throw new ArgumentNullException(nameof(levelIds));
            _rewards = rewards;

            _events.Subscribe<LevelWon>(OnLevelWon);
            _events.Subscribe<AudioToggled>(OnAudioToggled);
            _events.Subscribe<TutorialEnded>(OnTutorialEnded);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelWon>(OnLevelWon);
            _events.Unsubscribe<AudioToggled>(OnAudioToggled);
            _events.Unsubscribe<TutorialEnded>(OnTutorialEnded);
        }

        private void OnLevelWon(LevelWon won)
        {
            int levelId = _levelIds[won.LevelIndex];

            // The reward needs the stars the level had BEFORE this win, so it must see them before they are saved.
            int previousStars = _save.Progress.StarsOf(levelId);
            _save.RecordWin(levelId, won.MovesUsed, won.Stars);
            _rewards?.OnLevelWon(won.Stars, previousStars);
        }

        // Finishing or skipping the tutorial is remembered, so it does not start by itself again. Leaving it half way is not.
        private void OnTutorialEnded(TutorialEnded ended)
        {
            if (ended.Seen) _save.SetTutorialSeen(true);
        }

        private void OnAudioToggled(AudioToggled toggled)
        {
            _audio.SetEnabled(toggled.Channel, toggled.Enabled);
            _save.SetAudio(toggled.Channel, toggled.Enabled);
        }
    }
}
