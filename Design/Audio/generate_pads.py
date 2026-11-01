#!/usr/bin/env python3
"""Synthesizes one seamless ambient pad per challenge (16 s loops, 22.05 kHz mono) into Assets/Resources/SFX/Pad_<Challenge>.wav.
Every frequency and modulation is a whole number of cycles over the loop, so the loop point is silent. Run: python3 generate_pads.py"""
import os, wave
import numpy as np

SR = 22050
LOOP = 16.0
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'SFX')
t = np.arange(int(SR * LOOP)) / SR

def hz(note):  # semitones from A4 = 440
    return 440.0 * 2 ** (note / 12)

def snap(f):  # a whole number of cycles in the loop
    return round(f * LOOP) / LOOP

def voice(f, lfo_cycles, phase, depth=0.35):
    f = snap(f)
    lfo = 1 - depth + depth * np.sin(2 * np.pi * lfo_cycles / LOOP * t + phase)
    tone = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * snap(f * 2) * t + 1.0) + 0.12 * np.sin(2 * np.pi * snap(f * 3) * t + 2.0)
    return tone * lfo

# Chords as semitones from A4 (A=0): C4 = -9, etc.
PADS = {
    'Classic':    [-9, -5, -2, 5],      # C E G D : bright, open
    'Marathon':   [-12, -5, 2, 14],     # A E B + high shimmer : cold fifths
    'Summer':     [-16, -12, -9, -5],   # F A C E : warm major seventh
    'Autumn':     [-19, -16, -12, -9],  # D F A C : minor seventh
    'Orchard':    [-14, -10, -7, -4],   # G B D F : mixolydian
    'Greenhouse': [-17, -15, -10, -5],  # D E A D : suspended, airy
}
rng = np.random.default_rng(3)
for name, notes in PADS.items():
    y = np.zeros_like(t)
    for i, n in enumerate(notes):
        y += voice(hz(n), 1 + (i % 3), rng.random() * 6.28, 0.3) * (1.0 - i * 0.12)
        y += 0.5 * voice(hz(n) * 1.003, 2 + i, rng.random() * 6.28, 0.4)  # a hair detuned: slow beating
    y += 0.12 * np.sin(2 * np.pi * 1 / LOOP * t) * np.sin(2 * np.pi * snap(hz(notes[-1] + 12)) * t)  # a slow breath of the top note
    y = y / np.max(np.abs(y)) * 0.55
    data = (y * 32767).astype('<i2')
    with wave.open(os.path.join(OUT, 'Pad_%s.wav' % name), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
print('pads written')
