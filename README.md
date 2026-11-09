# Orbiska

Arcade missile defense: six cities, three interceptor batteries, five waves of incoming missiles.

## Where to play

| Platform | Link |
|----------|------|
| itch.io (WebGL + Android) | <https://peryloth.itch.io/orbiska> |
| Google Play | Coming soon — listing pending review |
| Website & legal pages | <https://luisplata.github.io/orbiska/> (published from the `orbiska` repository on GitHub Pages) |

## v0.0.1 — first Orbiska release

First release under the Orbiska name. Same game, new identity and a new Android package (`com.bellseboss.orbiska`).

**What's in the build**

- Six cities to keep alive; lose them all and the run ends.
- Three interceptor batteries (A, D, O), 10 shots each. Holding fire uses the nearest battery that still has ammo, one shot every 0.5 s.
- Five waves with rising speed: normal missiles, MIRVs that split into three warheads, and smart missiles that dodge and re-aim at a living city.
- Pick 1 of 3 upgrades between waves: Double (two interceptors per shot), Slow (enemies slowed for 3 s), Thrusters (faster interceptors and aiming), Radius+ (bigger explosion), Reload (shorter cooldown).
- Score per intercept, victory after clearing wave 5 with at least one city standing.
- Fully offline. No sign-in, no ads, no analytics, nothing stored between runs.

**Controls**

- Hold left mouse button / touch: aim and fire at the pointer.
- `Esc`: back to the main menu.

## Build

Unity `6000.3.10f1`. Targets WebGL (itch.io) and Android (AAB for Google Play, APK for itch.io). WebGL and Android builds are produced by the GitHub Actions workflows in `.github/workflows/`; there is no desktop target.

## License

Proprietary — all rights reserved. See [NOTICE](NOTICE).
