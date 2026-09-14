mergeInto(LibraryManager.library, {
  $JankenAudio: {
    context: null,

    ensureContext: function () {
      if (!window.__jankenAudioDebug) {
        window.__jankenAudioDebug = { state: 'not-created', plays: 0, lastSound: -1 };
      }
      if (!JankenAudio.context) {
        var AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (AudioContextClass) {
          JankenAudio.context = new AudioContextClass();
          window.__jankenAudioDebug.state = JankenAudio.context.state;
          JankenAudio.context.onstatechange = function () {
            window.__jankenAudioDebug.state = JankenAudio.context.state;
          };
        } else {
          window.__jankenAudioDebug.state = 'unsupported';
        }
      }
      return JankenAudio.context;
    },

    envelope: function (gain, start, attack, duration, peak) {
      gain.gain.cancelScheduledValues(start);
      gain.gain.setValueAtTime(0.0001, start);
      gain.gain.exponentialRampToValueAtTime(Math.max(0.0001, peak), start + attack);
      gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    },

    tone: function (ctx, destination, start, duration, type, fromHz, toHz, volume) {
      var oscillator = ctx.createOscillator();
      var gain = ctx.createGain();
      oscillator.type = type;
      oscillator.frequency.setValueAtTime(Math.max(1, fromHz), start);
      oscillator.frequency.exponentialRampToValueAtTime(Math.max(1, toHz), start + duration);
      JankenAudio.envelope(gain, start, 0.006, duration, volume);
      oscillator.connect(gain);
      gain.connect(destination);
      oscillator.start(start);
      oscillator.stop(start + duration + 0.02);
    },

    noise: function (ctx, destination, start, duration, volume) {
      var frameCount = Math.max(1, Math.floor(ctx.sampleRate * duration));
      var buffer = ctx.createBuffer(1, frameCount, ctx.sampleRate);
      var samples = buffer.getChannelData(0);
      for (var i = 0; i < frameCount; i++) samples[i] = Math.random() * 2 - 1;
      var source = ctx.createBufferSource();
      var gain = ctx.createGain();
      source.buffer = buffer;
      JankenAudio.envelope(gain, start, 0.002, duration, volume);
      source.connect(gain);
      gain.connect(destination);
      source.start(start);
    },

    play: function (soundId, volume) {
      var ctx = JankenAudio.ensureContext();
      if (!ctx) return;
      window.__jankenAudioDebug.plays += 1;
      window.__jankenAudioDebug.lastSound = soundId;
      window.__jankenAudioDebug.state = ctx.state;
      var master = ctx.createGain();
      master.gain.value = Math.min(1, Math.max(0, volume)) * 0.72;
      master.connect(ctx.destination);
      var now = ctx.currentTime + 0.008;

      if (soundId === 0) {
        JankenAudio.tone(ctx, master, now, 0.62, 'sawtooth', 260, 1050, 0.30);
        JankenAudio.tone(ctx, master, now + 0.03, 0.58, 'sine', 520, 1500, 0.34);
        JankenAudio.noise(ctx, master, now, 0.34, 0.10);
      } else if (soundId === 1) {
        JankenAudio.tone(ctx, master, now, 0.15, 'square', 650, 920, 0.28);
        JankenAudio.tone(ctx, master, now, 0.13, 'sine', 980, 1250, 0.22);
      } else if (soundId === 2) {
        JankenAudio.tone(ctx, master, now, 0.22, 'sine', 125, 48, 0.72);
        JankenAudio.noise(ctx, master, now, 0.10, 0.12);
      } else if (soundId === 3) {
        JankenAudio.tone(ctx, master, now, 0.58, 'sine', 150, 42, 0.78);
        JankenAudio.tone(ctx, master, now, 0.44, 'sawtooth', 1050, 170, 0.25);
        JankenAudio.noise(ctx, master, now, 0.42, 0.34);
      } else if (soundId === 4) {
        var notes = [523.25, 659.25, 783.99, 1046.5, 1318.5];
        for (var n = 0; n < notes.length; n++) {
          JankenAudio.tone(ctx, master, now + n * 0.14, 0.42, 'sine', notes[n], notes[n] * 1.01, 0.42);
          JankenAudio.tone(ctx, master, now + n * 0.14, 0.28, 'triangle', notes[n] * 2, notes[n] * 2.01, 0.13);
        }
        JankenAudio.noise(ctx, master, now + 0.45, 0.55, 0.08);
      } else if (soundId === 5) {
        JankenAudio.tone(ctx, master, now, 0.82, 'sawtooth', 340, 82, 0.36);
        JankenAudio.tone(ctx, master, now + 0.06, 0.75, 'sine', 170, 48, 0.36);
      } else if (soundId === 6) {
        JankenAudio.tone(ctx, master, now, 0.65, 'triangle', 245, 185, 0.42);
        JankenAudio.tone(ctx, master, now + 0.08, 0.55, 'sine', 310, 235, 0.24);
      } else if (soundId === 7) {
        // 実選択とフェイクで共通使用する、情報を漏らさないクリック音。
        JankenAudio.tone(ctx, master, now, 0.14, 'square', 720, 980, 0.26);
        JankenAudio.tone(ctx, master, now, 0.12, 'sine', 1080, 1320, 0.18);
        JankenAudio.noise(ctx, master, now, 0.07, 0.08);
      } else if (soundId === 8) {
        for (var beat = 0; beat < 6; beat++) {
          var beatStart = now + beat * 0.27;
          JankenAudio.tone(ctx, master, beatStart, 0.22, 'sine', 82 + beat * 8, 46, 0.34 + beat * 0.045);
          JankenAudio.tone(ctx, master, beatStart, 0.24, 'sawtooth', 170 + beat * 55, 220 + beat * 70, 0.055);
        }
      } else if (soundId === 9) {
        JankenAudio.tone(ctx, master, now, 0.68, 'sine', 165, 38, 0.82);
        JankenAudio.tone(ctx, master, now, 0.48, 'sawtooth', 1450, 210, 0.22);
        JankenAudio.noise(ctx, master, now, 0.52, 0.36);
      } else {
        JankenAudio.tone(ctx, master, now, 0.44, 'square', 520, 95, 0.32);
        JankenAudio.tone(ctx, master, now + 0.05, 0.39, 'sine', 260, 82, 0.38);
      }
    }
  },

  JankenWebAudio_Init__deps: ['$JankenAudio'],
  JankenWebAudio_Init: function () {
    var unlock = function () {
      var ctx = JankenAudio.ensureContext();
      if (ctx && ctx.state === 'suspended') {
        ctx.resume().then(function () {
          console.log('[JankenAudio] unlocked: ' + ctx.state);
        });
      } else if (ctx) {
        console.log('[JankenAudio] ready: ' + ctx.state);
      }
      document.removeEventListener('pointerdown', unlock, true);
      document.removeEventListener('keydown', unlock, true);
    };
    document.addEventListener('pointerdown', unlock, true);
    document.addEventListener('keydown', unlock, true);
    console.log('[JankenAudio] waiting for user input');
  },

  JankenWebAudio_Play__deps: ['$JankenAudio'],
  JankenWebAudio_Play: function (soundId, volume) {
    var ctx = JankenAudio.ensureContext();
    if (!ctx) return;
    if (ctx.state === 'suspended') {
      ctx.resume().then(function () {
        JankenAudio.play(soundId, volume);
        console.log('[JankenAudio] played ' + soundId + ': ' + ctx.state);
      });
    } else {
      JankenAudio.play(soundId, volume);
      console.log('[JankenAudio] played ' + soundId + ': ' + ctx.state);
    }
  }
});
