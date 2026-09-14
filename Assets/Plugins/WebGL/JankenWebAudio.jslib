mergeInto(LibraryManager.library, {
  $JankenAudio: {
    contexts: [],
    nextContextIndex: 0,
    contextCount: 10,
    voiceCapacity: 10,

    updateDebugState: function () {
      var states = [];
      for (var i = 0; i < JankenAudio.contexts.length; i++) states.push(JankenAudio.contexts[i].state);
      window.__jankenAudioDebug.contextCount = JankenAudio.contexts.length;
      window.__jankenAudioDebug.voiceCapacity = JankenAudio.voiceCapacity;
      window.__jankenAudioDebug.contextStates = states;
      window.__jankenAudioDebug.state = states.length > 0 && states.every(function (state) { return state === 'running'; }) ? 'running' : states.join(',');
    },

    ensureContexts: function () {
      if (!window.__jankenAudioDebug) {
        window.__jankenAudioDebug = { state: 'not-created', plays: 0, lastSound: -1, contextCount: 0, contextStates: [], voiceCapacity: JankenAudio.voiceCapacity };
      }
      if (JankenAudio.contexts.length === 0) {
        var AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (AudioContextClass) {
          for (var i = 0; i < JankenAudio.contextCount; i++) {
            try {
              var ctx = new AudioContextClass();
              ctx.onstatechange = JankenAudio.updateDebugState;
              JankenAudio.contexts.push(ctx);
            } catch (error) {
              console.warn('[JankenAudio] AudioContext pool limited at ' + JankenAudio.contexts.length, error);
              break;
            }
          }
          JankenAudio.updateDebugState();
        } else {
          window.__jankenAudioDebug.state = 'unsupported';
        }
      }
      return JankenAudio.contexts;
    },

    nextContext: function () {
      var contexts = JankenAudio.ensureContexts();
      if (contexts.length === 0) return null;
      var index = JankenAudio.nextContextIndex % contexts.length;
      JankenAudio.nextContextIndex = (index + 1) % contexts.length;
      window.__jankenAudioDebug.contextIndex = index;
      return contexts[index];
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

    play: function (soundId, volume, ctx) {
      ctx = ctx || JankenAudio.nextContext();
      if (!ctx) return;
      window.__jankenAudioDebug.plays += 1;
      window.__jankenAudioDebug.lastSound = soundId;
      JankenAudio.updateDebugState();
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
        // 「ジャン」「ケン」の拍を取る、短く乾いたクリック。
        JankenAudio.tone(ctx, master, now, 0.085, 'square', 1760, 1160, 0.28);
        JankenAudio.noise(ctx, master, now, 0.045, 0.14);
      } else if (soundId === 3) {
        // 「ポン」は少し低く強いクリック。同じ短音系統で拍を統一する。
        JankenAudio.tone(ctx, master, now, 0.115, 'square', 1320, 720, 0.38);
        JankenAudio.noise(ctx, master, now, 0.06, 0.18);
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
        // どの手でも共通使用する短いクリック音。
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
      } else if (soundId === 10) {
        JankenAudio.tone(ctx, master, now, 0.44, 'square', 520, 95, 0.32);
        JankenAudio.tone(ctx, master, now + 0.05, 0.39, 'sine', 260, 82, 0.38);
      } else if (soundId === 11) {
        // グー: 低いド(C3)を矩形波ブザーで鳴らす。
        JankenAudio.tone(ctx, master, now, 0.56, 'square', 130.81, 130.81, 0.42);
        JankenAudio.tone(ctx, master, now, 0.50, 'sine', 261.62, 258, 0.10);
        JankenAudio.tone(ctx, master, now + 0.045, 0.48, 'sine', 360, 350, 0.09);
        JankenAudio.noise(ctx, master, now, 0.05, 0.06);
      } else if (soundId === 12) {
        // チョキ: ソ(G3)のノコギリ波を一度下げ、後半で元の高さへ戻す。
        JankenAudio.tone(ctx, master, now, 0.31, 'sawtooth', 196.00, 164.81, 0.40);
        JankenAudio.tone(ctx, master, now + 0.31, 0.31, 'sawtooth', 164.81, 196.00, 0.36);
        JankenAudio.noise(ctx, master, now, 0.08, 0.16);
        JankenAudio.noise(ctx, master, now + 0.43, 0.045, 0.12);
        JankenAudio.tone(ctx, master, now + 0.07, 0.28, 'sine', 1250, 520, 0.08);
      } else if (soundId === 13) {
        // パー: 高いド(C4)を丸いサイン波で鳴らす。
        JankenAudio.noise(ctx, master, now, 0.05, 0.18);
        JankenAudio.tone(ctx, master, now + 0.02, 0.56, 'sine', 261.63, 261.63, 0.56);
        JankenAudio.tone(ctx, master, now + 0.045, 0.48, 'sine', 760, 735, 0.07);
      }
    }
  },

  JankenWebAudio_Init__deps: ['$JankenAudio'],
  JankenWebAudio_Init: function () {
    var unlock = function () {
      var contexts = JankenAudio.ensureContexts();
      var resumes = [];
      for (var i = 0; i < contexts.length; i++) {
        if (contexts[i].state === 'suspended') resumes.push(contexts[i].resume());
      }
      Promise.all(resumes).then(function () {
        JankenAudio.updateDebugState();
        console.log('[JankenAudio] pool ready: ' + window.__jankenAudioDebug.contextStates.join(','));
      });
      document.removeEventListener('pointerdown', unlock, true);
      document.removeEventListener('keydown', unlock, true);
    };
    document.addEventListener('pointerdown', unlock, true);
    document.addEventListener('keydown', unlock, true);
    console.log('[JankenAudio] waiting for user input');
  },

  JankenWebAudio_Play__deps: ['$JankenAudio'],
  JankenWebAudio_Play: function (soundId, volume) {
    var ctx = JankenAudio.nextContext();
    if (!ctx) return;
    if (ctx.state === 'suspended') {
      ctx.resume().then(function () {
        JankenAudio.play(soundId, volume, ctx);
        console.log('[JankenAudio] played ' + soundId + ': ' + ctx.state + ' (pool)');
      });
    } else {
      JankenAudio.play(soundId, volume, ctx);
      console.log('[JankenAudio] played ' + soundId + ': ' + ctx.state + ' (pool)');
    }
  }
});
