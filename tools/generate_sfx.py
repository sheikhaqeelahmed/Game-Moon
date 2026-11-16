"""Synthesises Planet Scrappers' placeholder sound effects as 16-bit mono WAV files.
All sounds are generated from maths here, so they are original and licence-free.
Usage: python tools/generate_sfx.py Assets/Audio/Generated
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100
random.seed(7)


def write(path, samples):
    peak = max(1e-6, max(abs(s) for s in samples))
    scale = 0.85 / peak
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * scale)) * 32767)) for s in samples))


def env(t, attack, decay):
    if t < attack:
        return t / attack
    return math.exp(-(t - attack) / decay)


def noise():
    return random.uniform(-1, 1)


def lowpass(samples, k):
    out, y = [], 0.0
    for s in samples:
        y += k * (s - y)
        out.append(y)
    return out


def metal_hit(length=0.45):
    # Inharmonic partials of a struck steel plate plus a click.
    partials = [(523, 1.0), (1187, 0.6), (1734, 0.45), (2391, 0.3), (3307, 0.2)]
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        s = sum(a * math.sin(2 * math.pi * f * t) * math.exp(-t * (6 + f / 400)) for f, a in partials)
        s += noise() * math.exp(-t * 60) * 0.8
        out.append(s)
    return out


def bonk(length=0.35):
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        f = 140 * math.exp(-t * 3)
        out.append(math.sin(2 * math.pi * f * t) * env(t, 0.003, 0.08) + noise() * math.exp(-t * 80) * 0.3)
    return lowpass(out, 0.3)


def crunch(length=1.1):
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        rumble = math.sin(2 * math.pi * 55 * t) * env(t, 0.01, 0.35)
        grit = noise() * env(t, 0.005, 0.25)
        bolts = 0.0
        for k, start in enumerate((0.12, 0.2, 0.31, 0.45, 0.6)):
            dt = t - start
            if dt > 0:
                bolts += math.sin(2 * math.pi * (1500 + k * 370) * dt) * math.exp(-dt * 40) * 0.5
        out.append(rumble * 0.9 + grit * 0.7 + bolts)
    return out


def tink(length=0.25):
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        out.append((math.sin(2 * math.pi * 1760 * t) + 0.4 * math.sin(2 * math.pi * 2640 * t)) * env(t, 0.002, 0.06))
    return out


def register(length=1.2):
    out = []
    clicks = [0.0, 0.07, 0.14, 0.21, 0.28, 0.35]
    for i in range(int(RATE * length)):
        t = i / RATE
        s = 0.0
        for c in clicks:
            dt = t - c
            if 0 <= dt < 0.03:
                s += noise() * (1 - dt / 0.03) * 0.6
        dt = t - 0.45
        if dt > 0:
            s += (math.sin(2 * math.pi * 1318 * dt) + 0.6 * math.sin(2 * math.pi * 1975 * dt)) * math.exp(-dt * 4)
        out.append(s)
    return out


def arpeggio(notes, step, length, decay=0.25):
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        s = 0.0
        for k, f in enumerate(notes):
            dt = t - k * step
            if dt > 0:
                s += (math.sin(2 * math.pi * f * dt) + 0.3 * math.sin(4 * math.pi * f * dt)) * env(dt, 0.005, decay)
        out.append(s)
    return out


def click(length=0.06):
    return [noise() * env(i / RATE, 0.001, 0.008) + math.sin(2 * math.pi * 2200 * i / RATE) * env(i / RATE, 0.001, 0.01)
            for i in range(int(RATE * length))]


def step(length=0.18):
    out = [noise() * env(i / RATE, 0.004, 0.04) for i in range(int(RATE * length))]
    return lowpass(out, 0.08)


def whoosh(length=0.25):
    out = [noise() * math.sin(math.pi * i / (RATE * length)) for i in range(int(RATE * length))]
    return lowpass(out, 0.12)


def ambience(length=12.0):
    # Seamless loop: low suit hum plus slow breathing-like swell. Partials chosen to loop cleanly.
    out = []
    for i in range(int(RATE * length)):
        t = i / RATE
        hum = 0.35 * math.sin(2 * math.pi * 60 * t) + 0.15 * math.sin(2 * math.pi * 120 * t)
        swell = 0.5 + 0.5 * math.sin(2 * math.pi * t / 4.0)
        out.append(hum * 0.4 + noise() * 0.12 * swell)
    return lowpass(out, 0.05)


def warning(length=0.6):
    return arpeggio([880, 660], 0.22, length, decay=0.12)


if __name__ == "__main__":
    out_dir = sys.argv[1]
    os.makedirs(out_dir, exist_ok=True)
    sounds = {
        "hit_metal": metal_hit(),
        "hit_bonk": bonk(),
        "break_crunch": crunch(),
        "pickup_tink": tink(),
        "sell_register": register(),
        "purchase": arpeggio([523, 659, 784, 1046], 0.08, 0.8),
        "mission_complete": arpeggio([392, 523, 659, 784, 1046], 0.11, 1.4, decay=0.4),
        "ui_click": click(),
        "footstep": step(),
        "swing_whoosh": whoosh(),
        "ambience_loop": ambience(),
        "warning": warning(),
    }
    for name, samples in sounds.items():
        write(os.path.join(out_dir, name + ".wav"), samples)
        print(name, round(len(samples) / RATE, 2), "s")
