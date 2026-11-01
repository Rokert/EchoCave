using System;
using System.Collections.Generic;
using DG.Tweening;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Draws the board and plays move animations. It decides nothing: the presenter calls Play with the
    // result the core already produced. Positions are local to this transform, so the board can be moved
    // or scaled in the scene.
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        [SerializeField] private BatView batPrefab;
        [SerializeField] private Transform batContainer;
        [SerializeField] private BoardViewSettings settings;
        [Tooltip("Drawn under every cell. Leave empty to draw no tiles.")]
        [SerializeField] private Sprite cellSprite;
        [Tooltip("The ring of the ping wave. Leave empty to reveal hidden bats at once, with no wave.")]
        [SerializeField] private Sprite pingRingSprite;

        private readonly Dictionary<int, BatView> _views = new Dictionary<int, BatView>();
        private readonly List<SpriteRenderer> _tiles = new List<SpriteRenderer>();
        private readonly List<PingWave> _activeWaves = new List<PingWave>();
        private BatFactory _factory;
        private Transform _tileContainer;
        private ObjectPool<PingWave> _wavePool;

        // Raised once per hidden bat, when the ping wave reaches it and its reveal begins. The sound listens.
        public event Action BatRevealed;

        public GridLayout2D Layout { get; private set; }

        // Replaces whatever is on screen. Animations still running are cut short and their callbacks never fire.
        public void Build(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            Clear();
            Layout = new GridLayout2D(board.Width, board.Height, settings.CellSize);
            _factory ??= new BatFactory(batPrefab, batContainer, board.Count);

            BuildTiles();
            foreach (Bat bat in board.Bats) Spawn(bat);
        }

        public bool TryGetCell(Vector2 world, out Cell cell)
        {
            if (Layout == null)
            {
                cell = default;
                return false;
            }

            return Layout.TryWorldToCell(transform.InverseTransformPoint(world), out cell);
        }

        public void Spawn(Bat bat)
        {
            if (_views.ContainsKey(bat.Id)) throw new InvalidOperationException($"Bat {bat.Id} is already on the board view.");

            BatView view = _factory.Get(bat);
            view.transform.localPosition = Layout.CellToWorld(bat.Cell);
            _views.Add(bat.Id, view);
        }

        public void Clear()
        {
            _factory?.ReleaseAll();
            _views.Clear();

            foreach (PingWave wave in new List<PingWave>(_activeWaves)) ReleaseWave(wave);

            foreach (SpriteRenderer tile in _tiles) tile.gameObject.SetActive(false);
        }

        // One tile per cell, reused between levels: Build only moves, scales and shows the ones it needs.
        private void BuildTiles()
        {
            if (cellSprite == null) return;

            if (_tileContainer == null)
            {
                _tileContainer = new GameObject("Tiles").transform;
                _tileContainer.SetParent(transform, false);
            }

            int count = Layout.Width * Layout.Height;
            while (_tiles.Count < count) _tiles.Add(CreateTile());

            float scale = Layout.CellSize * settings.TileScale / SpriteMetrics.Size(cellSprite).x;
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer tile = _tiles[i];
                tile.transform.localPosition = Layout.CellToWorld(new Cell(i % Layout.Width, i / Layout.Width));
                tile.transform.localScale = Vector3.one * scale;
                tile.gameObject.SetActive(true);
            }
        }

        private SpriteRenderer CreateTile()
        {
            var tile = new GameObject("Tile");
            tile.transform.SetParent(_tileContainer, false);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = cellSprite;
            renderer.sortingOrder = settings.TileSortingOrder;
            return renderer;
        }

        // No wave: used when the echo runs out and when a loss shows what was hidden.
        public void SetRevealed(IReadOnlyCollection<int> batIds)
        {
            if (batIds == null) throw new ArgumentNullException(nameof(batIds));

            var revealed = new HashSet<int>(batIds);
            foreach (KeyValuePair<int, BatView> pair in _views)
            {
                if (revealed.Contains(pair.Key)) pair.Value.Reveal(0f, settings.RevealFadeSeconds, settings.RevealPopScale);
                else pair.Value.Conceal(settings.ConcealFadeSeconds);
            }
        }

        // The ring leaves the centre of the board and each hidden bat reveals when it reaches it (D-028). Without a
        // ring sprite the bats reveal at once.
        public void ShowPing(IReadOnlyCollection<int> batIds)
        {
            if (batIds == null) throw new ArgumentNullException(nameof(batIds));
            if (Layout == null) return;

            var revealed = new HashSet<int>(batIds);
            bool hasWave = pingRingSprite != null;

            foreach (KeyValuePair<int, BatView> pair in _views)
            {
                if (!revealed.Contains(pair.Key))
                {
                    pair.Value.Conceal(settings.ConcealFadeSeconds);
                    continue;
                }

                float distanceCells = ((Vector2)pair.Value.transform.localPosition).magnitude / Layout.CellSize;
                float delay = hasWave ? EchoWaveTiming.DelayFor(distanceCells, settings.WaveSpeedCells) : 0f;
                pair.Value.Reveal(delay, settings.RevealFadeSeconds, settings.RevealPopScale, RaiseBatRevealed);
            }

            if (hasWave) PlayWave();
        }

        private void RaiseBatRevealed()
        {
            BatRevealed?.Invoke();
        }

        private void PlayWave()
        {
            _wavePool ??= new ObjectPool<PingWave>(CreateWave, wave => wave.gameObject.SetActive(true), wave => wave.gameObject.SetActive(false));

            PingWave wave = _wavePool.Get();
            _activeWaves.Add(wave);

            float cell = Layout.CellSize;
            float reachWorld = EchoWaveTiming.ReachCells(Layout.Width, Layout.Height) * cell;
            wave.transform.localPosition = Vector3.zero;
            wave.Play(settings.WaveSpeedCells * cell, reachWorld, settings.WaveBandShare, () => ReleaseWave(wave));
        }

        private PingWave CreateWave()
        {
            var host = new GameObject("PingWave");
            host.transform.SetParent(transform, false);
            PingWave wave = host.AddComponent<PingWave>();
            wave.Setup(pingRingSprite, settings.WaveSortingOrder);
            return wave;
        }

        private void ReleaseWave(PingWave wave)
        {
            if (!_activeWaves.Remove(wave)) return;

            wave.Stop();
            _wavePool.Release(wave);
        }

        // A short sideways shake of every bat: "no, that did nothing".
        public void ShowRejected()
        {
            if (Layout == null) return;

            // Finish a shake still running, so a second refusal never starts from a displaced container.
            batContainer.DOComplete();
            batContainer
                .DOPunchPosition(
                    Vector3.right * (settings.RejectShakeCells * Layout.CellSize),
                    settings.RejectShakeDuration,
                    settings.RejectShakeVibrato)
                .SetLink(gameObject);
        }

        // Plays one tap. onComplete runs when the animation ends, after an exited bat has been recycled.
        public void Play(MoveResult result, Action onComplete = null)
        {
            int batId = result.Bat.Id;
            if (!_views.TryGetValue(batId, out BatView view))
                throw new InvalidOperationException($"Bat {batId} has no view to animate.");

            MovePlan plan = MovePathPlanner.Plan(result, Layout, settings.ExitOvershootCells, settings.BumpCells);
            Transform bat = view.transform;
            Sequence sequence = DOTween.Sequence().SetLink(view.gameObject);

            if (plan.Outcome == MoveOutcome.Exited)
            {
                sequence.Append(bat.DOLocalMove(plan.Target, plan.TravelCells / settings.FlySpeed).SetEase(settings.ExitEase));
                sequence.OnComplete(() =>
                {
                    _views.Remove(batId);
                    _factory.Release(view);
                    onComplete?.Invoke();
                });
                return;
            }

            if (plan.TravelCells > 0f)
                sequence.Append(bat.DOLocalMove(plan.Target, plan.TravelCells / settings.FlySpeed).SetEase(Ease.Linear));

            sequence.Append(bat.DOLocalMove(plan.Bump, settings.BumpDuration).SetEase(Ease.OutQuad));
            sequence.AppendCallback(() => PunchBlocker(result));
            sequence.Append(bat.DOLocalMove(plan.Start, settings.ReturnDuration).SetEase(settings.ReturnEase));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void PunchBlocker(MoveResult result)
        {
            if (result.BlockingBat == null || !_views.TryGetValue(result.BlockingBat.Id, out BatView blocker)) return;

            Cell offset = result.Bat.Direction.ToOffset();
            Vector3 punch = new Vector3(offset.X, offset.Y, 0f) * (settings.BlockerPunchCells * Layout.CellSize);
            blocker.transform.DOPunchPosition(punch, settings.BlockerPunchDuration, 6).SetLink(blocker.gameObject);
        }
    }
}
