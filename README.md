LS2OVR
======

[![Build Status](https://travis-ci.com/MikuAuahDark/LS2OVR.svg?branch=master)](https://travis-ci.com/MikuAuahDark/LS2OVR)

C# implementation of [Live Simulator: 2 "Over the Rainbow"](https://github.com/MikuAuahDark/livesim2/blob/over_the_rainbow/docs/ls2ovr_beatmap_format.txt)
beatmap decoding and encoding. This repository consist of 3 programs and 1 library:

* LS2OVR: The main library for loading and saving LS2OVR beatmaps.

* LS2OVR.Pack: Packs folder to LS2OVR beatmap, providing `.ls2ovr.yaml` exist.

* LS2OVR.Unpack: Unpack LS2OVR beatmap to a directory, writing the `.ls2ovr.yaml` file.

* LS2OVR.Inspect: Inspect LS2OVR beatmap file.

License
-------

* Main program: Zlib

* [fNBT](https://github.com/fragmer/fNbt): 3-Clause BSD license

* [Newtonsoft.Json](https://www.newtonsoft.com/json): MIT

* [YamlDotNet](https://github.com/aaubry/YamlDotNet): MIT

* [CommandLine](https://github.com/commandlineparser/commandline): MIT
