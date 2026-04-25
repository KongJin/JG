using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Shared.EventBus;
using Shared.Math;
using Shared.Runtime.Sound;
using Shared.Sound;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class SoundSystemReflectionTests
    {
        private static readonly FieldInfo CatalogEntriesField =
            typeof(SoundCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo LastPlayTimeField =
            typeof(SoundPlayer).GetField("_lastPlayTime", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void SoundCatalog_ReportsDuplicateKeys()
        {
            var catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            var clip = AudioClip.Create("test", 441, 1, 44100, false);
            CatalogEntriesField.SetValue(catalog, new[]
            {
                new SoundEntry("ui_click", clip),
                new SoundEntry("ui_click", clip),
                new SoundEntry("ui_confirm", clip),
            });

            CollectionAssert.AreEqual(new[] { "ui_click" }, catalog.GetDuplicateKeys());

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void SoundPlayer_ComputesClampedChannelVolumes()
        {
            var go = new GameObject("SoundPlayerTest");
            var player = go.AddComponent<SoundPlayer>();
            var sfxEntry = new SoundEntry("sfx", null, volume: 0.5f, channel: SoundChannel.Sfx);
            var bgmEntry = new SoundEntry("bgm", null, volume: 0.8f, channel: SoundChannel.Bgm);

            player.SetMasterVolume(1.5f);
            player.SetChannelVolumes(0.25f, 2f);

            Assert.That(player.GetEffectiveVolume(sfxEntry), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(player.GetEffectiveVolume(bgmEntry), Is.EqualTo(0.2f).Within(0.0001f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SoundPlayer_RespectsSfxCooldown()
        {
            var catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            var clip = AudioClip.Create("click", 441, 1, 44100, false);
            CatalogEntriesField.SetValue(catalog, new[]
            {
                new SoundEntry("ui_click", clip, cooldown: 1f),
            });

            var go = new GameObject("SoundPlayerTest");
            var player = go.AddComponent<SoundPlayer>();
            typeof(SoundPlayer)
                .GetField("catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player, catalog);

            var eventBus = new EventBus();
            player.Initialize(eventBus, "local");

            var request = new SoundRequestEvent(new SoundRequest(
                "ui_click",
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                "local"));

            eventBus.Publish(request);
            eventBus.Publish(request);

            var lastPlayTime = (Dictionary<string, float>)LastPlayTimeField.GetValue(player);
            Assert.That(lastPlayTime, Has.Count.EqualTo(1));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(clip);
        }
    }
}
