using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiOptions.Models;
using LogiOptions.Native;
using LogiOptions.Services;
using Xunit;

namespace LogiOptions.Tests
{
    public class SimulationEngineTests
    {
        private MacroPlaybackEngine CreateEngine(RecordingInputInjector injector, int seed = 42)
            => new MacroPlaybackEngine(injector, new ThreadSafeRandom(seed));

        // ────────────────────────────────────────────
        // Constructor validation
        // ────────────────────────────────────────────

        [Fact]
        public void Constructor_NullInjector_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SimulationEngine(null!, new ThreadSafeRandom()));
        }

        [Fact]
        public void Constructor_NullRandom_Throws()
        {
            var injector = new RecordingInputInjector();
            Assert.Throws<ArgumentNullException>(() => new SimulationEngine(injector, (ThreadSafeRandom)null!));
        }

        // ────────────────────────────────────────────
        // Key press: happy path
        // ────────────────────────────────────────────

        [Fact]
        public async Task SimulateKeyPress_EmitsKeyDownThenKeyUp()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.PlaybackKeyPressAsync(NativeMethods.VK_SCROLL);

            Assert.Equal(2, injector.Events.Count);
            Assert.Equal(InputEventType.KeyDown, injector.Events[0].Type);
            Assert.Equal(NativeMethods.VK_SCROLL, injector.Events[0].vk);
            Assert.Equal(InputEventType.KeyUp, injector.Events[1].Type);
            Assert.Equal(NativeMethods.VK_SCROLL, injector.Events[1].vk);
        }

        [Fact]
        public async Task SimulateKeyPress_PageDown_UsesCorrectVK()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.PlaybackKeyPressAsync(NativeMethods.VK_NEXT);

            Assert.All(injector.Events, ev => Assert.Equal(NativeMethods.VK_NEXT, ev.vk));
        }

        [Fact]
        public async Task SimulateKeyPress_PageUp_UsesCorrectVK()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.PlaybackKeyPressAsync(NativeMethods.VK_PRIOR);

            Assert.All(injector.Events, ev => Assert.Equal(NativeMethods.VK_PRIOR, ev.vk));
        }

        // ────────────────────────────────────────────
        // Mouse scroll: happy path
        // ────────────────────────────────────────────

        [Fact]
        public async Task SimulateMouseScroll_EmitsOnlyWheelEvents()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.PlaybackMouseScrollAsync();

            Assert.NotEmpty(injector.Events);
            Assert.All(injector.Events, ev => Assert.Equal(InputEventType.MouseWheel, ev.Type));
        }

        [Fact]
        public async Task SimulateMouseScroll_AllDeltasAreMultiplesOf120()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.PlaybackMouseScrollAsync();

            Assert.All(injector.Events, ev => Assert.Equal(0, ev.wheelDelta % 120));
        }

        // ────────────────────────────────────────────
        // Mouse movement: happy path
        // ────────────────────────────────────────────

        [Fact]
        public async Task SimulateMouseMovement_EmitsMouseMoveEvents()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            await engine.SyncPeripheralStateAsync();

            Assert.NotEmpty(injector.Events);
            Assert.All(injector.Events, ev => Assert.Equal(InputEventType.MouseMove, ev.Type));
        }

        [Fact]
        public async Task SimulateMouseMovement_CursorPositionChanges()
        {
            var injector = new RecordingInputInjector { CursorPosition = new System.Drawing.Point(500, 500) };
            var engine = CreateEngine(injector);

            var before = injector.CursorPosition;
            await engine.SyncPeripheralStateAsync();

            // After many micro-moves the cursor should have shifted
            Assert.True(injector.CursorPosition != before, "Cursor should have moved.");
        }

        // ────────────────────────────────────────────
        // Page scroll oscillation
        // ────────────────────────────────────────────

        [Fact]
        public async Task PageScroll_ReverseDirectionAfterLimit()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            // Force a known small limit
            engine.PageScrollDown = true;
            engine.PageScrollCount = 0;
            engine.PageScrollLimit = 0; // will be picked fresh (3-7)

            // Execute enough page scrolls to guarantee at least one reversal
            bool startedDown = true;
            bool sawReversal = false;
            for (int i = 0; i < 20; i++)
            {
                injector.Clear();
                await engine.PlaybackPageScrollAsync();

                // Check the VK used
                var keyDown = injector.Events.First(e => e.Type == InputEventType.KeyDown);
                bool isDown = keyDown.vk == NativeMethods.VK_NEXT;

                if (i > 0 && isDown != startedDown)
                {
                    sawReversal = true;
                    break;
                }
            }

            Assert.True(sawReversal, "Page scroll should reverse direction after reaching its limit.");
        }

        [Fact]
        public async Task PageScroll_AlternatesBetweenPageDownAndPageUp()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector, seed: 123);

            var keysUsed = new HashSet<ushort>();

            for (int i = 0; i < 30; i++)
            {
                injector.Clear();
                await engine.PlaybackPageScrollAsync();
                var keyDown = injector.Events.First(e => e.Type == InputEventType.KeyDown);
                keysUsed.Add(keyDown.vk);
            }

            Assert.Contains(NativeMethods.VK_NEXT, keysUsed);
            Assert.Contains(NativeMethods.VK_PRIOR, keysUsed);
        }

        // ────────────────────────────────────────────
        // Weighted action selection
        // ────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTick_RespectsWeightedDistribution()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector, seed: 99);

            var counts = new Dictionary<string, int>
            {
                ["MouseScroll"] = 0,
                ["KeyPress"] = 0, ["PageScroll"] = 0
            };

            int runs = 200;
            for (int i = 0; i < runs; i++)
            {
                injector.Clear();
                string action = await engine.ExecutePlaybackTickAsync();
                counts[action]++;
            }

            Assert.True(counts["MouseScroll"] > counts["KeyPress"],
                $"MouseScroll ({counts["MouseScroll"]}) should occur more than KeyPress ({counts["KeyPress"]}).");
            Assert.True(counts["MouseScroll"] > counts["PageScroll"],
                $"MouseScroll ({counts["MouseScroll"]}) should occur more than PageScroll ({counts["PageScroll"]}).");
        }

        [Fact]
        public async Task ExecuteTick_ZeroWeightAction_NeverFires()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector, seed: 77);

            // Disable KeyPress entirely
            engine.WKeyPress = 0;

            for (int i = 0; i < 100; i++)
            {
                injector.Clear();
                string action = await engine.ExecutePlaybackTickAsync();
                Assert.NotEqual("KeyPress", action);
            }
        }

        [Fact]
        public async Task ExecuteTick_OnlyMouseScrollWeight_AlwaysMouseScroll()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector, seed: 55);

            engine.WMouseScroll = 100;
            engine.WKeyPress = 0;
            engine.WPageScroll = 0;

            for (int i = 0; i < 20; i++)
            {
                injector.Clear();
                string action = await engine.ExecutePlaybackTickAsync();
                Assert.Equal("MouseScroll", action);
            }
        }

        // ────────────────────────────────────────────
        // VK pool from config
        // ────────────────────────────────────────────

        [Fact]
        public void ApplySettings_ParsesHexVirtualKeyPool()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            var settings = new Settings
            {
                VirtualKeyPool = new List<string> { "0x91", "0x22", "0x21" }
            };

            engine.ApplySettings(settings);

            Assert.Equal(3, engine.VirtualKeyPool.Count);
            Assert.Contains(NativeMethods.VK_SCROLL, engine.VirtualKeyPool);
            Assert.Contains(NativeMethods.VK_NEXT, engine.VirtualKeyPool);
            Assert.Contains(NativeMethods.VK_PRIOR, engine.VirtualKeyPool);
        }

        [Fact]
        public void ApplySettings_EmptyPool_KeepsDefault()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            var settings = new Settings { VirtualKeyPool = new List<string>() };
            engine.ApplySettings(settings);

            Assert.Single(engine.VirtualKeyPool);
            Assert.Equal(NativeMethods.VK_SCROLL, engine.VirtualKeyPool[0]);
        }

        [Fact]
        public void ApplySettings_InvalidHex_SkippedGracefully()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            var settings = new Settings
            {
                VirtualKeyPool = new List<string> { "not_hex", "0x22" }
            };
            engine.ApplySettings(settings);

            Assert.Single(engine.VirtualKeyPool);
            Assert.Equal(NativeMethods.VK_NEXT, engine.VirtualKeyPool[0]);
        }

        [Fact]
        public void ApplySettings_UpdatesWeights()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            var settings = new Settings
            {
                Weights = new ActionWeights
                {
                    MouseScroll = 20,
                    KeyPress = 10,
                    PageScroll = 10
                }
            };
            engine.ApplySettings(settings);

            Assert.Equal(20, engine.WMouseScroll);
            Assert.Equal(10, engine.WKeyPress);
            Assert.Equal(10, engine.WPageScroll);
        }

        [Fact]
        public void ApplySettings_NegativeWeights_ClampedToZero()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);

            var settings = new Settings
            {
                Weights = new ActionWeights { MouseScroll = -10, KeyPress = 0, PageScroll = 100 }
            };
            engine.ApplySettings(settings);

            Assert.Equal(0, engine.WMouseScroll);
            Assert.Equal(0, engine.WKeyPress);
            Assert.Equal(100, engine.WPageScroll);
        }

        // ────────────────────────────────────────────
        // PickRandomKey
        // ────────────────────────────────────────────

        [Fact]
        public void PickRandomKey_ReturnsFromPool()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);
            engine.VirtualKeyPool = new List<ushort> { 0x91, 0x22, 0x21 };

            for (int i = 0; i < 50; i++)
            {
                ushort key = engine.PickRandomKey();
                Assert.Contains(key, engine.VirtualKeyPool);
            }
        }

        [Fact]
        public void PickRandomKey_SingleItemPool_AlwaysReturnsThat()
        {
            var injector = new RecordingInputInjector();
            var engine = CreateEngine(injector);
            engine.VirtualKeyPool = new List<ushort> { 0xFF };

            for (int i = 0; i < 10; i++)
                Assert.Equal(0xFF, engine.PickRandomKey());
        }
    }
}
