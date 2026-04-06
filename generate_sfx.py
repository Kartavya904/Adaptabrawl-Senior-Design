import wave
import struct
import math
import random
import os

def generate_wav(filename, duration, sample_rate, wave_func):
    num_samples = int(duration * sample_rate)
    with wave.open(filename, 'w') as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        for i in range(num_samples):
            t = float(i) / sample_rate
            sample = wave_func(t, num_samples, i)
            sample = max(-32768, min(32767, int(sample * 32767.0)))
            wav_file.writeframes(struct.pack('h', sample))

os.makedirs('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX', exist_ok=True)
S_RATE = 44100

# 1. Slash (light attack whoosh)
def slash_func(t, total_samps, i):
    env = math.exp(-t * 22)
    return (random.random() * 2.0 - 1.0) * env * 0.55
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/slash.wav', 0.25, S_RATE, slash_func)

# 2. Hit (light punch thud)
def hit_func(t, total_samps, i):
    freq = 250 * math.exp(-t * 35) + 60
    env = math.exp(-t * 18)
    tone = math.sin(2 * math.pi * freq * t)
    noise = (random.random() * 2.0 - 1.0) * 0.25
    return (tone + noise) * env * 0.85
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/hit.wav', 0.3, S_RATE, hit_func)

# 3. Heavy Hit (deep bass thud)
def heavy_hit_func(t, total_samps, i):
    freq = 120 * math.exp(-t * 20) + 40
    env = math.exp(-t * 10)
    tone = math.sin(2 * math.pi * freq * t)
    noise = (random.random() * 2.0 - 1.0) * 0.35
    return (tone * 0.7 + noise * 0.5) * env * 0.9
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/heavy_hit.wav', 0.4, S_RATE, heavy_hit_func)

# 4. Block (metallic clink)
def block_func(t, total_samps, i):
    freq = 1400
    env = math.exp(-t * 28)
    mod = math.sin(2 * math.pi * 280 * t)
    return math.sin(2 * math.pi * (freq + mod * 180) * t) * env * 0.55
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/block.wav', 0.2, S_RATE, block_func)

# 5. Swap (ascending teleport whirr)
def swap_func(t, total_samps, i):
    freq = 180 + (t * 900)
    env = math.sin(math.pi * (i / total_samps))
    return math.sin(2 * math.pi * freq * t) * env * 0.65
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/swap.wav', 0.5, S_RATE, swap_func)

# 6. Countdown Beep
def beep_func(t, total_samps, i):
    freq = 880
    env = 1.0 if t < 0.08 else math.exp(-(t - 0.08) * 12)
    return math.sin(2 * math.pi * freq * t) * env * 0.45
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/beep.wav', 0.3, S_RATE, beep_func)

# 7. Fight Start (deep gong)
def fight_func(t, total_samps, i):
    freq = 110
    env = math.exp(-t * 1.8)
    tone1 = math.sin(2 * math.pi * freq * t)
    tone2 = math.sin(2 * math.pi * (freq * 1.5) * t) * 0.5
    return (tone1 + tone2) * env * 0.85
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/fight.wav', 1.2, S_RATE, fight_func)

# 8. Match Over (dramatic descending stab)
def matchover_func(t, total_samps, i):
    freq = max(55, 320 - (t * 120))
    env = math.exp(-t * 1.4)
    return math.sin(2 * math.pi * freq * t) * env * 0.75
generate_wav('Adaptabrawl/Adaptabrawl/Assets/Resources/SFX/matchover.wav', 2.0, S_RATE, matchover_func)

print("Generated 8 SFX files in Resources/SFX!")
