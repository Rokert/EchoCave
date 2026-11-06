# Challenges

Very hard levels, each with its own id, ranked on taps, then time, then who solved it first (D-036). The code is in `Assets/EchoCave/Core/Challenges`; this file records the levels.

## challenge-001

| | |
|---|---|
| Board | 9 x 9 |
| Bats | 40, every one hidden |
| Pings | 1 (a ping reveals every arrow for `EchoSettings.RevealSeconds`, 3 s) |
| Move limit | none: the ranking is the number of taps, so a mistake costs rank, not the run |
| Minimum | 40 taps (one per bat, no bounce) |
| Blocking chain | 20 waves (8, 5, 4, 3, 2, 2, 2, 1, 1, 1, 1, 2, then 1 eight times), 32 bats blocked at the start |
| A blind player | wastes about 26 of those taps and finishes with no wasted tap in under 0.1 % of runs |
| Recipe | `ChallengeLevels.Challenge001Spec`, seed 259 |

How it was found: 6 board configurations (8x8 and 9x9, 28 to 40 bats, 20 to 40 hidden, 1 or 2 pings) with 300 seeds each, every solvable layout measured with `ChallengeDifficulty` (150 trials), the best eight measured again with 3000. The winner had the highest score (a blind player's wasted taps, the hidden bats and the depth of the chain). The full table is not kept: the recipe is.

```
 .  v  .  .  ^  .  <  .  <
 ^  .  <  .  ^  <  .  .  ^
 .  v  .  .  .  .  ^  <  .
 .  .  .  .  .  >  v  v  ^
 <  .  ^  .  <  <  v  .  >
 <  <  ^  v  .  .  .  .  .
 .  .  .  .  .  .  v  .  ^
 <  .  .  >  .  ^  v  >  >
 .  v  <  .  .  <  >  >  ^
```

(Top row is y = 8. In the game every arrow is hidden.)

### What it does not prove

The level is hard to play by luck, not impossible to solve: the arrows can be read from a ping, a bounce shows its bat's arrow, and nothing stops a player from writing the order down. Once someone solves it and shares the order, every later solver can tie on taps, and the board becomes a contest of time. That is what the ranking is built for (taps, then time, then who was first), and the first solver keeps the "first" for good. A new challenge is the answer when a solution has spread.

## challenge-002

| | |
|---|---|
| Board | 9 x 9 |
| Bats | 48, every one hidden |
| Pings | 1 |
| Move limit | none |
| Minimum | 48 taps |
| Blocking chain | 18 waves (9, 5, 8, 6, 2, 1, 2, 2, 1, 1, 1, 2, 1, 1, 1, 2, 2, 1), 39 bats blocked at the start |
| A blind player | wastes about 32 of those taps; a flawless run by luck is under 0.1 % |
| Recipe | `ChallengeLevels.Challenge002Spec`, seed 175 |

The densest board of the three and the highest difficulty score (130.5). Found by rolling 300 seeds on 9x9 with 44 and 48 bats and taking the best of the 48-bat roll.

```
 <  .  ^  <  <  <  ^  ^  .
 <  .  .  ^  ^  <  .  .  .
 .  <  >  ^  .  ^  .  .  v
 .  .  .  >  >  >  .  ^  v
 ^  >  ^  .  >  ^  .  ^  .
 .  ^  .  <  <  .  v  <  .
 .  >  .  ^  >  .  .  ^  v
 ^  <  >  >  .  .  v  .  .
 >  ^  >  v  ^  .  >  .  .
```

## challenge-003

| | |
|---|---|
| Board | 8 x 8 |
| Bats | 36, every one hidden |
| Pings | none: the only way to learn an arrow is to spend a tap on a bounce |
| Move limit | none |
| Minimum | 36 taps |
| Blocking chain | 20 waves (6, 3, 2, 2, 1, then 2 seven times, then 1 eight times), 30 bats blocked at the start |
| A blind player | wastes about 25 of those taps; a flawless run by luck is under 0.1 % |
| Recipe | `ChallengeLevels.Challenge003Spec`, seed 11 |

A different kind of hard: no ping, so memory and deduction replace the reveal.

```
 >  .  v  ^  >  .  .  v
 ^  .  .  ^  .  .  <  >
 .  v  <  ^  ^  >  ^  v
 >  .  .  ^  .  .  v  .
 .  .  .  ^  .  .  <  <
 .  .  <  .  ^  .  .  .
 v  >  >  ^  .  .  >  .
 <  .  <  <  <  ^  <  v
```

(Top row is y = 7.)

## Adding a challenge

1. Roll candidates with the generator and `ChallengeDifficulty` (a spec with many hidden bats, few pings, no floor on the structure, a huge difficulty window), keep the best.
2. Add a `Challenge00N` recipe (spec and seed) next to `Challenge001` in `ChallengeLevels` and its numbers to the tests in `ChallengeLevelsTests`: they fail if the generator ever changes the level.
3. Add its id to `ChallengeLevels.AllIds`, `SpecOf` and `SeedOf`. Then run `EchoCave > Challenges > Create missing challenge assets` (level id stays 0: challenges are identified by their string id) and `Create or update challenge catalog`, which appends it at the end. The shipped tests check the assets and the catalog against `AllIds`.
4. Add it to this file.

Never edit an old recipe or its asset: its id is what the leaderboard hangs on. `EchoCave > Challenges > Check challenge assets` and `ShippedChallengesTests` tell you if an asset has drifted from its recipe.
