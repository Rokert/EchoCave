using System;
using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // Plays the cue that belongs to each game event. Plain C#: the composition root creates it on enable and
    // disposes it on disable. The reveal sound is not here: it comes from the board view, one per bat, as the
    // wave reaches it.
    public sealed class SoundCuePlayer : IDisposable
    {
        private readonly EventBus _events;
        private readonly IAudioService _audio;

        public SoundCuePlayer(EventBus events, IAudioService audio)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));

            _events.Subscribe<PingUsed>(OnPingUsed);
            _events.Subscribe<EchoExpired>(OnEchoExpired);
            _events.Subscribe<PingRefused>(OnPingRefused);
            _events.Subscribe<MoveSpent>(OnMoveSpent);
            _events.Subscribe<LevelWon>(OnLevelWon);
            _events.Subscribe<LevelLost>(OnLevelLost);
        }

        public void Dispose()
        {
            _events.Unsubscribe<PingUsed>(OnPingUsed);
            _events.Unsubscribe<EchoExpired>(OnEchoExpired);
            _events.Unsubscribe<PingRefused>(OnPingRefused);
            _events.Unsubscribe<MoveSpent>(OnMoveSpent);
            _events.Unsubscribe<LevelWon>(OnLevelWon);
            _events.Unsubscribe<LevelLost>(OnLevelLost);
        }

        private void OnPingUsed(PingUsed used) => _audio.Play(SoundCue.Ping);

        private void OnEchoExpired(EchoExpired expired) => _audio.Play(SoundCue.Expire);

        private void OnPingRefused(PingRefused refused) => _audio.Play(SoundCue.Refuse);

        // Published as the animation starts, so the whoosh and the thud begin with the movement.
        private void OnMoveSpent(MoveSpent spent)
        {
            _audio.Play(spent.Outcome == MoveOutcome.Exited ? SoundCue.Fly : SoundCue.Bounce);
        }

        private void OnLevelWon(LevelWon won) => _audio.Play(SoundCue.Win);

        private void OnLevelLost(LevelLost lost) => _audio.Play(SoundCue.Lose);
    }
}
