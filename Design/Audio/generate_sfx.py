#!/usr/bin/env python3
"""Synthesizes the game's extra sound effects (no samples, just maths): writes mono 16-bit 44.1 kHz WAVs into Assets/Resources/SFX/.
Run from this folder: python3 generate_sfx.py"""
import os, wave
import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'SFX')
rng = np.random.default_rng(7)

def t(dur): return np.arange(int(SR * dur)) / SR
def env(n, attack=0.005, decay=6.0):
    x = np.arange(n) / SR
    a = np.minimum(1.0, x / attack)
    return a * np.exp(-decay * x)
def tone(freq, dur, decay=6.0, harmonics=((1, 1.0),), attack=0.004):
    x = t(dur)
    y = sum(amp * np.sin(2 * np.pi * freq * h * x) for h, amp in harmonics)
    return y * env(len(x), attack, decay)
def bell(freq, dur, decay=7.0):
    # Inharmonic partials: a struck metal/bell colour.
    return tone(freq, dur, decay, ((1, 1.0), (2.76, 0.5), (5.4, 0.25), (8.93, 0.12)))
def place(buf, sound, at):
    i = int(at * SR)
    end = min(len(buf), i + len(sound))
    buf[i:end] += sound[:end - i]
def noise(dur, decay, lp=0.0):
    n = rng.standard_normal(int(SR * dur))
    if lp > 0:
        k = max(1, int(lp))
        n = np.convolve(n, np.ones(k) / k, mode='same')
    return n * env(len(n), 0.002, decay)
def save(name, y, gain=0.8):
    y = y / max(1e-6, np.max(np.abs(y))) * gain
    data = (np.clip(y, -1, 1) * 32767).astype('<i2')
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())

NOTE = lambda semis: 261.63 * 2 ** (semis / 12)   # semis above middle C

def arpeggio(steps, spacing, dur, decay=7.0, shape=None):
    buf = np.zeros(int(SR * (spacing * len(steps) + dur)))
    for i, s in enumerate(steps):
        sound = (shape or (lambda f: tone(f, dur, decay, ((1, 1.0), (2, 0.35), (3, 0.15)))))(NOTE(s))
        place(buf, sound, i * spacing)
    return buf

# A line harvested: a quick bright run up, with a soft swish underneath.
buf = arpeggio([0, 4, 7, 12], 0.055, 0.35, 8.0)
place(buf, noise(0.25, 14.0, 6) * 0.25, 0.0)
save('Harvest', buf)

# The anvil takes the hit: a metallic clang.
buf = np.zeros(int(SR * 0.9))
place(buf, bell(520, 0.9, 5.5), 0.0)
place(buf, bell(780, 0.5, 9.0) * 0.5, 0.0)
place(buf, noise(0.04, 60.0) * 0.6, 0.0)
save('Clang', buf)

# Compost: a seed crumbles into earth (low filtered noise crackles).
buf = np.zeros(int(SR * 0.7))
for i in range(7):
    place(buf, noise(0.12, 25.0, 14) * (0.9 - i * 0.1), i * 0.045 + rng.random() * 0.02)
place(buf, tone(95, 0.3, 10.0) * 0.6, 0.0)
save('Crumble', buf)

# A mastery level-up: a rising sparkle with a held chord on top.
buf = arpeggio([0, 4, 7, 12, 16], 0.07, 0.6, 5.0)
place(buf, (tone(NOTE(12), 0.8, 3.5) + tone(NOTE(16), 0.8, 3.5) + tone(NOTE(19), 0.8, 3.5)) * 0.4, 0.3)
save('LevelUp', buf)

# Victory fanfare.
buf = np.zeros(int(SR * 2.2))
for i, (semi, at, d) in enumerate([(0, 0.0, 0.25), (0, 0.18, 0.25), (0, 0.36, 0.25), (5, 0.55, 0.5), (4, 1.05, 0.2), (7, 1.25, 0.9)]):
    place(buf, tone(NOTE(semi), d + 0.3, 4.0, ((1, 1.0), (2, 0.5), (3, 0.3), (4, 0.15))), at)
for semi in (0, 4, 7, 12):
    place(buf, tone(NOTE(semi), 1.1, 2.6, ((1, 1.0), (2, 0.3))) * 0.45, 1.25)
save('Victory', buf)

# Defeat: a sinking minor sting.
buf = np.zeros(int(SR * 1.6))
for i, semi in enumerate([7, 3, 0, -5]):
    place(buf, tone(NOTE(semi - 12), 0.7, 4.5, ((1, 1.0), (2, 0.4), (3, 0.2))), i * 0.28)
save('Defeat', buf)

# Round cleared: three cheerful rising notes.
buf = arpeggio([0, 4, 7], 0.11, 0.5, 6.0)
place(buf, tone(NOTE(12), 0.7, 4.0, ((1, 1.0), (2, 0.3))), 0.33)
save('RoundClear', buf)

# A fertilizer lands in its slot: a soft pop and a tiny sparkle.
buf = np.zeros(int(SR * 0.4))
x = t(0.12)
place(buf, np.sin(2 * np.pi * (260 + 520 * np.exp(-x * 40)) * x) * env(len(x), 0.002, 28.0), 0.0)
place(buf, tone(NOTE(19), 0.3, 12.0), 0.05)
save('SlotPop', buf)

# A purchase: two bright coin notes.
buf = np.zeros(int(SR * 0.5))
place(buf, bell(1318, 0.45, 9.0), 0.0)
place(buf, bell(1760, 0.4, 10.0), 0.07)
save('Buy', buf)
print('sfx written to', os.path.normpath(OUT))
