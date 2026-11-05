using System;
using System.Collections.Generic;
using Rokert.Core;

namespace EchoCave.Game
{
    // Chooses the music from the flow. The menu has its own track; the first level or lesson that starts switches to
    // the first track of the playlist, and every 'secondsPerTrack' of play the next track takes over, so the ear does
    // not tire. A new level does not restart the track, and the playlist keeps its place until the flow goes back to
    // the menu. Plain C#: the composition root creates it on enable, ticks it every frame and disposes it on disable.
    public sealed class MusicConductor : IDisposable
    {
        private readonly EventBus _events;
        private readonly IAudioService _audio;
        private readonly MusicTrack _menuTrack;
        private readonly IReadOnlyList<MusicTrack> _playTracks;
        private readonly float _secondsPerTrack;

        private MusicArea _area = MusicArea.Menu;
        private int _index;
        private float _elapsed;

        public MusicArea Area => _area;

        // An empty playlist plays the menu track while playing (the music bank reports it as a problem).
        public MusicConductor(EventBus events, IAudioService audio, MusicTrack menuTrack, IReadOnlyList<MusicTrack> playTracks, float secondsPerTrack)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _menuTrack = menuTrack;
            _playTracks = playTracks != null && playTracks.Count > 0 ? playTracks : new[] { menuTrack };
            _secondsPerTrack = secondsPerTrack;

            _events.Subscribe<MenuOpened>(OnMenuOpened);
            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
            _events.Subscribe<ChallengeStarted>(OnChallengeStarted);

            _audio.PlayMusic(_menuTrack);
        }

        public void Dispose()
        {
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
            _events.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
        }

        // Counts play time. Called every frame with the real time that passed (a paused game still plays music).
        public void Tick(float deltaSeconds)
        {
            if (_area != MusicArea.Play || _playTracks.Count < 2 || _secondsPerTrack <= 0f) return;

            _elapsed += deltaSeconds;
            if (_elapsed < _secondsPerTrack) return;

            _elapsed = 0f;
            _index = (_index + 1) % _playTracks.Count;
            _audio.PlayMusic(_playTracks[_index]);
        }

        private void OnMenuOpened(MenuOpened opened)
        {
            if (_area == MusicArea.Menu) return;

            _area = MusicArea.Menu;
            _audio.PlayMusic(_menuTrack);
        }

        private void OnLevelStarted(LevelStarted started) => EnterPlay();

        private void OnLessonStarted(LessonStarted started) => EnterPlay();

        // A challenge is playing too.
        private void OnChallengeStarted(ChallengeStarted started) => EnterPlay();

        private void EnterPlay()
        {
            if (_area == MusicArea.Play) return;

            _area = MusicArea.Play;
            _index = 0;
            _elapsed = 0f;
            _audio.PlayMusic(_playTracks[0]);
        }
    }
}
