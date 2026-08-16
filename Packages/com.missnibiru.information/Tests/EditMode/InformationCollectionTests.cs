using System;
using System.Collections.Generic;
using MissNibiru.Information.Collection;
using MissNibiru.Information.Data;
using MissNibiru.Information.Unity;
using NUnit.Framework;
using UnityEngine;
using InformationCollectionService =
    MissNibiru.Information.Collection.InformationCollection;

using Object = UnityEngine.Object;

namespace MissNibiru.Information.Tests
{
    public sealed class InformationCollectionTests
    {
        private readonly List<Object> _createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject
                     in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Store_TrimsAndPreventsDuplicateIds()
        {
            InMemoryInformationCollectionStore store =
                new InMemoryInformationCollectionStore();

            Assert.That(
                store.TryAdd(" potion "),
                Is.True);

            Assert.That(
                store.Contains("potion"),
                Is.True);

            Assert.That(
                store.TryAdd("potion"),
                Is.False);

            Assert.That(
                store.TryAdd("Potion"),
                Is.True);

            Assert.That(
                store.TryAdd("   "),
                Is.False);

            Assert.That(
                store.CollectedIds.Count,
                Is.EqualTo(2));

            Assert.That(
                store.CollectedIds[0],
                Is.EqualTo("potion"));

            Assert.That(
                store.CollectedIds[1],
                Is.EqualTo("Potion"));
        }

        [Test]
        public void Register_ResolvesEntryById()
        {
            InformationEntry entry =
                CreateEntry("ancient_note");

            InformationCollectionService collection =
                CreateCollection();

            collection.Register(entry);

            bool found =
                collection.TryGetRegisteredEntry(
                    "ancient_note",
                    out InformationEntry result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(entry));
        }

        [Test]
        public void Register_RejectsInvalidAndDuplicateIds()
        {
            InformationEntry validEntry =
                CreateEntry("potion");

            InformationEntry duplicateEntry =
                CreateEntry("potion");

            InformationEntry blankEntry =
                CreateEntry(" ");

            InformationCollectionService collection =
                CreateCollection();

            Assert.Throws<ArgumentNullException>(
                () => collection.Register(null));

            Assert.Throws<ArgumentException>(
                () => collection.Register(blankEntry));

            collection.Register(validEntry);

            Assert.Throws<InvalidOperationException>(
                () => collection.Register(
                    duplicateEntry));
        }

        [Test]
        public void TryCollect_ReportsFirstAndRepeatedCollection()
        {
            InformationEntry entry =
                CreateEntry("training_sword");

            InformationCollectionService collection =
                CreateCollection();

            collection.Register(entry);

            int firstEventCount = 0;
            int reportEventCount = 0;

            collection.FirstCollected +=
                _ => firstEventCount++;

            collection.CollectionReported +=
                _ => reportEventCount++;

            Assert.That(
                collection.TryCollect(
                    entry,
                    out InformationCollectionResult first),
                Is.True);

            Assert.That(
                first.IsFirstCollection,
                Is.True);

            Assert.That(
                collection.TryCollect(
                    entry,
                    out InformationCollectionResult repeat),
                Is.True);

            Assert.That(
                repeat.IsFirstCollection,
                Is.False);

            Assert.That(firstEventCount, Is.EqualTo(1));
            Assert.That(reportEventCount, Is.EqualTo(2));

            Assert.That(
                collection.CollectedEntries.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void Collection_FiltersByTypeAndCategory()
        {
            InformationType itemType =
                CreateType("item");

            InformationType weaponType =
                CreateType("weapon");

            InformationCategory potionCategory =
                CreateCategory("potion");

            InformationCategory bladeCategory =
                CreateCategory("blade");

            InformationEntry potion =
                CreateEntry(
                    "healing_potion",
                    itemType,
                    potionCategory);

            InformationEntry sword =
                CreateEntry(
                    "training_sword",
                    weaponType,
                    bladeCategory);

            InformationCollectionService collection =
                CreateCollection();

            collection.RegisterRange(
                new[] { potion, sword });

            collection.TryCollect(potion, out _);
            collection.TryCollect(sword, out _);

            Assert.That(
                collection
                    .GetCollectedByType(itemType)
                    .Count,
                Is.EqualTo(1));

            Assert.That(
                collection
                    .GetCollectedByCategory(
                        bladeCategory)
                    .Count,
                Is.EqualTo(1));
        }

        [Test]
        public void Clear_RemovesCollectionButKeepsRegistry()
        {
            InformationEntry entry =
                CreateEntry("note");

            InformationCollectionService collection =
                CreateCollection();

            collection.Register(entry);
            collection.TryCollect(entry, out _);
            collection.Clear();

            Assert.That(
                collection.Contains("note"),
                Is.False);

            Assert.That(
                collection.TryGetRegisteredEntry(
                    "note",
                    out InformationEntry registered),
                Is.True);

            Assert.That(registered, Is.SameAs(entry));
        }

        [Test]
        public void StoredId_ResolvesAfterEntryIsRegistered()
        {
            InMemoryInformationCollectionStore store =
                new InMemoryInformationCollectionStore(
                    new[] { "late_entry" });

            InformationCollectionService collection =
                new InformationCollectionService(store);

            Assert.That(
                collection.CollectedEntries.Count,
                Is.EqualTo(0));

            InformationEntry entry =
                CreateEntry("late_entry");

            collection.Register(entry);

            Assert.That(
                collection.CollectedEntries.Count,
                Is.EqualTo(1));

            Assert.That(
                collection.CollectedEntries[0],
                Is.SameAs(entry));
        }

        [Test]
        public void InformationSource_ForwardsCollection()
        {
            InformationEntry entry =
                CreateEntry("world_statue");

            InformationDatabase database =
                CreateAsset<InformationDatabase>();

            database.Configure(entry);

            GameObject gameObject =
                new GameObject(
                    "Information Source Test");

            _createdObjects.Add(gameObject);

            InformationCollectionComponent component =
                gameObject.AddComponent<
                    InformationCollectionComponent>();

            component.Configure(
                database,
                new InMemoryInformationCollectionStore());

            InformationSource source =
                gameObject.AddComponent<
                    InformationSource>();

            source.Configure(entry, component);

            Assert.That(
                source.TryCollect(
                    out InformationCollectionResult result),
                Is.True);

            Assert.That(
                result.IsFirstCollection,
                Is.True);

            Assert.That(
                component.Contains("world_statue"),
                Is.True);
        }

        private InformationCollectionService CreateCollection()
        {
            return new InformationCollectionService(
                new InMemoryInformationCollectionStore());
        }

        private InformationEntry CreateEntry(
            string id,
            InformationType type = null,
            InformationCategory category = null)
        {
            InformationEntry entry =
                CreateAsset<InformationEntry>();

            entry.Configure(
                id,
                id,
                "Test information.",
                type,
                category);

            return entry;
        }

        private InformationType CreateType(
            string id)
        {
            InformationType type =
                CreateAsset<InformationType>();

            type.Configure(id, id);

            return type;
        }

        private InformationCategory CreateCategory(
            string id)
        {
            InformationCategory category =
                CreateAsset<InformationCategory>();

            category.Configure(id, id);

            return category;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset =
                ScriptableObject.CreateInstance<T>();

            _createdObjects.Add(asset);

            return asset;
        }
    }
}