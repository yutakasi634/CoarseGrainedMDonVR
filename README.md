# Coral iMD VR

This is simple implementation of **Co**arese-grained gene**ral** purpose **i**nteractive **M**olecular **D**ynamics simulation on **V**irtual **R**eality.
Supported VR platform is Oculus Rift.

![2021-1-15-OfLatticeGo](https://user-images.githubusercontent.com/15133454/104824523-c3022080-5895-11eb-93a4-0ab95d6272e0.gif)

## Requirements
- Unity 2019.4.15f

## Project Setup
- Import `Oculus Integration 20.1` from Asset Store.
- Restart Unity and accept updates.
- Configure settings following https://developer.oculus.com/documentation/unity/unity-conf-settings/ .
- Import NuGetForUnity. The unitypackage file is [here](https://github.com/GlitchEnzo/NuGetForUnity/releases).
- Restart Unity and accept updates.
- Import the scene from `Project>Assets>Scenes>Main`.

## Input file format
You can specify each parameter of lennard-jones system, for example, radius of a specific particle, size of the simulation box, temperature and so on, by input file based on `Toml` format.

When you execute simulation from `play` button of Unity application, locate your input file as `(ProjectRoot)/input/input.toml` in Project folder.There is sample input file there.
When you execute simulation by the binary file in `(ProjectRoot)/bin` folder, locate your input file as `(ProjectRoot)/bin/input/input.toml`.

The detail of file format is like below. This file is a subset of [Mjolnir](https://github.com/Mjolnir-MD/Mjolnir)'s format.

```toml:input.toml
[simulator]
integrator.type = "UnderdampedLangevin"
integrator.gammas = [
{index = 0, gamma = 0.083424},
{index = 1, gamma = 0.053108},
# ...
]

[[systems]]
attributes.temperature = 300.0
particles = [
{m =  1.00, pos = [  0.9084463866571024, 2.667970365022592, 0.6623741650618591]}, # particle index 0
{m =  1.00, pos = [-0.39914657537482867, 2.940257103317942, 3.5414659037905025]}, # particle index 1
# ...
]

[[forcefields]]
[[forcefields.local]]
interaction = "BondLength"
potential   = "Harmonic"
parameters = [
{indices = [  0, 1], v0 = 3.822321, k = 60.0},
{indices = [  1, 2], v0 = 3.822569, k = 60.0},
# ...
]

# ...

[[forcefields.global]]
potential = "ExcludedVolume"
epsilon   = 0.6
parameters = [
{index =   0, radius = 2.0},
{index =   1, radius = 2.0},
# ...
]
```
