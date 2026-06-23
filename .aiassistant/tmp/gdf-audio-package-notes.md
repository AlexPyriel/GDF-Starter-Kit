# GDF Audio package notes

Temporary working notes for continuing the `GDF.Audio` package discussion in a future chat.

## Current position

- Do **not** copy `AppFox/Assets/ThirdParty/MCE/Audio` 1:1.
- Use the AppFox package as an architectural reference only.
- The good reusable idea from AppFox is the separation between:
  - runtime playback state for SFX,
  - runtime playback state for music,
  - persisted audio settings,
  - actual `AudioSource`-based scene/runtime players.

## Important user decisions

- Do **not** split out a separate `GDF.Audio.UI` package.
- UI-specific helpers like button/toggle sound hooks are considered extra glue and should not define the package boundary.
- Since this is the GDF ecosystem, it is acceptable and desirable to integrate with:
  - `LocalDataStorage`
  - `ConfigRepository`
- Still prefer depending on their interfaces or narrow contracts when that does not noticeably bloat the package API.

## AppFox audio analysis summary

### Useful ideas to keep

- Separate music flow and SFX flow.
- Keep settings persistence separate from playback logic.
- Use dedicated runtime players for:
  - pooled SFX playback,
  - music playback with crossfade.

### Things not to copy directly

- `int` IDs everywhere for sounds and music.
- Hard dependency on AppFox-specific `ConfigsAsset` / `LocalSaveService`.
- Linear `FirstOrDefault` config lookups during playback.
- Extremely thin `SoundService` / `MusicService` event dispatchers that carry almost no playback context.
- Static extension-based IDs like `SoundExtension` / `MusicExtension`.
- Public API that is tightly coupled to scene `MonoBehaviour` injectables.

## Recommended GDF.Audio shape

Create a single reusable core package: `GDF.Audio`.

### Package responsibilities

- audio settings persistence
- SFX playback
- music playback
- audio catalog/config lookup
- runtime `AudioSource` pooling / players
- mixer group / bus support

### Public runtime API direction

- `IAudioSettingsService`
- `ISfxService`
- `IMusicService`
- `IAudioCatalog` or equivalent repository-facing resolver

### Data direction

- `AudioCue`
  - key
  - clip or clips
  - volume
  - optional pitch range
  - mixer group / bus
  - optional cooldown / max voices
- `MusicCue`
  - key
  - clip
  - default volume
  - fade settings

## Initial v1 scope recommendation

Keep `GDF.Audio` intentionally smaller than AppFox:

- `AudioCue` catalog
- `SfxService`
- `MusicService`
- `AudioSettingsService`
- integration with `LocalDataStorage`
- integration with `ConfigRepository`
- `AudioMixer` buses like:
  - `Master`
  - `Music`
  - `Sfx`
- `AudioSource` pooling for SFX
- 2-source crossfade player for music

## Explicitly out of v1 for now

- world / positional / 3D audio layer
- addressables integration
- network-driven sound replication
- UI helper mini-components as a separate package concern

## Open design questions for next chat

1. What should be the cue key type?
   - `enum`
   - `string`
   - dedicated key struct/value object

2. How tightly should `GDF.Audio` depend on GDF ecosystem packages?
   - direct concrete usage of `ConfigRepository` and `LocalDataStorage`
   - or narrow interfaces/adapters over them

3. Should the package ship with `AudioMixer` integration as a hard requirement in v1, or keep it optional?

4. Should the first catalog format be:
   - plain clip references in ScriptableObjects
   - or a repository-backed cue model from day one

5. Do we want runtime players exposed as public Unity-facing components, or keep them internal behind services and installers?

## Practical recommendation for tomorrow

Start from package boundaries and contracts first, not from players:

1. lock the v1 responsibilities
2. choose cue key strategy
3. choose concrete package dependencies on `LocalDataStorage` and `ConfigRepository`
4. define public interfaces
5. only then shape runtime players and data assets
